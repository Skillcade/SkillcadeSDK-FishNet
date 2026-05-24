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
            return !string.IsNullOrEmpty(playerId) && _graceSlots.ContainsKey(playerId);
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

            if (hubIntegrated)
            {
                if (string.IsNullOrEmpty(broadcastPlayerId) || !_graceSlots.TryGetValue(broadcastPlayerId, out slot))
                    return false;

#if UNITY_SERVER || UNITY_EDITOR
                if (!string.Equals(slot.JoinToken, joinToken, StringComparison.Ordinal))
                {
                    rejectReason = "join token does not match grace snapshot";
                    Debug.LogWarning($"[PlayerReconnect] Reject reconnect player={broadcastPlayerId}: {rejectReason}");
                    slot = null;
                    return false;
                }

                if (!TokenPayloadEquals(slot.TokenPayload, payload))
                {
                    rejectReason = "session token payload does not match grace snapshot";
                    Debug.LogWarning($"[PlayerReconnect] Reject reconnect player={broadcastPlayerId}: {rejectReason}");
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

#if UNITY_SERVER || UNITY_EDITOR
        private static bool TokenPayloadEquals(SessionTokenPayload a, SessionTokenPayload b)
        {
            if (a == null || b == null)
                return false;

            return string.Equals(a.PlayerId, b.PlayerId, StringComparison.Ordinal)
                   && string.Equals(a.GameSessionId, b.GameSessionId, StringComparison.Ordinal)
                   && string.Equals(a.Nonce, b.Nonce, StringComparison.Ordinal)
                   && a.IssuedAtUtc == b.IssuedAtUtc
                   && a.ExpiresAtUtc == b.ExpiresAtUtc;
        }
#endif
    }
}
