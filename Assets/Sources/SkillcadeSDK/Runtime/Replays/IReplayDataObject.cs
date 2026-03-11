namespace SkillcadeSDK.Replays
{
    public interface IReplayDataObject
    {
        public int Size { get; }

        public void Read(ReplayReader reader);
        public void Write(ReplayWriter writer);
    }
}