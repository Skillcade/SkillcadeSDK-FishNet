using System.Collections.Generic;
using UnityEngine;

namespace SkillcadeSDK.FishNetAdapter.Authenticator
{
    public sealed class AuthenticatedPlayerData
    {
        public int ClientId;
        public string PlayerId;
        public string Nickname;
        public string CharacterName;
    }

    public sealed class AuthenticatedPlayerDataStore
    {
        private readonly Dictionary<int, AuthenticatedPlayerData> _dataByClientId = new();
        private readonly HashSet<string> _knownPlayerIds = new();
        private readonly HashSet<string> _reservedPlayerIds = new();

        public bool CanAcceptPlayer(string playerId, int targetPlayerCount)
        {
            bool result;
            if (targetPlayerCount <= 0)
                result = true;
            else if (string.IsNullOrEmpty(playerId))
                result = _knownPlayerIds.Count < targetPlayerCount;
            else if (_knownPlayerIds.Contains(playerId))
                result = false;
            else
                result = _knownPlayerIds.Count < targetPlayerCount;

            if (!result && !string.IsNullOrEmpty(playerId) && _knownPlayerIds.Contains(playerId))
                Debug.Log($"[PlayerAuth] CanAcceptPlayer({playerId})=false: duplicate (reserved={_reservedPlayerIds.Contains(playerId)}) knownCount={_knownPlayerIds.Count} target={targetPlayerCount}");
            else
                Debug.Log($"[PlayerAuth] CanAcceptPlayer({playerId})={result} knownCount={_knownPlayerIds.Count} reservedCount={_reservedPlayerIds.Count} target={targetPlayerCount}");

            return result;
        }

        public bool IsPlayerKnown(string playerId)
        {
            return !string.IsNullOrEmpty(playerId) && _knownPlayerIds.Contains(playerId);
        }

        public void Store(AuthenticatedPlayerData data)
        {
            Debug.Log($"[AuthenticatedPlayerDataStore] Store player data {data.ClientId} - {data.PlayerId}");
            _dataByClientId[data.ClientId] = data;

            if (!string.IsNullOrEmpty(data.PlayerId))
            {
                if (_knownPlayerIds.Add(data.PlayerId))
                    Debug.Log("[AuthenticatedPlayerDataStore] Stored player id in known players");
                else
                    Debug.Log("[AuthenticatedPlayerDataStore] Already knew this player");
            }
        }

        public bool TryGetByClientId(int clientId, out AuthenticatedPlayerData data)
        {
            return _dataByClientId.TryGetValue(clientId, out data);
        }

        /// <summary>
        /// Holds a PlayerId in _knownPlayerIds across a disconnect so the slot is preserved
        /// for a grace-period reconnect. Released via <see cref="ReleasePlayerId"/>.
        /// </summary>
        public void ReservePlayerId(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
                return;

            if (_reservedPlayerIds.Add(playerId))
                Debug.Log($"[AuthenticatedPlayerDataStore] [PlayerReconnect] Reserve playerId={playerId} for reconnect grace");
            _knownPlayerIds.Add(playerId);
        }

        public void ReleasePlayerId(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
                return;

            if (_reservedPlayerIds.Remove(playerId))
                Debug.Log($"[AuthenticatedPlayerDataStore] [PlayerReconnect] Release playerId={playerId} from reconnect grace");

            // Only drop from known players if no live client still owns it.
            foreach (var entry in _dataByClientId.Values)
            {
                if (entry.PlayerId == playerId)
                    return;
            }
            _knownPlayerIds.Remove(playerId);
        }

        public void RemoveClient(int clientId)
        {
            Debug.Log($"[AuthenticatedPlayerDataStore] Remove client {clientId}");
            if (!_dataByClientId.Remove(clientId, out var removed))
            {
                Debug.Log("[AuthenticatedPlayerDataStore] Client not saved in data");
                return;
            }

            if (string.IsNullOrEmpty(removed.PlayerId))
            {
                Debug.Log("[AuthenticatedPlayerDataStore] player id is null or empty");
                return;
            }

            foreach (var entry in _dataByClientId.Values)
            {
                if (entry.PlayerId == removed.PlayerId)
                {
                    Debug.Log($"[AuthenticatedPlayerDataStore] Found duplicate player id {removed.PlayerId} in data - dont remove from known players");
                    return;
                }
            }

            if (_reservedPlayerIds.Contains(removed.PlayerId))
            {
                Debug.Log($"[AuthenticatedPlayerDataStore] [PlayerReconnect] Keep player {removed.PlayerId} in known players (reserved for reconnect grace)");
                return;
            }

            Debug.Log($"[AuthenticatedPlayerDataStore] Remove {removed.PlayerId} from known players");
            _knownPlayerIds.Remove(removed.PlayerId);
        }
    }
}
