using FishNet.CodeGenerating;
using FishNet.Serializing;

namespace SkillcadeSDK.FishNetAdapter.Players
{
    [UseGlobalCustomSerializer]
    public interface IDataContainer
    {
        public void Write(Writer writer);
        public void Read(Reader reader);
    }
}