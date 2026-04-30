using System;
using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Utility.Template;
using SkillcadeSDK.FishNetAdapter.Replays;
using SkillcadeSDK.FishNetAdapter.Serialization;
using SkillcadeSDK.StateMachine;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.ColliderRollback
{
    public class PlayerRollbackSource : TickNetworkBehaviour
    {
        public Vector2? PlayerOwnerPosition;
        public event Action<PreciseTick, int> OnRollback;

        [SerializeField] private int _tickDivisor = 1;

        [Inject] private readonly IObjectResolver _objectResolver;

        private int _tickCounter;

        private void Awake()
        {
            SetTickCallbacks(TickCallback.Tick);
        }

        protected override void TimeManager_OnTick()
        {
            if (!IsOwner)
                return;
            
            if (_objectResolver == null)
                this.InjectToMe();
            
            if (!_objectResolver.TryResolve(out SkillcadeGameStateMachine stateMachine))
                return;
            
            if (stateMachine.CurrentStateType != GameStateType.Running)
                return;

            // if (++_tickCounter % _tickDivisor != 0)
            //     return;

            var tick = TimeManager.GetPreciseTick(TickType.Tick);

            var writeTick = TimeManager.Tick;
            PerformRollbackServerRpc(tick, (int)writeTick, transform.position);
        }

        [ServerRpc(RequireOwnership = true)]
        private void PerformRollbackServerRpc(PreciseTick tick, int writeTick, Vector2Short ownerPosition)
        {
            PlayerOwnerPosition = ownerPosition;

            foreach (var tmb in TickBasedMoveBehaviour.All)
            {
                if (tmb == null) continue;
                if (!tmb.TryGetComponent<FishNetRigidbody2dReplayComponent>(out var replay)) continue;

                replay.OverridePosition = tmb.GetPositionAtTick(tick);
            }

            OnRollback?.Invoke(tick, writeTick);

            PlayerOwnerPosition = null;
            foreach (var tmb in TickBasedMoveBehaviour.All)
            {
                if (tmb == null) continue;
                if (!tmb.TryGetComponent<FishNetRigidbody2dReplayComponent>(out var replay)) continue;

                replay.OverridePosition = null;
            }
        }
    }
}
