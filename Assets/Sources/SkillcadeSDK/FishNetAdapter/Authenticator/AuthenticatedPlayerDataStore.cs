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

        public bool CanAcceptPlayer(string playerId, int targetPlayerCount)
        {
            if (targetPlayerCount <= 0)
                return true;

            if (string.IsNullOrEmpty(playerId))
                return _knownPlayerIds.Count < targetPlayerCount;

            if (_knownPlayerIds.Contains(playerId))
            {
                Debug.Log($"[AuthenticatedPlayerDataStore] Duplicate connection from player {playerId}");
                return false;
            }
            
            return _knownPlayerIds.Count < targetPlayerCount;
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

            Debug.Log($"[AuthenticatedPlayerDataStore] Remove {removed.PlayerId} from known players");
            _knownPlayerIds.Remove(removed.PlayerId);
        }
    }
}
