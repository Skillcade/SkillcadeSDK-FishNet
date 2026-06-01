using System;
using System.Collections.Generic;
using SkillcadeSDK.Connection;
using SkillcadeSDK.FishNetAdapter.Authenticator;
using SkillcadeSDK.StateMachine;
using UnityEngine;
using VContainer;
using VContainer.Unity;

#if UNITY_SERVER || UNITY_EDITOR
using SkillcadeSDK.ServerValidation;
#endif

namespace SkillcadeSDK.FishNetAdapter.Players
{
    /// <summary>
    /// Server-side coordinator for the reconnect grace period and replay client-id continuity.
    /// Holds a snapshot of a disconnected in-game player for ConnectionConfig.ReconnectGraceSeconds.
    /// Maps the new ephemeral FishNet connection id to the stable ReplayClientId from the original
    /// connection so replay frames keep going into the same client-world after a reconnect.
    /// </summary>
    public class PlayerReconnectService : IInitializable, ITickable, IDisposable
    {
        public class GraceSlot
        {
            public string PlayerId;
            public int ReplayClientId;
            public int LastConnectionClientId;

            public PlayerInGameData InGameData;
            public PlayerMatchData MatchData;
            public PlayerCharacterData CharacterData;
            public bool HasCharacterData;

            public string Nickname;
            public string CharacterName;

#if UNITY_SERVER || UNITY_EDITOR
            public string JoinToken;
            public SessionTokenPayload TokenPayload;
#endif

            public float Deadline;
        }

        public event Action<GraceSlot> OnGraceExpired;

        [Inject] private readonly ConnectionConfig _connectionConfig;
        [Inject] private readonly AuthenticatedPlayerDataStore _authStore;
        [Inject] private readonly IConnectionController _connectionController;

        private readonly Dictionary<string, int> _replayClientIdByPlayerId = new();
        private readonly Dictionary<int, int> _replayClientIdByConnectionId = new();
        private readonly Dictionary<string, GraceSlot> _graceSlots = new();
        private readonly List<string> _expiredBuffer = new();

#if UNITY_SERVER || UNITY_EDITOR
        private readonly Dictionary<int, string> _joinTokenByConnectionId = new();
        private readonly Dictionary<int, SessionTokenPayload> _tokenPayloadByConnectionId = new();
#endif

        public int PendingInGameCount => _graceSlots.Count;
        public IReadOnlyCollection<GraceSlot> ActiveGraceSlots => _graceSlots.Values;

        public bool IsGraceActive(string playerId)
        {
            bool active = !string.IsNullOrEmpty(playerId) && _graceSlots.ContainsKey(playerId);
            if (!string.IsNullOrEmpty(playerId))
                Debug.Log($"[PlayerReconnect] IsGraceActive({playerId})={active} (pending={_graceSlots.Count})");
            return active;
        }

        public bool TryGetGraceSlot(string playerId, out GraceSlot slot)
        {
            slot = null;
            return !string.IsNullOrEmpty(playerId) && _graceSlots.TryGetValue(playerId, out slot);
        }

        public void Initialize()
        {
            Debug.Log("[PlayerReconnect] Service initialized");
        }

        public void Dispose()
        {
        }

        public void Tick()
        {
            if (_graceSlots.Count == 0)
                return;

            float now = Time.realtimeSinceStartup;
            _expiredBuffer.Clear();
            foreach (var kvp in _graceSlots)
            {
                if (now >= kvp.Value.Deadline)
                    _expiredBuffer.Add(kvp.Key);
            }

            for (int i = 0; i < _expiredBuffer.Count; i++)
            {
                var id = _expiredBuffer[i];
                if (!_graceSlots.Remove(id, out var slot))
                    continue;

                Debug.Log($"[PlayerReconnect] Grace expired for player {id} (replayClientId={slot.ReplayClientId}, lastConnection={slot.LastConnectionClientId})");
                _authStore.ReleasePlayerId(id);
                OnGraceExpired?.Invoke(slot);
            }
        }

