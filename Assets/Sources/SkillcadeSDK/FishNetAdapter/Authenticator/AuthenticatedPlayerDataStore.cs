using System.Collections.Generic;

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

            return _knownPlayerIds.Contains(playerId) || _knownPlayerIds.Count < targetPlayerCount;
        }

        public void Store(AuthenticatedPlayerData data)
        {
            _dataByClientId[data.ClientId] = data;

            if (!string.IsNullOrEmpty(data.PlayerId))
                _knownPlayerIds.Add(data.PlayerId);
        }

        public bool TryGetByClientId(int clientId, out AuthenticatedPlayerData data)
        {
            return _dataByClientId.TryGetValue(clientId, out data);
        }

        public void RemoveClient(int clientId)
        {
            if (!_dataByClientId.TryGetValue(clientId, out var removed))
                return;

            _dataByClientId.Remove(clientId);

            if (string.IsNullOrEmpty(removed.PlayerId))
                return;

            foreach (var entry in _dataByClientId.Values)
            {
                if (entry.PlayerId == removed.PlayerId)
                    return;
            }

            _knownPlayerIds.Remove(removed.PlayerId);
        }
    }
}
