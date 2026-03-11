using System.Collections.Generic;
using SkillcadeSDK.Replays.Components;
using UnityEngine;

namespace SkillcadeSDK.Replays
{
    [CreateAssetMenu(fileName = "ReplayPrefabRegistry", menuName = "Configs/Replay Prefab Registry")]
    public class ReplayPrefabRegistry : ScriptableObject
    {
        [SerializeField] private ReplayObjectHandler[] _prefabs;

        public bool TryGetPrefab(int prefabId, out ReplayObjectHandler objectHandlerPrefab)
        {
            foreach (var prefab in _prefabs)
            {
                if (prefab.PrefabId == prefabId)
                {
                    objectHandlerPrefab = prefab;
                    return true;
                }
            }
            
            objectHandlerPrefab = null;
            return false;
        }
    }
}