        /// <summary>
        /// Call once per successful auth (server side). On first connection assigns
        /// ReplayClientId = connectionClientId; on a reconnect remaps the new connection
        /// to the previously-assigned ReplayClientId.
        /// </summary>
        public int RegisterAuthenticatedConnection(string playerId, int connectionClientId)
        {
            int replayClientId;
            if (string.IsNullOrEmpty(playerId))
            {
                replayClientId = connectionClientId;
                _replayClientIdByConnectionId[connectionClientId] = replayClientId;
                Debug.Log($"[PlayerReconnect] RegisterAuthenticatedConnection without playerId: connection={connectionClientId} mapped to replay={replayClientId}");
                return replayClientId;
            }

            if (!_replayClientIdByPlayerId.TryGetValue(playerId, out replayClientId))
            {
                replayClientId = connectionClientId;
                _replayClientIdByPlayerId[playerId] = replayClientId;
                Debug.Log($"[PlayerReconnect] First connection: player={playerId} replayClientId={replayClientId} (connection={connectionClientId})");
            }
            else
            {
                Debug.Log($"[PlayerReconnect] Reconnect mapping: player={playerId} replayClientId={replayClientId} (new connection={connectionClientId}, replacing previous)");
            }

            _replayClientIdByConnectionId[connectionClientId] = replayClientId;
            return replayClientId;
        }

        /// <summary>
        /// Resolves a transient FishNet connection id to the stable ReplayClientId for that
        /// player. Falls back to the input when no mapping is registered (single-player,
        /// non-player connections, replay playback).
        /// </summary>
        public int ResolveReplayClientId(int connectionClientId)
        {
            if (_replayClientIdByConnectionId.TryGetValue(connectionClientId, out var replayClientId))
                return replayClientId;

            return connectionClientId;
        }

#if UNITY_SERVER || UNITY_EDITOR
        public void RememberAuthToken(int connectionClientId, string joinToken, SessionTokenPayload payload)
        {
            _joinTokenByConnectionId[connectionClientId] = joinToken;
            _tokenPayloadByConnectionId[connectionClientId] = payload;
            Debug.Log($"[PlayerReconnect] RememberAuthToken connection={connectionClientId} playerId={payload?.PlayerId} sessionId={payload?.GameSessionId} tokenLen={joinToken?.Length ?? 0}");
        }
#endif

        /// <summary>
        /// Server-side: called when a player network object is destroyed. If the player
        /// was actually playing during Countdown/Running, starts the grace period and
        /// reserves the PlayerId so other clients can't claim it.
        /// </summary>
        public bool TryBeginGracePeriod(int disconnectedClientId, FishNetPlayerData data, GameStateType currentStateType)
        {
            if (!_connectionController.IsServer)
                return false;

            if (data == null)
            {
                Debug.Log($"[PlayerReconnect] TryBeginGracePeriod: data is null for connection={disconnectedClientId}");
                return false;
            }

            float grace = _connectionConfig != null ? _connectionConfig.ReconnectGraceSeconds : 0f;
            if (grace <= 0f)
            {
                Debug.Log($"[PlayerReconnect] TryBeginGracePeriod skipped: ReconnectGraceSeconds={grace}");
                return false;
            }

            if (currentStateType != GameStateType.Countdown && currentStateType != GameStateType.Running)
            {
                Debug.Log($"[PlayerReconnect] TryBeginGracePeriod skipped: state={currentStateType} (only Countdown/Running)");
                return false;
            }

            if (!PlayerInGameData.TryGetFromPlayer(data, out var inGameData) || !inGameData.InGame)
            {
                Debug.Log($"[PlayerReconnect] TryBeginGracePeriod skipped: connection={disconnectedClientId} was not InGame");
                return false;
            }

            if (!PlayerMatchData.TryGetFromPlayer(data, out var matchData) || string.IsNullOrEmpty(matchData.PlayerId))
            {
                Debug.LogWarning($"[PlayerReconnect] TryBeginGracePeriod skipped: connection={disconnectedClientId} has no PlayerMatchData/PlayerId");
                return false;
            }

            if (_graceSlots.ContainsKey(matchData.PlayerId))
            {
                Debug.Log($"[PlayerReconnect] Grace slot already exists for player {matchData.PlayerId}");
                return false;
            }

            int replayClientId;
            if (!_replayClientIdByPlayerId.TryGetValue(matchData.PlayerId, out replayClientId))
            {
                replayClientId = data.PlayerNetworkId >= 0 ? data.PlayerNetworkId : disconnectedClientId;
                _replayClientIdByPlayerId[matchData.PlayerId] = replayClientId;
            }

            PlayerCharacterData characterData = null;
            bool hasCharacterData = PlayerCharacterData.TryGetFromPlayer(data, out characterData);

            string nickname = null;
            string characterName = null;
            if (_authStore.TryGetByClientId(disconnectedClientId, out var authData))
            {
                nickname = authData.Nickname;
                characterName = authData.CharacterName;
            }

            var slot = new GraceSlot
            {
                PlayerId = matchData.PlayerId,
                ReplayClientId = replayClientId,
                LastConnectionClientId = disconnectedClientId,
                InGameData = inGameData,
                MatchData = matchData,
                CharacterData = characterData,
                HasCharacterData = hasCharacterData,
                Nickname = nickname,
                CharacterName = characterName,
                Deadline = Time.realtimeSinceStartup + grace,
            };

#if UNITY_SERVER || UNITY_EDITOR
            if (_joinTokenByConnectionId.TryGetValue(disconnectedClientId, out var joinToken))
                slot.JoinToken = joinToken;
            if (_tokenPayloadByConnectionId.TryGetValue(disconnectedClientId, out var payload))
                slot.TokenPayload = payload;
#endif

            _graceSlots[matchData.PlayerId] = slot;
            _authStore.ReservePlayerId(matchData.PlayerId);

            Debug.Log($"[PlayerReconnect] Begin grace: player={matchData.PlayerId}, replayClientId={replayClientId}, lastConnection={disconnectedClientId}, graceSeconds={grace}, deadline={slot.Deadline:F2}, hasCharacterData={hasCharacterData}, inGamePending={_graceSlots.Count}");
#if UNITY_SERVER || UNITY_EDITOR
            LogJoinTokenSnapshot("grace-snapshot", slot.JoinToken, slot.TokenPayload);
#endif
            return true;
        }

