using System;
using FishNet.Connection;
using FishNet.Managing.Server;
using FishNet.Object;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SkillcadeSDK.FishNetAdapter
{
    public static class FishNetExtensions
    {
        public static T InstantiateAndSpawn<T>(this ServerManager serverManager, T prefab, Vector3 position,
            Quaternion rotation,
            NetworkConnection ownerConnection = null)
            where T : NetworkBehaviour
        {
            if (prefab == null)
                throw new ArgumentException("Trying to instantiate a null prefab");
            
            var instance = Object.Instantiate(prefab, position, rotation);
            var networkObject = instance.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Object.Destroy(instance.gameObject);
                throw new ArgumentException("Trying to instantiate a prefab without a network object");
            }
            
            serverManager.Spawn(networkObject, ownerConnection);
            return instance;
        }
        
        public static NetworkObject InstantiateAndSpawn(this ServerManager serverManager, NetworkObject prefab, Vector3 position,
            Quaternion rotation,
            NetworkConnection ownerConnection = null)
        {
            if (prefab == null)
                throw new ArgumentException("Trying to instantiate a null prefab");
            
            var instance = Object.Instantiate(prefab, position, rotation);
            serverManager.Spawn(instance, ownerConnection);
            return instance;
        }
    }
}