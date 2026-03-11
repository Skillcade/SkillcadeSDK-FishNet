using System;
using Unity.Collections.LowLevel.Unsafe;

namespace SkillcadeSDK.StateMachine
{
    public struct StateData
    {
        public int Type;
        public string JsonData;
        
        public T GetType<T>() where T : Enum
        {
            return UnsafeUtility.As<int, T>(ref Type);
        }
        
        public StateData WithType<T>(T value) where T : Enum
        {
            Type = UnsafeUtility.As<T, int>(ref value);
            return this;
        }
        
        public StateData WithData(string value)
        {
            JsonData = value;
            return this;
        }
    }
}