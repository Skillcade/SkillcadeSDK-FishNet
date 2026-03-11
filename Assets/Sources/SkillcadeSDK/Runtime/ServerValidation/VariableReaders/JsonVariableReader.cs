#if UNITY_SERVER || UNITY_EDITOR
using System;
using Newtonsoft.Json;
using UnityEngine;

namespace SkillcadeSDK.ServerValidation
{
    public class JsonVariableReader<T> : IServerVariableReader
    {
        public object Read(string value)
        {
            try
            {
                var result = JsonConvert.DeserializeObject<T>(value);
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonVariableReader] Can't deserialize {value} to type {typeof(T).Name}: {e}");
                throw;
            }
        }
    }
}
#endif