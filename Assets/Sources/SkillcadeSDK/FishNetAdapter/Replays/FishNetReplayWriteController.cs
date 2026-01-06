using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using SkillcadeSDK.Replays;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.Replays
{
    public class FishNetReplayWriteController : NetworkBehaviour
    {
        [SerializeField] private int _framesToSend;
        
        [Inject] private readonly ReplayWriteService _replayWriteService;

        private List<FishNetFrameDataBroadcast> _framesBuffer;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            TimeManager.OnPostTick += SimulateTick;
            _replayWriteService.OnFrameReady += OnFrameReady;
            NetworkManager.ServerManager.RegisterBroadcast<FishNetFrameDataBroadcast>(HandleFrameDataBroadcast);
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            TimeManager.OnPostTick -= SimulateTick;
            _replayWriteService.OnFrameReady -= OnFrameReady;
            NetworkManager.ServerManager.UnregisterBroadcast<FishNetFrameDataBroadcast>(HandleFrameDataBroadcast);
        }

        private void SimulateTick()
        {
            _replayWriteService.OnNetworkTick((int)TimeManager.Tick, IsServerInitialized);
        }

        private void OnFrameReady(int frameId, byte[] frameData)
        {
            if (!IsClientInitialized)
                return;
            
            _framesBuffer ??= new List<FishNetFrameDataBroadcast>();
            
            var broadcast = new FishNetFrameDataBroadcast(frameId, frameData);
            Debug.Log($"[FishNetReplayWriteController] Send frame {broadcast.FrameId} to server, length {broadcast.FrameData.Length}");
            NetworkManager.ClientManager.Broadcast(broadcast);
            // _framesBuffer.Add(broadcast);
            //
            // if (_framesBuffer.Count >= _framesToSend)
            //     SendFrames();
        }

        private void SendFrames()
        {
            if (_framesBuffer == null)
                return;

            Debug.Log($"[FishNetReplayWriteController] Send {_framesBuffer.Count} frames to server");
            foreach (var broadcast in _framesBuffer)
            {
                Debug.Log($"[FishNetReplayWriteController] Send frame {broadcast.FrameId} to server, length {broadcast.FrameData.Length}");
                NetworkManager.ClientManager.Broadcast(broadcast);
            }
            _framesBuffer.Clear();
        }

        private void HandleFrameDataBroadcast(NetworkConnection connection, FishNetFrameDataBroadcast frameData, Channel channel)
        {
            Debug.Log($"[FishNetReplayWriteController] Received client {connection.ClientId} frame {frameData.FrameId} with length {frameData.FrameData.Length}");
            _replayWriteService.AddFrameFromClient(connection.ClientId, frameData.FrameId, frameData.FrameData);
        }
    }
}