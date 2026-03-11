using System.Collections.Generic;

namespace SkillcadeSDK.Common.Players
{
    public interface IPlayersController<TData, TDataContainer> where TData : IPlayerData<TData, TDataContainer>
    {
        public delegate void PlayerDataEventHandler(int playerId, TData data);
        
        public event PlayerDataEventHandler OnPlayerAdded;
        public event PlayerDataEventHandler OnPlayerDataUpdated;
        public event PlayerDataEventHandler OnPlayerRemoved;
        
        public int LocalPlayerId { get; }
        
        public bool TryGetPlayerData(int playerId, out TData data);
        public IEnumerable<TData> GetAllPlayersData();
    }
}