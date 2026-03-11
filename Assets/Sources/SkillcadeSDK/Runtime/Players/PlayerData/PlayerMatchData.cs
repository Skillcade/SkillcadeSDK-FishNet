using FishNet.CodeGenerating;
using FishNet.Serializing;

namespace SkillcadeSDK.FishNetAdapter.Players
{
    [UseGlobalCustomSerializer]
    public class PlayerMatchData : IDataContainer
    {
        public const string Name = "PlayerMatchData";
        
        public string Nickname;
        public string PlayerId;
        
        public void Write(Writer writer)
        {
            writer.Write(Nickname);
            writer.Write(PlayerId);
        }

        public void Read(Reader reader)
        {
            Nickname = reader.ReadStringAllocated();
            PlayerId = reader.ReadStringAllocated();
        }

        public void SetToPlayer(FishNetPlayerData playerData)
        {
            playerData.SetData(Name, this);
        }

        public static bool TryGetFromPlayer(FishNetPlayerData playerData, out PlayerMatchData data)
        {
            return playerData.TryGetData(Name, out data);
        }

        public static void SetToPlayer(FishNetPlayerData playerData, PlayerMatchData data)
        {
            playerData.SetData(Name, data);
        }
    }
}