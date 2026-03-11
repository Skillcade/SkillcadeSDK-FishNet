using System;
using VContainer;
using VContainer.Unity;

namespace SkillcadeSDK.DI
{
    public class ContainerSingletonWrapper : IInitializable, IDisposable
    {
        public static ContainerSingletonWrapper Instance { get; private set; }

        public IObjectResolver Resolver => _resolver;
        
        [Inject] private readonly IObjectResolver _resolver;
        
        public void Initialize()
        {
            if (Instance == null)
                Instance = this;
        }

        public void Dispose()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}