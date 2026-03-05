using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SkillcadeSDK.Connection;
using SkillcadeSDK.Replays;
using SkillcadeSDK.Replays.Components;
using SkillcadeSDK.Replays.Events;
using UnityEngine;
using VContainer;
using VContainer.Unity;

#if UNITY_SERVER || UNITY_EDITOR
using SkillcadeSDK.ServerValidation;
#endif

namespace SkillcadeSDK.FishNetAdapter.Replays.Rollback
{
    public class RollbackReplayWriteService : IInitializable, IDisposable
    {
        private struct FrameInfo
        {
            public int FrameId;
            public byte[] FrameData;
        }

        private const string FileName = "replay-new.replay";

        [Inject] private readonly ReplayWriteService _replayWriteService;
        [Inject] private readonly GameVersionConfig _gameVersionConfig;
        [Inject] private readonly IConnectionController _connectionController;

#if UNITY_SERVER || UNITY_EDITOR
        [Inject] private readonly ServerPayloadController _serverPayloadController;
#endif

        private readonly Dictionary<int, List<FrameInfo>> _replayDataForClients = new();
        private readonly List<ReplayEvent> _pendingEvents = new();

        private int _frameId;
        private bool _active;
        private DateTime _startTime;

        public void Initialize()
        {
            _replayWriteService.OnWriteStarted += OnWriteStarted;
            _replayWriteService.OnWriteFinished += OnWriteFinished;
            _replayWriteService.OnObjectRegistered += OnObjectRegistered;
            _replayWriteService.OnObjectUnregistered += OnObjectUnregistered;
        }

        public void Dispose()
        {
            _replayWriteService.OnWriteStarted -= OnWriteStarted;
            _replayWriteService.OnWriteFinished -= OnWriteFinished;
            _replayWriteService.OnObjectRegistered -= OnObjectRegistered;
            _replayWriteService.OnObjectUnregistered -= OnObjectUnregistered;
        }

        private void OnWriteStarted()
        {
            _active = true;
            _startTime = DateTime.UtcNow;
            _frameId = 0;
            _replayDataForClients.Clear();
            _pendingEvents.Clear();
        }

        private void OnWriteFinished(bool asServer)
        {
            if (asServer && _connectionController.ConnectionState != ConnectionState.SinglePlayer)
                WriteFile();

            _active = false;
            _replayDataForClients.Clear();
            _pendingEvents.Clear();
        }

        private void OnObjectRegistered(ReplayObjectHandler handler)
        {
            if (!_active) return;
            _pendingEvents.Add(new ObjectCreatedEvent(handler.ObjectId, handler.PrefabId, handler.transform.position));
        }

        private void OnObjectUnregistered(ReplayObjectHandler handler)
        {
            if (!_active) return;
            _pendingEvents.Add(new ObjectDestroyedEvent(handler.ObjectId, handler.PrefabId, handler.transform.position));
        }

        public void CaptureServerFrame(int tick)
        {
            if (!_active) return;
            CaptureFrame(0, tick);
        }

        public void CaptureRollbackFrame(int clientId, int tick)
        {
            if (!_active) return;
            CaptureFrame(clientId, tick);
        }

        private void CaptureFrame(int clientId, int tick)
        {
            using var stream = new MemoryStream();
            using var binaryWriter = new BinaryWriter(stream);

            var writer = new ReplayWriter(binaryWriter);
            writer.WriteInt(tick);

            writer.WriteInt(_pendingEvents.Count);
            foreach (var pendingEvent in _pendingEvents)
                writer.Write(pendingEvent);

            var activeObjects = _replayWriteService.ActiveObjects;
            writer.WriteInt(activeObjects.Count);
            foreach (var handler in activeObjects)
            {
                writer.WriteInt(handler.PrefabId);
                writer.WriteInt(handler.ObjectId);
                handler.Write(writer);
            }

            var frameData = stream.ToArray();

            if (!_replayDataForClients.TryGetValue(clientId, out var clientFrames))
            {
                clientFrames = new List<FrameInfo>();
                _replayDataForClients[clientId] = clientFrames;
            }

            clientFrames.Add(new FrameInfo
            {
                FrameId = _frameId,
                FrameData = frameData
            });

            _frameId++;
            _pendingEvents.Clear();
        }

        private void WriteFile()
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
                writer.Write(clientData.Key);
                writer.Write(clientData.Value.Count);
                Debug.Log($"[RollbackReplayWriteService] Client {clientData.Key} has {clientData.Value.Count} frames");
                var orderedFrames = clientData.Value.OrderBy(x => x.FrameId);
                foreach (var frameInfo in orderedFrames)
                {
                    writer.Write(frameInfo.FrameId);
                    writer.Write(frameInfo.FrameData.Length);
                    writer.Write(frameInfo.FrameData);
                }
            }

            Debug.Log($"[RollbackReplayWriteService] Rollback replay for {_replayDataForClients.Count} clients was written to {filePath}");
        }
    }
}
