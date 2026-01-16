using SkillcadeSDK.DI;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter
{
    public class DefaultPlayerSpawnerInstaller : MonoInstaller
    {
        [SerializeField] private PlayerSpawner _playerSpawner;
        
        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_playerSpawner);
        }
    }
}