using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SkillcadeSDK.Connection;
using SkillcadeSDK.Replays;
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

        [Inject] private readonly ReplayWriteService _replayWriteService;
        [Inject] private readonly GameVersionConfig _gameVersionConfig;
        [Inject] private readonly IConnectionController _connectionController;

#if UNITY_SERVER || UNITY_EDITOR
        [Inject] private readonly ServerPayloadController _serverPayloadController;
        [Inject] private readonly ReplaySendService _replaySendService;
#endif

        private readonly Dictionary<int, List<(int, FrameInfo)>> _replayDataForClients = new();
        private readonly Dictionary<int, List<ReplayEvent>> _eventsByTick = new();
        private readonly List<ReplayEvent> _pendingEvents = new();

        private readonly List<ReplayEvent> _eventsFrameBuffer = new();

        private bool _active;
        private DateTime _startTime;
        private int _currentTick;

        public void Initialize()
        {
            _replayWriteService.OnWriteStarted += OnWriteStarted;
            _replayWriteService.OnWriteFinished += OnWriteFinished;
            _replayWriteService.OnEventAdded += OnEventAdded;
        }

        public void Dispose()
        {
            _replayWriteService.OnWriteStarted -= OnWriteStarted;
            _replayWriteService.OnWriteFinished -= OnWriteFinished;
            _replayWriteService.OnEventAdded -= OnEventAdded;
        }

        private void OnWriteStarted()
        {
            // Debug.Log("[RollbackReplayWriteService] Write started");
            _active = true;
            _startTime = DateTime.UtcNow;
            _replayDataForClients.Clear();
            _eventsByTick.Clear();
        }

        private void OnWriteFinished(bool asServer)
        {
            // Debug.Log("[RollbackReplayWriteService] Write finished");
            if (asServer && _connectionController.ConnectionState != ConnectionState.SinglePlayer)
                WriteFile();

            _active = false;
            _replayDataForClients.Clear();
            _eventsByTick.Clear();
        }

        private void OnEventAdded(ReplayEvent evt)
        {
            if (_active)
                AddEventAtCurrentTick(evt);
            else
                _pendingEvents.Add(evt);
        }

        private void AddEventAtCurrentTick(ReplayEvent evt)
        {
            if (!_eventsByTick.TryGetValue(_currentTick, out var events))
            {
                events = new List<ReplayEvent>();
                _eventsByTick[_currentTick] = events;
            }
            events.Add(evt);
            // Debug.Log($"[RollbackReplayWriteService] Add event {evt.GetType().Name} at tick {_currentTick}");
        }

        public void SetCurrentTick(int tick)
        {
            // if (_active)
            //     Debug.Log($"[RollbackReplayWriteService] set current tick to {tick}");
            _currentTick = tick;
        }

        public void CaptureServerFrame(int tick)
        {
            if (_active)
            {
                // Debug.Log($"[RollbackReplayWriteService] Capture server frame at {tick}");
                CaptureFrame(0, tick);
            }
        }

        public void CaptureRollbackFrame(int clientId, int tick)
        {
            if (_active)
                CaptureFrame(clientId, tick);
        }

        private void CaptureFrame(int clientId, int tick)
        {
            using var stream = new MemoryStream();
            using var binaryWriter = new BinaryWriter(stream);

            // Debug.Log($"[RollbackReplayWriteService] [{tick}] Capture client {clientId} frame");
            
            var writer = new ReplayWriter(binaryWriter);
            writer.WriteInt(tick);

            _eventsFrameBuffer.Clear();
            if (_pendingEvents.Count > 0)
            {
                bool writePendingEvents = !_replayDataForClients.TryGetValue(clientId, out var frames) || frames.Count == 0;
                if (writePendingEvents)
                {
                    // Debug.Log($"[RollbackReplayWriteService] [{tick}] got {_pendingEvents.Count} pending events for first frame client {clientId}");
                    _eventsFrameBuffer.AddRange(_pendingEvents);
                }
            }

            if (_eventsByTick.TryGetValue(tick, out var events))
            {
                // Debug.Log($"[RollbackReplayWriteService] [{tick}] Got {events.Count} events for client {clientId}");
                _eventsFrameBuffer.AddRange(events);
            }

            if (_eventsFrameBuffer.Count > 0)
            {
                writer.WriteInt(_eventsFrameBuffer.Count);
                foreach (var pendingEvent in _eventsFrameBuffer)
                {
                    // Debug.Log($"[RollbackReplayWriteService] [{tick}] Write event {pendingEvent.GetType().Name} to replay for client {clientId}");
                    writer.Write(pendingEvent);
                }
            }
            else
            {
                writer.WriteInt(0);
            }

            var activeObjects = _replayWriteService.ActiveObjects;
            writer.WriteInt(activeObjects.Count);
            foreach (var handler in activeObjects)
            {
                // Debug.Log($"[RollbackReplayWriteService] [{tick}] Write object {handler.ObjectId} with prefab {handler.PrefabId} to replay");
                writer.WriteInt(handler.PrefabId);
                writer.WriteInt(handler.ObjectId);
                handler.Write(writer);
            }

            // Debug.Log($"[RollbackReplayWriteService] [{tick}] Write {activeObjects.Count} objects to replay for client {clientId}");
            var frameData = stream.ToArray();
            var currentFrame = new FrameInfo
            {
                FrameId = -1,
                FrameData = frameData
            };
            
            if (!_replayDataForClients.TryGetValue(clientId, out var clientFrames))
            {
                clientFrames = new List<(int, FrameInfo)>();
                _replayDataForClients[clientId] = clientFrames;
            }


            bool inserted = false;
            for (int i = 0; i < clientFrames.Count; i++)
            {
                var (frameTick, _) = clientFrames[i];
                if (frameTick > tick)
                {
                    // Debug.Log($"[RollbackReplayWriteService] [{tick}] Insert frame in {i}, next tick: {frameTick}, client {clientId}");
                    clientFrames.Insert(i, (tick, currentFrame));
                    inserted = true;
                    break;
                }
            }

            if (!inserted)
            {
                clientFrames.Add((tick, currentFrame));
                // Debug.Log($"[RollbackReplayWriteService] [{tick}] Add frame as {clientFrames.Count}, client {clientId}");
            }

            // int frameId = 0;
            for (int i = 0; i < clientFrames.Count; i++)
            {
                var (frameTick, frame) = clientFrames[i];
                frame.FrameId = i;
                // frameId = i;
                clientFrames[i] = (frameTick, frame);
            }
            
            // Debug.Log($"[RollbackReplayWriteService] Frame {frameId} on tick {tick} write {frameData.Length} bytes for client {clientId}, total frames: {clientFrames.Count}");
        }

        private void WriteFile()
        {
            var filePath = Path.Combine(Application.streamingAssetsPath, ReplayWriteService.GetFileName());
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
                var orderedFrames = clientData.Value.Select(x => x.Item2);
                foreach (var frameInfo in orderedFrames)
                {
                    writer.Write(frameInfo.FrameId);
                    writer.Write(frameInfo.FrameData.Length);
                    writer.Write(frameInfo.FrameData);
                }
            }

            Debug.Log($"[RollbackReplayWriteService] Rollback replay for {_replayDataForClients.Count} clients was written to {filePath}");
#if UNITY_SERVER || UNITY_EDITOR
            if (_serverPayloadController.Payload != null)
                _replaySendService.SendReplayFile(filePath).DoNotAwait();
#endif
        }
    }
}
