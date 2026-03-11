using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkillcadeSDK.Common.Players
{
    public interface IPlayerData<TData, TDataContainer> where TData : IPlayerData<TData, TDataContainer>
    {
        public event Action<TData> OnChanged;
        
        public int PlayerNetworkId { get; }
        
        public void SetData<T>(string key, T data) where T : TDataContainer;
        public bool TryGetData<T>(string key, out T data) where T : TDataContainer;
        
        public void AddPlayerObject<T>(T instance) where T : MonoBehaviour;
        public void RemovePlayerObject<T>(T instance) where T : MonoBehaviour;
        public bool TryGetPlayerObject<T>(out T playerObject) where T : MonoBehaviour;
        public IEnumerator<T> GetAllPlayerObjects<T>() where T : MonoBehaviour;
    }
}