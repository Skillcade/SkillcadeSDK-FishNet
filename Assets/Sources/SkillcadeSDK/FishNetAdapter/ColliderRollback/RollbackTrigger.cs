using FishNet.Object;

namespace SkillcadeSDK.FishNetAdapter.ColliderRollback
{
    /// <summary>
    /// Base class for all triggers that should be verified on server using ColliderRollback feature.
    /// Implement HandleTriggerServer in your components.
    /// playerNetworkObject is reference to NetworkObject of player, who called ColliderRollback verification.
    /// </summary>
    public abstract class RollbackTrigger : NetworkBehaviour
    {
        [Server]
        public abstract void HandleTriggerServer(NetworkObject playerNetworkObject);
    }
}