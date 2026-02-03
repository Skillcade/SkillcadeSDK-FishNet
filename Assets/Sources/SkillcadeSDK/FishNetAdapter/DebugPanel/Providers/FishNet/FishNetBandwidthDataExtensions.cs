using FishNet.Editing;

namespace Sources.SkillcadeSDK.FishNetAdapter.DebugPanel.Providers.FishNet
{
    public static class FishNetBandwidthDataExtensions
    {
        public static ulong GetInboundTrafficBytes(this BidirectionalNetworkTraffic serverTraffic)
        {
            return serverTraffic.InboundTraffic.Bytes;
        }
        
        public static ulong GetOutboundTrafficBytes(this BidirectionalNetworkTraffic serverTraffic)
        {
            return serverTraffic.OutboundTraffic.Bytes;
        }
    }
}