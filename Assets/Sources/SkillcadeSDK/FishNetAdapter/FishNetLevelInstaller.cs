using SkillcadeSDK.DI;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter
{
    public class FishNetLevelInstaller : MonoInstaller
    {
        [SerializeField] private SpawnPointProvider _spawnPointProvider;
        
        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_spawnPointProvider);
        }
    }
}