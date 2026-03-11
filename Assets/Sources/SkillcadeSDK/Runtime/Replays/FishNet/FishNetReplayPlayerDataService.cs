using System.Collections.Generic;
using SkillcadeSDK.Replays.GUI;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.Replays
{
    public class FishNetReplayPlayerDataService
    {
        public class PlayerData
        {
            public int PlayerObjectId;
            public int PlayerNetworkId;
            public string Nickname;
            public string PlayerId;
            public int Ping;
        }
        
        public IReadOnlyDictionary<int, PlayerData> PlayersData => _playersData;

        [Inject] private readonly IObjectResolver _objectResolver;
        
        private readonly Dictionary<int, PlayerData> _playersData = new();
        
        public void SetPlayerData(int playerNetworkId, int playerObjectId, string nickname, string playerId)
        {
            if (_playersData.ContainsKey(playerNetworkId))
            {
                _playersData[playerNetworkId].PlayerObjectId = playerObjectId;
                _playersData[playerNetworkId].PlayerId = playerId;
                _playersData[playerNetworkId].Nickname = nickname;
            }
            else
            {
                var data = new PlayerData
                {
                    PlayerNetworkId = playerNetworkId,
                    PlayerObjectId = playerObjectId,
                    Nickname = nickname,
                    PlayerId = playerId
                };
                _playersData.Add(playerNetworkId, data);
            }
            
            if (_objectResolver.TryResolve(out ReplayInfoPanel infoPanel))
                infoPanel.SetPlayerNickname(playerNetworkId, nickname);
        }

        public void SetPlayerPing(int playerObjectId, int ping)
        {
            foreach (var playerData in _playersData)
            {
                if (playerData.Value.PlayerObjectId == playerObjectId)
                {
                    playerData.Value.Ping = ping;
                    if (_objectResolver.TryResolve(out ReplayInfoPanel infoPanel))
                        infoPanel.SetPlayerPing(playerData.Key, ping);
                    
                    return;
                }
            }
        }
    }
}