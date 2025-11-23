using FishNet.Serializing;
using SkillcadeSDK.FishNetAdapter.Players;

namespace SkillcadeSDK.FishNetAdapter.PingService
{
    public class PlayerPingData : IDataContainer
    {
        private const string Name = "PlayerPingData";
        
        public int PingInMs;
        
        public void Write(Writer writer)
        {
            writer.Write(PingInMs);
        }

        public void Read(Reader reader)
        {
            PingInMs = reader.ReadInt32();
        }
        
        public void SetToPlayer(FishNetPlayerData playerData)
        {
            playerData.SetData(Name, this);
        }

        public static bool TryGetFromPlayer(FishNetPlayerData playerData, out PlayerPingData data)
        {
            return playerData.TryGetData(Name, out data);
        }

        public static void SetToPlayer(FishNetPlayerData playerData, PlayerPingData data)
        {
            playerData.SetData(Name, data);
        }
    }
}