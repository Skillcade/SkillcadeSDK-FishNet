using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SkillcadeSDK.DI
{
    public abstract class MonoInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField] protected List<GameObject> _autoInjectGameObjects;
        
        public abstract void Install(IContainerBuilder builder);
        
        public virtual IEnumerable<GameObject> GetAutoInjectGameObjects() => _autoInjectGameObjects;
    }
}