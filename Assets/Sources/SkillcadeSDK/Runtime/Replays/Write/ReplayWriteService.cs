using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SkillcadeSDK.Common;
using SkillcadeSDK.Connection;
using SkillcadeSDK.Replays.Components;
using SkillcadeSDK.Replays.Events;
using UnityEngine;
using VContainer;
using VContainer.Unity;

#if UNITY_SERVER || UNITY_EDITOR
using SkillcadeSDK.ServerValidation;
#endif

namespace SkillcadeSDK.Replays
{
    // Inherit from this class in framework implementation to use framework time service
    // Call OnNetworkTick on every network tick if the game is running
    // Call StartWrite and FinishWrite on start and finish active game state
    public class ReplayWriteService : IInitializable
    {
        private struct FrameInfo
        {
            public int FrameId;
            public byte[] FrameData;
        }
        
        private const string FileName = "replay.replay";

        public event Action<int, byte[]> OnFrameReady;
        public event Action OnWriteStarted;
        public event Action<bool> OnWriteFinished;
        public event Action<ReplayEvent> OnEventAdded;
        public event Action<ReplayObjectHandler> OnObjectRegistered;
        public event Action<ReplayObjectHandler> OnObjectUnregistered;

        public IReadOnlyList<ReplayObjectHandler> ActiveObjects => _activeObjects;
        public bool IsActive => _active;

        [Inject] private readonly GameVersionConfig _gameVersionConfig;
        [Inject] private readonly IConnectionController _connectionController;
        [Inject] private readonly ISkillcadeConfig _skillcadeConfig;

#if UNITY_SERVER || UNITY_EDITOR
        [Inject] private readonly ServerPayloadController _serverPayloadController;
        [Inject] private readonly ReplaySendService _replaySendService;
#endif
        
        private readonly List<ReplayObjectHandler> _activeObjects = new();
        private readonly Dictionary<int, List<FrameInfo>> _replayDataForClients = new ();
        private readonly List<byte[]> _localFrameData = new();
        private readonly List<ReplayEvent> _pendingEvents = new();

        private int _frameId;
        private bool _active;
        private DateTime _startTime;

        public void Initialize()
        {
            ReplayDataObjectsRegistry.CollectDataObjectTypes();
        }

        public void AddEvent(ReplayEvent replayEvent)
        {
            _pendingEvents.Add(replayEvent);
            OnEventAdded?.Invoke(replayEvent);
        }

        public void RegisterObjectHandler(ReplayObjectHandler handler)
        {
            // Debug.Log($"[ReplayWriteService] Register object {handler.ObjectId} with prefab {handler.PrefabId}");
            _activeObjects.Add(handler);
            AddEvent(new ObjectCreatedEvent(handler.ObjectId, handler.PrefabId, handler.transform.position));
            OnObjectRegistered?.Invoke(handler);
        }

        public void UnregisterObjectHandler(ReplayObjectHandler handler)
        {
            // Debug.Log($"[ReplayWriteService] Unregister object {handler.ObjectId} with prefab {handler.PrefabId}");
            _activeObjects.Remove(handler);
            AddEvent(new ObjectDestroyedEvent(handler.ObjectId, handler.PrefabId, handler.transform.position));
            OnObjectUnregistered?.Invoke(handler);
        }

        public void AddFrameFromClient(int clientId, int frameId, byte[] frameData)
        {
            if (!_replayDataForClients.TryGetValue(clientId, out var clientFrames))
            {
                clientFrames = new List<FrameInfo>();
                _replayDataForClients[clientId] = clientFrames;
            }

            clientFrames.Add(new FrameInfo
            {
                FrameId = frameId,
                FrameData = frameData
            });
        }

        public void StartWrite()
        {
            if (_connectionController.ConnectionState == ConnectionState.SinglePlayer)
                return;
            
            _active = _skillcadeConfig.UseReplaysV1;
            _startTime = DateTime.UtcNow;
            _replayDataForClients.Clear();
            _localFrameData.Clear();
            OnWriteStarted?.Invoke();
        }

