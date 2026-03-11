using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SkillcadeSDK.Connection;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SkillcadeSDK.StateMachine
{
    public abstract class NetworkStateMachine<TStateType> : IInitializable, ITickable, IDisposable
        where TStateType : Enum
    {
        public TStateType CurrentStateType => _stateMachineSyncer.CurrentState.GetType<TStateType>();

        public bool IsServer => _connectionController.IsServer || _stateMachineSyncer.IsServer;
        public bool IsClient => _connectionController.IsClient || _stateMachineSyncer.IsClient;

        [Inject] protected readonly IConnectionController _connectionController;
        [Inject] private readonly INetworkStateMachineSyncer _stateMachineSyncer;
        [Inject] private readonly IReadOnlyList<INetworkState<TStateType>> _states;
        
        private readonly EqualityComparer<TStateType> _comparer = EqualityComparer<TStateType>.Default;

        private INetworkState<TStateType> _currentState;
        private Dictionary<TStateType, INetworkState<TStateType>> _typedStates;

        public virtual void Initialize()
        {
            _typedStates = _states.ToDictionary(x => x.Type);
            foreach (var state in _states)
            {
                state.SetStateMachine(this);
            }
            
            _stateMachineSyncer.OnNetworkStart += OnNetworkStart;
            _stateMachineSyncer.OnNetworkStop += OnNetworkStop;
            _stateMachineSyncer.StateChanged += OnStateChanged;
        }

        protected virtual void OnNetworkStart()
        {
            if (IsServer)
                return;
            
            var currentStateType = CurrentStateType;
            if (!_typedStates.TryGetValue(currentStateType, out var initState))
            {
                Debug.LogError($"[NetworkStateMachine] Error initializing first state on client: {currentStateType}, no state found");
                return;
            }
                
            if (!string.IsNullOrWhiteSpace(_stateMachineSyncer.CurrentState.JsonData))
                initState.OnEnterWithData(currentStateType, _stateMachineSyncer.CurrentState.JsonData);
            else
                initState.OnEnter(currentStateType);

            _currentState = initState;
        }

        protected virtual void OnNetworkStop() { }

        public virtual void Dispose()
        {
            foreach (var state in _states)
            {
                state.CleanupStateMachine();
            }
            
            _stateMachineSyncer.OnNetworkStart -= OnNetworkStart;
            _stateMachineSyncer.OnNetworkStop -= OnNetworkStop;
            _stateMachineSyncer.StateChanged -= OnStateChanged;
        }

        public void SetStateServer(TStateType stateType)
        {
            if (!_stateMachineSyncer.IsServer)
                return;
            
            Debug.Log($"[NetworkStateMachine] Set state server: {stateType}");
            if (!_typedStates.TryGetValue(stateType, out var nextState))
            {
                Debug.LogError($"[NetworkStateMachine] Trying to set not registered state: {stateType}");
                return;
            }
            
            SetStateServerInternal(nextState, null);
        }

        public void SetStateServer<T>(TStateType stateType, T data)
        {
            if (!_stateMachineSyncer.IsServer)
                return;
            
            Debug.Log($"[NetworkStateMachine] Set state server with data: {stateType}");
            if (!_typedStates.TryGetValue(stateType, out var nextState))
            {
                Debug.LogError($"[NetworkStateMachine] Trying to set not registered state: {stateType}");
                return;
            }

            if (nextState is not NetworkState<TStateType, T>)
            {
                var nextStateType = nextState.GetType();
                var arguments = nextStateType.GenericTypeArguments;
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (arguments.Length == 1)
                    Debug.LogError($"[NetworkStateMachine] Trying to set state {stateType} with data type {typeof(T).Name}, but state doesn't receive any data");
                else
                    Debug.LogError($"[NetworkStateMachine] Trying to set state {stateType} with wrong data type, provided: {typeof(T).Name}, required: {arguments[1].Name}");
                
                return;
            }

            var jsonData = JsonConvert.SerializeObject(data);
            SetStateServerInternal(nextState, jsonData);
        }

        private void SetStateServerInternal(INetworkState<TStateType> nextState, string jsonData)
        {
            if (_comparer.Equals(nextState.Type, CurrentStateType))
            {
                Debug.LogError($"[NetworkStateMachine] Trying to set same state: {nextState.Type}");
                return;
            }
            
            _stateMachineSyncer.SetStateOnServer(new StateData().WithData(jsonData).WithType(nextState.Type));
        }

        private void OnStateChanged(StateData prev, StateData next)
        {
            var prevType = prev.GetType<TStateType>();
            var nextType = next.GetType<TStateType>();

            if (_currentState != null && _comparer.Equals(_currentState.Type, nextType))
                return;
            
            if (!string.IsNullOrWhiteSpace(next.JsonData))
                Debug.Log($"[NetworkStateMachine] On state changed from {prevType} to {nextType}, data: {next.JsonData}");
            else
                Debug.Log($"[NetworkStateMachine] On state changed from {prevType} to {nextType}");
            
            if (!_typedStates.TryGetValue(nextType, out var nextState))
            {
                Debug.LogError($"[NetworkStateMachine] Received state changed on not registered state: {next.Type}");
                return;
            }
            
            if (_typedStates.TryGetValue(prevType, out var prevState))
                prevState.OnExit(nextType);

            _currentState = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(next.JsonData))
                    nextState.OnEnterWithData(prevType, next.JsonData);
                else
                    nextState.OnEnter(prevType);

                _currentState = nextState;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkStateMachine] Error on entering state: {next.Type}: {e}");
            }
        }

        public virtual void Tick()
        {
            _currentState?.Update();
        }
    }
}