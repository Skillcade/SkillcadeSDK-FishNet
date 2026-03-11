#if UNITY_SERVER || UNITY_EDITOR
using JetBrains.Annotations;

namespace SkillcadeSDK.ServerValidation
{
    [UsedImplicitly]
    public interface IServerVariableReader
    {
        public object Read(string value);
    }
}
#endif