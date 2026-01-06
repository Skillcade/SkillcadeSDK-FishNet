using FishNet.Broadcast;
using FishNet.CodeGenerating;

namespace SkillcadeSDK.FishNetAdapter.Replays
{
    [UseGlobalCustomSerializer]
    public struct FishNetFrameDataBroadcast : IBroadcast
    {
        public readonly int FrameId;
        public readonly byte[] FrameData;

        public FishNetFrameDataBroadcast(int frameId, byte[] frameData)
        {
            FrameId = frameId;
            FrameData = frameData;
        }
    }
}