using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.Replays.Components
{
    public abstract class ReplayObjectHandler : MonoBehaviour
    {
        public int PrefabId => _prefabId;
        public int ObjectId => _isReplaying ? _replayObjectId : RuntimeNetworkObjectId;
        public int WorldId => _replayWorldId;
        public bool IsReplaying => _isReplaying;
        
        protected abstract int RuntimeNetworkObjectId { get; }

        [SerializeField] private bool _isReplaying;
        [SerializeField] private int _replayObjectId;
        [SerializeField] private int _replayWorldId;

        [SerializeField] private int _prefabId;
        [SerializeField] private GameObject _targetObject;
        [SerializeField] private GameObject _graphicsObject;

        [Inject] private readonly ReplayWriteService _replayWriteService;
        
        private Dictionary<Type, IReplayComponent> _replayComponents;

        protected virtual void Awake()
        {
            var replayComponents = GetComponentsInChildren<IReplayComponent>();
            _replayComponents = new Dictionary<Type, IReplayComponent>();
            foreach (var component in replayComponents)
            {
                _replayComponents.Add(component.GetType(), component);
            }
        }

        public void InitializeReplay(int objectId, int worldId)
        {
            _isReplaying = true;
            _replayObjectId = objectId;
            _replayWorldId = worldId;
        }

        protected void Register()
        {
            this.InjectToMe();
            if (!_isReplaying)
                _replayWriteService.RegisterObjectHandler(this);
        }

        protected void Unregister()
        {
            if (!_isReplaying)
                _replayWriteService.UnregisterObjectHandler(this);
        }

        public virtual void SetVisible(float transparency)
        {
            if (_graphicsObject == null)
            {
                Debug.LogError($"[ReplayObjectHandler] Graphics object on {gameObject.name} is null");
                return;
            }

            bool visible = transparency >= 1f;
            Debug.Log($"[ReplayObjectHandler] Set object {ObjectId} in world {WorldId} visible: {visible}");
            _graphicsObject.SetActive(visible);
        }

        public void Write(ReplayWriter writer)
        {
            writer.WriteUshort((ushort)_replayComponents.Count);
            foreach (var component in _replayComponents.Values)
            {
                writer.Write(component);
            }
        }

        public void Read(ReplayReader reader)
        {
            Debug.Log($"[ReplayObjectHandler] Read object {ObjectId} in world {WorldId}, active: {_targetObject.activeSelf}");
            _targetObject.SetActive(true);
            int componentsCount = reader.ReadUshort();
            for (int i = 0; i < componentsCount; i++)
            {
                int id = reader.ReadUshort();
                int size = reader.ReadUshort();
                if (!ReplayDataObjectsRegistry.IdToType.TryGetValue(id, out var type))
                {
                    Debug.LogError($"[ReplayObjectHandler] Can't get replay component type for id {id}");
                    reader.SkipBytes(size);
                    continue;
                }

                if (!_replayComponents.TryGetValue(type, out var component))
                {
                    Debug.LogError($"[ReplayObjectHandler] Component with type {type.Name} not registered!");
                    reader.SkipBytes(size);
                    continue;
                }
                
                component.Read(reader);
            }
        }
    }
}