using SkillcadeSDK.Events;

namespace SkillcadeSDK.FishNetAdapter.StateMachine.Events
{
    public struct RunningTimerTickEvent : IGameEvent
    {
        public readonly int RemainingSeconds;

        public RunningTimerTickEvent(int remainingSeconds)
        {
            RemainingSeconds = remainingSeconds;
        }
    }
}