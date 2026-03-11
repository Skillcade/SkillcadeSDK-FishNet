using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SkillcadeSDK.DI
{
    public class GameScopeWithInstallers : LifetimeScope
    {
        [SerializeField] private MonoInstaller[] _installers;

        protected override void Configure(IContainerBuilder builder)
        {
            EntryPointsBuilder.EnsureDispatcherRegistered(builder);
            
            builder.Register<ContainerSingletonWrapper>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.RegisterBuildCallback(AutoInjectTargets);
            
            foreach (var installer in _installers)
            {
                if (installer != null)
                    installer.Install(builder);
            }

            base.Configure(builder);
        }

        private void AutoInjectTargets(IObjectResolver objectResolver)
        {
            foreach (var installer in _installers)
            {
                foreach (var autoInjectGameObject in installer.GetAutoInjectGameObjects())
                {
                    objectResolver.InjectGameObject(autoInjectGameObject);
                }
            }
        }
    }
}