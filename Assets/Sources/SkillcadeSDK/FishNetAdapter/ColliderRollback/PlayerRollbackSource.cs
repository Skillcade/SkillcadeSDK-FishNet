using System;
using FishNet.Component.ColliderRollback;
using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Utility.Template;

namespace SkillcadeSDK.FishNetAdapter.ColliderRollback
{
    public class PlayerRollbackSource : TickNetworkBehaviour
    {
        public event Action<PreciseTick> OnRollback;
        
        private void Awake()
        {
            SetTickCallbacks(TickCallback.Tick);
        }

        protected override void TimeManager_OnTick()
        {
            if (!IsOwner)
                return;
            
            var tick = TimeManager.GetPreciseTick(TickType.LastPacketTick);
            PerformRollbackServerRpc(tick);
        }

        [ServerRpc(RequireOwnership = true)]
        private void PerformRollbackServerRpc(PreciseTick tick)
        {
            RollbackManager.Rollback(tick, RollbackPhysicsType.Physics2D, IsOwner);
            OnRollback?.Invoke(tick);
            RollbackManager.Return();
        }
    }
}