using FishNet.Managing.Timing;
using FishNet.Object;
using SkillcadeSDK.FishNetAdapter.ColliderRollback;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.Replays.Rollback
{
    public class RollbackReplayTickSource : MonoBehaviour
    {
        [SerializeField] private NetworkObject _networkObject;
        [SerializeField] private PlayerRollbackSource _rollbackSource;
        
        [Inject] private readonly IObjectResolver _objectResolver;
        
        private RollbackReplayWriteService _rollbackReplayService;

        private void Awake()
        {
            this.InjectToMe();
        }

        private void OnEnable()
        {
            _rollbackSource.OnRollback += Processtick;
        }

        private void OnDisable()
        {
            _rollbackSource.OnRollback -= Processtick;
        }

        private void Processtick(PreciseTick tick)
        {
            if (_rollbackReplayService == null)
            {
                if (!_objectResolver.TryResolve(out _rollbackReplayService))
                    return;
            }
            
            _rollbackReplayService.CaptureRollbackFrame(_networkObject.Owner.ClientId, (int)tick.Tick);
        }
    }
}
