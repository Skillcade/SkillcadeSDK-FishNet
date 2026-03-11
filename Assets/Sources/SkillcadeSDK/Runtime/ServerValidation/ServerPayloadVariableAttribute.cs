#if UNITY_SERVER || UNITY_EDITOR
using System;

namespace SkillcadeSDK.ServerValidation
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ServerPayloadVariableAttribute : Attribute
    {
        public string Name { get; set; }
        public Type ReaderType { get; set; }

        public ServerPayloadVariableAttribute(string name)
        {
            Name = name;
            ReaderType = null;
        }

        public ServerPayloadVariableAttribute(string name, Type readerType)
        {
            Name = name;
            ReaderType = readerType;
        }
    }
}
#endif