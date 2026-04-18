using System.Collections.Generic;
using FishNet.Managing.Timing;
using FishNet.Object;
using UnityEngine;

namespace SkillcadeSDK.FishNetAdapter.ColliderRollback
{
    /// <summary>
    /// Base for server-authoritative movement whose position at any tick can be
    /// recomputed deterministically. Used by PlayerRollbackSource to reconstruct
    /// other objects' positions at the client's PreciseTick during rollback,
    /// replacing FishNet's ColliderRollback history for these objects.
    ///
    /// Instances register themselves in <see cref="All"/> while enabled.
    /// </summary>
    public abstract class TickBasedMoveBehaviour : NetworkBehaviour
    {
        private static readonly List<TickBasedMoveBehaviour> _all = new();
        public static IReadOnlyList<TickBasedMoveBehaviour> All => _all;

        protected virtual void OnEnable()  => _all.Add(this);
        protected virtual void OnDisable() => _all.Remove(this);

        /// <summary>
        /// Returns the world position this object would occupy at the given PreciseTick.
        /// Must be pure/deterministic — no side effects.
        /// </summary>
        public abstract Vector2 GetPositionAtTick(PreciseTick pt);
    }
}