        public void FinishWrite(bool asServer)
        {
            OnWriteFinished?.Invoke(asServer);
            _active = false;
            if (!_skillcadeConfig.UseReplaysV1)
                return;
            
            if (asServer && _connectionController.ConnectionState != ConnectionState.SinglePlayer)
            {
                var filePath = Path.Combine(Application.streamingAssetsPath, FileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                var writer = new BinaryWriter(stream);

                var info = new ReplayInfo
                {
                    GameName = _gameVersionConfig.GameName,
                    GameVersion = _gameVersionConfig.GameVersion,
                    UnityVersion = _gameVersionConfig.UnityVersion,
                    StartTimestamp = (_startTime - DateTime.UnixEpoch).Ticks,
                    EndTimestamp = (DateTime.UtcNow - DateTime.UnixEpoch).Ticks
                };

#if UNITY_SERVER || UNITY_EDITOR
                if (_serverPayloadController.Payload != null)
                    info.MatchId = _serverPayloadController.Payload.MatchId;
#endif

                var infoJson = JsonConvert.SerializeObject(info);
                var infoJsonBytes = System.Text.Encoding.UTF8.GetBytes(infoJson);
                writer.Write(infoJsonBytes.Length);
                writer.Write(infoJsonBytes);
                writer.Write(_replayDataForClients.Count);

                foreach (var clientData in _replayDataForClients)
                {
                    writer.Write(clientData.Key); // client id
                    writer.Write(clientData.Value.Count); // frame count
                    Debug.Log($"[ReplayWriteService] Client {clientData.Key} has {clientData.Value.Count} frames");
                    var orderedFrames = clientData.Value.OrderBy(x => x.FrameId);
                    foreach (var frameInfo in orderedFrames)
                    {
                        writer.Write(frameInfo.FrameId);
                        writer.Write(frameInfo.FrameData.Length);
                        writer.Write(frameInfo.FrameData);
                    }
                }
                
                Debug.Log($"[ReplayService] Replay for {_replayDataForClients.Count} clients was written to {filePath}");
#if UNITY_SERVER || UNITY_EDITOR
                if (_serverPayloadController.Payload != null)
                    _replaySendService.SendReplayFile(filePath).DoNotAwait();
#endif
            }
            
            _replayDataForClients.Clear();
            _localFrameData.Clear();
            _activeObjects.Clear();
            _pendingEvents.Clear();
        }

        public void OnNetworkTick(int tick, bool isServer)
        {
            if (!_active) return;
            
            using var stream = new MemoryStream();
            using var binaryWriter = new BinaryWriter(stream);
            
            var writer = new ReplayWriter(binaryWriter);
            writer.WriteInt(tick);

            writer.WriteInt(_pendingEvents.Count);
            foreach (var pendingEvent in _pendingEvents)
            {
                // Debug.Log($"[ReplayWriteService] Write event {pendingEvent.GetType().Name} to replay");
                writer.Write(pendingEvent);
            }
            
            _pendingEvents.Clear();

            writer.WriteInt(_activeObjects.Count);
            foreach (var objectHandler in _activeObjects)
            {
                // Debug.Log($"[ReplayWriteService] Write object {objectHandler.ObjectId} with prefab {objectHandler.PrefabId} to replay");
                writer.WriteInt(objectHandler.PrefabId);
                writer.WriteInt(objectHandler.ObjectId);
                objectHandler.Write(writer);
            }

            // Debug.Log($"[ReplayWriteService] Write {_activeObjects.Count} objects to replay");

            var frameData = stream.ToArray();
            int frameId = _localFrameData.Count;
            _localFrameData.Add(frameData);
            
            // Debug.Log($"[ReplayWriteService] Frame {frameId} on tick {tick} write {frameData.Length} bytes, as server: {isServer}");
            
            if (isServer)
                AddFrameFromClient(0, frameId, frameData);
            else
                OnFrameReady?.Invoke(frameId, frameData);
        }
    }
}