using System;

namespace SkillcadeSDK.Replays
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ReplayDataObjectIdAttribute : Attribute
    {
        public readonly int Id;

        public ReplayDataObjectIdAttribute(int id)
        {
            Id = id;
        }
    }
}