        /// <summary>
        /// Hub flow: strict identity match. Non-hub flow: accept first reconnect into any free slot
        /// (identity cannot be proven without a signed token).
        /// </summary>
        public bool TryAcceptReconnect(
            int newConnectionClientId,
            string broadcastPlayerId,
            string broadcastNickname,
#if UNITY_SERVER || UNITY_EDITOR
            string joinToken,
            SessionTokenPayload payload,
#endif
            bool hubIntegrated,
            out GraceSlot slot,
            out string rejectReason)
        {
            slot = null;
            rejectReason = null;

#if UNITY_SERVER || UNITY_EDITOR
            Debug.Log($"[PlayerReconnect] TryAcceptReconnect connection={newConnectionClientId} broadcastPlayerId={broadcastPlayerId} nickname={broadcastNickname} hub={hubIntegrated} pendingSlots={_graceSlots.Count} tokenLen={joinToken?.Length ?? 0}");
#else
            Debug.Log($"[PlayerReconnect] TryAcceptReconnect connection={newConnectionClientId} broadcastPlayerId={broadcastPlayerId} nickname={broadcastNickname} hub={hubIntegrated} pendingSlots={_graceSlots.Count}");
#endif

            if (hubIntegrated)
            {
                if (string.IsNullOrEmpty(broadcastPlayerId))
                {
                    rejectReason = "broadcast PlayerId is empty";
                    Debug.LogWarning($"[PlayerReconnect] Hub reject: {rejectReason}");
                    return false;
                }

                if (!_graceSlots.TryGetValue(broadcastPlayerId, out slot))
                {
                    rejectReason = $"no active grace slot for player {broadcastPlayerId}";
                    Debug.LogWarning($"[PlayerReconnect] Hub reject: {rejectReason}. Active slots: [{FormatGraceSlotKeys()}]");
                    return false;
                }

                float remaining = slot.Deadline - Time.realtimeSinceStartup;
                Debug.Log($"[PlayerReconnect] Hub matched grace slot player={slot.PlayerId} replayClientId={slot.ReplayClientId} lastConnection={slot.LastConnectionClientId} remainingSec={remaining:F2}");

#if UNITY_SERVER || UNITY_EDITOR
                LogReconnectTokenComparison(slot.JoinToken, slot.TokenPayload, joinToken, payload);

                bool identityMatch = TokenPayloadMatchesForReconnect(slot.TokenPayload, payload);
                Debug.Log($"[PlayerReconnect] Hub reconnect identity check (playerId+gameSessionId only): match={identityMatch}");

                if (!identityMatch)
                {
                    rejectReason = "session identity does not match grace snapshot (playerId or gameSessionId)";
                    Debug.LogWarning($"[PlayerReconnect] Hub reject player={broadcastPlayerId}: {rejectReason}");
                    slot = null;
                    return false;
                }
#endif

                if (slot.MatchData != null && !string.Equals(slot.MatchData.PlayerId, broadcastPlayerId, StringComparison.Ordinal))
                {
                    rejectReason = "broadcast PlayerId does not match grace snapshot";
                    Debug.LogWarning($"[PlayerReconnect] Reject reconnect player={broadcastPlayerId}: {rejectReason}");
                    slot = null;
                    return false;
                }

                if (!string.IsNullOrEmpty(slot.Nickname)
                    && !string.IsNullOrEmpty(broadcastNickname)
                    && !string.Equals(slot.Nickname, broadcastNickname, StringComparison.Ordinal))
                {
                    rejectReason = "broadcast Nickname does not match grace snapshot";
                    Debug.LogWarning($"[PlayerReconnect] Reject reconnect player={broadcastPlayerId}: {rejectReason}");
                    slot = null;
                    return false;
                }
            }
            else
            {
                // Identity cannot be verified without hub; give the slot to the first reconnect.
                if (_graceSlots.Count == 0)
                    return false;

                // Prefer slot whose PlayerId equals the broadcast value, otherwise take any.
                if (string.IsNullOrEmpty(broadcastPlayerId) || !_graceSlots.TryGetValue(broadcastPlayerId, out slot))
                {
                    using var enumerator = _graceSlots.GetEnumerator();
                    if (!enumerator.MoveNext())
                        return false;
                    slot = enumerator.Current.Value;
                }

                Debug.Log($"[PlayerReconnect] Non-hub reconnect accepted into slot player={slot.PlayerId} (identity not verified)");
            }

            if (slot == null)
                return false;

            foreach (var kvp in _replayClientIdByConnectionId)
            {
                if (kvp.Value == slot.ReplayClientId && kvp.Key != newConnectionClientId)
                    Debug.LogWarning($"[PlayerReconnect] Orphan mapping detected: connection={kvp.Key} also maps to replayClientId={slot.ReplayClientId}, replacing with connection={newConnectionClientId}");
            }

            _replayClientIdByConnectionId[newConnectionClientId] = slot.ReplayClientId;
            slot.LastConnectionClientId = newConnectionClientId;
            Debug.Log($"[PlayerReconnect] Accept reconnect: player={slot.PlayerId}, replayClientId={slot.ReplayClientId}, newConnection={newConnectionClientId}, hub={hubIntegrated}");
            return true;
        }

