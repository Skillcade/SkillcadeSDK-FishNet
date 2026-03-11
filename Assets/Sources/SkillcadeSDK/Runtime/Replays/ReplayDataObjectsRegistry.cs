using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.Replays
{
    [Preserve]
    public static class ReplayDataObjectsRegistry
    {
        public static IReadOnlyDictionary<Type, int> TypeToId => _typeToId;
        public static IReadOnlyDictionary<int, Type> IdToType => _idToType;

        private static bool _initialized;
        private static readonly Dictionary<Type, int> _typeToId = new Dictionary<Type, int>();
        private static readonly Dictionary<int, Type> _idToType = new Dictionary<int, Type>();
        
        public static void CollectDataObjectTypes()
        {
            if (_initialized)
                return;
            
            _initialized = true;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().OrderBy(x => x.FullName))
            {
                foreach (var componentType in assembly.GetTypes().Where(t => !t.IsInterface && !t.IsAbstract && t.GetCustomAttribute(typeof(ReplayDataObjectIdAttribute)) != null))
                {
                    var attribute = componentType.GetCustomAttribute<ReplayDataObjectIdAttribute>();
                    Debug.Log($"[ReplayDataObjectsRegistry] Got object type {componentType.Name} with id {attribute.Id}");
                    _idToType.Add(attribute.Id, componentType);
                    _typeToId.Add(componentType, attribute.Id);
                }
            }
        }
    }
}