using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using SkillcadeSDK.FishNetAdapter.Players;
using SkillcadeSDK.Replays;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.Replays
{
    public class FishNetReplayWriteController : NetworkBehaviour
    {
        [Inject] private readonly ReplayWriteService _replayWriteService;
        [Inject] private readonly PlayerReconnectService _reconnectService;

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
            
            var broadcast = new FishNetFrameDataBroadcast(frameId, frameData);
            // Debug.Log($"[FishNetReplayWriteController] Send frame {broadcast.FrameId} to server, length {broadcast.FrameData.Length}");
            NetworkManager.ClientManager.Broadcast(broadcast);
        }

        private void HandleFrameDataBroadcast(NetworkConnection connection, FishNetFrameDataBroadcast frameData, Channel channel)
        {
            int replayClientId = _reconnectService.ResolveReplayClientId(connection.ClientId);
            if (replayClientId != connection.ClientId)
                Debug.Log($"[PlayerReconnect] FrameDataBroadcast: connection={connection.ClientId} routed to replay world={replayClientId}");
            _replayWriteService.AddFrameFromClient(replayClientId, frameData.FrameId, frameData.FrameData);
        }
    }
}