        /// <summary>
        /// Server-side: caller (FishNetPlayersController) takes the slot snapshot, applies it to
        /// the freshly spawned FishNetPlayerData and triggers a respawn.
        /// </summary>
        public bool TryConsumeReconnectSlot(string playerId, out GraceSlot slot)
        {
            slot = null;
            if (string.IsNullOrEmpty(playerId))
                return false;

            if (!_graceSlots.Remove(playerId, out slot))
                return false;

            _authStore.ReleasePlayerId(playerId);
            Debug.Log($"[PlayerReconnect] Consume slot: player={playerId}, replayClientId={slot.ReplayClientId}, lastConnection={slot.LastConnectionClientId}");
            return true;
        }

        public void ForgetConnection(int connectionClientId)
        {
            Debug.Log($"[PlayerReconnect] ForgetConnection connection={connectionClientId} (clearing auth token cache only)");
#if UNITY_SERVER || UNITY_EDITOR
            _joinTokenByConnectionId.Remove(connectionClientId);
            _tokenPayloadByConnectionId.Remove(connectionClientId);
#endif
            // Note: _replayClientIdByConnectionId is intentionally kept until match end so late
            // replay frames from the old connection still resolve to the right world.
        }

        public void ResetForNewMatch()
        {
            Debug.Log($"[PlayerReconnect] Reset for new match (clearing {_replayClientIdByPlayerId.Count} mappings, {_graceSlots.Count} pending slots)");
            _replayClientIdByPlayerId.Clear();
            _replayClientIdByConnectionId.Clear();
            foreach (var kvp in _graceSlots)
                _authStore.ReleasePlayerId(kvp.Key);
            _graceSlots.Clear();
#if UNITY_SERVER || UNITY_EDITOR
            _joinTokenByConnectionId.Clear();
            _tokenPayloadByConnectionId.Clear();
#endif
        }

