#if UNITY_SERVER || UNITY_EDITOR
using System;
using JetBrains.Annotations;

namespace SkillcadeSDK.ServerValidation
{
    [UsedImplicitly]
    public class DateTimeVariableReader : IServerVariableReader
    {
        public object Read(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("[DateTimeVariableReader] Provided value is null or empty");
            
            return DateTime.Parse(value);
        }
    }
}
#endif