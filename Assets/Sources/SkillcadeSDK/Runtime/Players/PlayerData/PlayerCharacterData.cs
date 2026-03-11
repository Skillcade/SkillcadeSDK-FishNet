using FishNet.CodeGenerating;
using FishNet.Serializing;

namespace SkillcadeSDK.FishNetAdapter.Players
{
    [UseGlobalCustomSerializer]
    public class PlayerCharacterData : IDataContainer
    {
        public const string Name = "PlayerCharacterData";
        
        public string CharacterName;
        
        public void Write(Writer writer)
        {
            writer.Write(CharacterName);
        }

        public void Read(Reader reader)
        {
            CharacterName = reader.ReadStringAllocated();
        }

        public void SetToPlayer(FishNetPlayerData playerData)
        {
            playerData.SetData(Name, this);
        }

        public static bool TryGetFromPlayer(FishNetPlayerData playerData, out PlayerCharacterData data)
        {
            return playerData.TryGetData(Name, out data);
        }

        public static void SetToPlayer(FishNetPlayerData playerData, PlayerCharacterData data)
        {
            playerData.SetData(Name, data);
        }
    }
}