        private string FormatGraceSlotKeys()
        {
            if (_graceSlots.Count == 0)
                return string.Empty;

            var parts = new List<string>(_graceSlots.Count);
            foreach (var kvp in _graceSlots)
            {
                float remaining = kvp.Value.Deadline - Time.realtimeSinceStartup;
                parts.Add($"{kvp.Key}(replay={kvp.Value.ReplayClientId},t={remaining:F1}s)");
            }
            return string.Join(", ", parts);
        }

#if UNITY_SERVER || UNITY_EDITOR
        /// <summary>
        /// Grace reconnect: same player in the same match (incoming token already passed ValidateToken).
        /// </summary>
        private static bool TokenPayloadMatchesForReconnect(SessionTokenPayload snapshot, SessionTokenPayload incoming)
        {
            if (snapshot == null || incoming == null)
                return false;

            return string.Equals(snapshot.PlayerId, incoming.PlayerId, StringComparison.Ordinal)
                   && string.Equals(snapshot.GameSessionId, incoming.GameSessionId, StringComparison.Ordinal);
        }

        private static void LogJoinTokenSnapshot(string context, string joinToken, SessionTokenPayload payload)
        {
            Debug.Log($"[PlayerReconnect] [{context}] joinToken={joinToken ?? "(null)"}");
            LogSessionPayloadFields(context, payload);
            Debug.Log($"[PlayerReconnect] [{context}] tokenParts={DescribeJoinTokenParts(joinToken)}");
        }

        private static void LogReconnectTokenComparison(
            string snapshotToken,
            SessionTokenPayload snapshotPayload,
            string incomingToken,
            SessionTokenPayload incomingPayload)
        {
            bool rawEqual = string.Equals(snapshotToken, incomingToken, StringComparison.Ordinal);
            Debug.Log($"[PlayerReconnect] [reconnect-tokens] rawJoinTokenEqual={rawEqual} (informational only, not used for accept)");

            LogJoinTokenSnapshot("reconnect-snapshot", snapshotToken, snapshotPayload);
            LogJoinTokenSnapshot("reconnect-incoming", incomingToken, incomingPayload);

            if (!rawEqual && snapshotToken != null && incomingToken != null)
                LogJoinTokenDiffHint(snapshotToken, incomingToken);
        }

        private static void LogSessionPayloadFields(string context, SessionTokenPayload payload)
        {
            if (payload == null)
            {
                Debug.Log($"[PlayerReconnect] [{context}] parsedPayload=(null)");
                return;
            }

            Debug.Log($"[PlayerReconnect] [{context}] parsedPayload playerId={payload.PlayerId} gameSessionId={payload.GameSessionId} nonce={payload.Nonce} issuedAt={payload.IssuedAtUtc:O} expiresAt={payload.ExpiresAtUtc:O}");
        }

        private static string DescribeJoinTokenParts(string joinToken)
        {
            if (string.IsNullOrEmpty(joinToken))
                return "empty";

            var parts = joinToken.Split('.', 2);
            if (parts.Length != 2)
                return $"invalidFormat(parts={parts.Length})";

            return $"payloadB64Len={parts[0].Length} sigB64Len={parts[1].Length} payloadB64Head={SafePrefix(parts[0], 24)} sigB64Head={SafePrefix(parts[1], 16)}";
        }

        private static void LogJoinTokenDiffHint(string snapshotToken, string incomingToken)
        {
            if (snapshotToken.Length != incomingToken.Length)
            {
                Debug.Log($"[PlayerReconnect] [reconnect-tokens] length differs snapshot={snapshotToken.Length} incoming={incomingToken.Length}");
                return;
            }

            int firstDiff = -1;
            for (int i = 0; i < snapshotToken.Length; i++)
            {
                if (snapshotToken[i] == incomingToken[i])
                    continue;
                firstDiff = i;
                break;
            }

            if (firstDiff < 0)
                return;

            int window = 32;
            int start = Math.Max(0, firstDiff - 8);
            Debug.Log($"[PlayerReconnect] [reconnect-tokens] firstCharDiffIndex={firstDiff} snapshotAround='{SafeSubstring(snapshotToken, start, window)}' incomingAround='{SafeSubstring(incomingToken, start, window)}'");
        }

        private static string SafePrefix(string value, int maxLen)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return value.Length <= maxLen ? value : value.Substring(0, maxLen) + "...";
        }

        private static string SafeSubstring(string value, int start, int maxLen)
        {
            if (string.IsNullOrEmpty(value) || start >= value.Length)
                return "";
            int len = Math.Min(maxLen, value.Length - start);
            return value.Substring(start, len);
        }
#endif
    }
}
