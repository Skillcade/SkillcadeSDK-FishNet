namespace SkillcadeSDK.Replays.Events
{
    public abstract class ReplayEvent : IReplayDataObject
    {
        public abstract int Size { get; }

        public abstract void Read(ReplayReader reader);
        public abstract void Write(ReplayWriter writer);

        public abstract void Handle(int worldId);
        public abstract void Undo(int clientId);
    }
}