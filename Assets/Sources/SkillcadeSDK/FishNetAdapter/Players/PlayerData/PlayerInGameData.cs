using FishNet.CodeGenerating;
using FishNet.Serializing;

namespace SkillcadeSDK.FishNetAdapter.Players
{
    [UseGlobalCustomSerializer]
    public class PlayerInGameData : IDataContainer
    {
        public const string Name = "PlayerInGameData";
        
        public bool IsReady;
        public bool InGame;
        
        public void Write(Writer writer)
        {
            writer.WriteBoolean(IsReady);
            writer.WriteBoolean(InGame);
        }

        public void Read(Reader reader)
        {
            IsReady = reader.ReadBoolean();
            InGame = reader.ReadBoolean();
        }

        public override string ToString()
        {
            return $"InGameData: {InGame}-{IsReady}";
        }

        public void SetToPlayer(FishNetPlayerData playerData)
        {
            playerData.SetData(Name, this);
        }

        public static bool TryGetFromPlayer(FishNetPlayerData playerData, out PlayerInGameData data)
        {
            return playerData.TryGetData(Name, out data);
        }

        public static void SetToPlayer(FishNetPlayerData playerData, PlayerInGameData data)
        {
            playerData.SetData(Name, data);
        }
    }
}