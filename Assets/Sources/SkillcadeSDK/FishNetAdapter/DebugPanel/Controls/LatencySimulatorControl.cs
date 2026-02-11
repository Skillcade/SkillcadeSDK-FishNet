#if SKILLCADE_DEBUG
using FishNet.Managing;
using FishNet.Managing.Transporting;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.DebugPanel.Controls
{
    public class LatencySimulatorControl
    {
        [Inject] private readonly NetworkManager _networkManager;

        public bool IsAvailable => Simulator != null;

        private LatencySimulator Simulator => _networkManager?.TransportManager?.LatencySimulator;

        public bool Enabled
        {
            get => Simulator?.GetEnabled() ?? false;
            set => Simulator?.SetEnabled(value);
        }

        public long Latency
        {
            get => Simulator?.GetLatency() ?? 0;
            set => Simulator?.SetLatency(value);
        }

        public float PacketLoss
        {
            get => (float)(Simulator?.GetPacketLost() ?? 0);
            set => Simulator?.SetPacketLoss(value);
        }

        public float OutOfOrder
        {
            get => (float)(Simulator?.GetOutOfOrder() ?? 0);
            set => Simulator?.SetOutOfOrder(value);
        }

        public long Jitter
        {
            get => Simulator?.GetJitter() ?? 0;
            set => Simulator?.SetJitter(value);
        }

        public float SpikeIntervalMin
        {
            get => Simulator?.GetSpikeIntervalMin() ?? 0;
            set => Simulator?.SetSpikeIntervalMin(value);
        }

        public float SpikeIntervalMax
        {
            get => Simulator?.GetSpikeIntervalMax() ?? 0;
            set => Simulator?.SetSpikeIntervalMax(value);
        }

        public long SpikeAmountMin
        {
            get => Simulator?.GetSpikeAmountMin() ?? 0;
            set => Simulator?.SetSpikeAmountMin(value);
        }

        public long SpikeAmountMax
        {
            get => Simulator?.GetSpikeAmountMax() ?? 0;
            set => Simulator?.SetSpikeAmountMax(value);
        }

        public void Reset()
        {
            if (Simulator == null) return;

            Enabled = false;
            Latency = 0;
            PacketLoss = 0;
            OutOfOrder = 0;
            Jitter = 0;
            SpikeIntervalMin = 0;
            SpikeIntervalMax = 0;
            SpikeAmountMin = 0;
            SpikeAmountMax = 0;
        }
    }
}
#endif
