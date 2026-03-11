using System.Collections.Generic;
using VContainer;

namespace SkillcadeSDK.Common.Level
{
    public class RespawnServiceProvider
    {
        [Inject] private readonly IReadOnlyList<IRespawnService> _respawnServices;

        public void TriggerRespawn()
        {
            foreach (var respawnService in _respawnServices)
            {
                respawnService.Respawn();
            }
        }
    }
}