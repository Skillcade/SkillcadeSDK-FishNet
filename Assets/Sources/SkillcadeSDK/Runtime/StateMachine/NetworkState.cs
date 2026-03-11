using System;
using Newtonsoft.Json;
using UnityEngine;

namespace SkillcadeSDK.StateMachine
{
    public interface INetworkState<TStateType> where TStateType : Enum
    {
        TStateType Type { get; }
        internal void SetStateMachine(NetworkStateMachine<TStateType> stateMachine);
        internal void CleanupStateMachine();

        public void OnEnter(TStateType prevState);
        public void OnEnterWithData(TStateType prevState, string data);
        public void OnExit(TStateType nextState);

        public void Update();
    }
    
    public abstract class NetworkState<TStateType> : INetworkState<TStateType>
        where TStateType : Enum
    {
        public abstract TStateType Type { get; }

        protected bool IsServer => StateMachine?.IsServer ?? false;
        protected bool IsClient => StateMachine?.IsClient ?? false;
        
        protected NetworkStateMachine<TStateType> StateMachine { get; private set; }

        void INetworkState<TStateType>.SetStateMachine(NetworkStateMachine<TStateType> stateMachine) => StateMachine = stateMachine;
        void INetworkState<TStateType>.CleanupStateMachine() => StateMachine = null;

        public virtual void OnEnter(TStateType prevState) { }
        public virtual void OnExit(TStateType nextState) { }
        public virtual void OnEnterWithData(TStateType prevState, string data) { }
        
        public virtual void Update() { }
    }

    public abstract class NetworkState<TStateType, TDataType> : NetworkState<TStateType>
        where TStateType : Enum
    {
        public sealed override void OnEnter(TStateType prevState)
        {
            Debug.LogError($"[NetworkState] Called OnEnter without data on state {Type} which requires data {typeof(TDataType).Name}");
        }

        public sealed override void OnEnterWithData(TStateType prevState, string data)
        {
            TDataType typedData = JsonConvert.DeserializeObject<TDataType>(data);
            OnEnter(prevState, typedData);
        }
        
        protected virtual void OnEnter(TStateType prevState, TDataType data) { }
    }
}