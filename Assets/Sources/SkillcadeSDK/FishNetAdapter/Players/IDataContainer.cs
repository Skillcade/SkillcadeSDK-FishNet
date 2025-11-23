using FishNet.Serializing;

namespace SkillcadeSDK.FishNetAdapter.Players
{
    public interface IDataContainer
    {
        public void Write(Writer writer);
        public void Read(Reader reader);
    }
}