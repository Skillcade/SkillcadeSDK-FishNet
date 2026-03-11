using System;
using System.Threading.Tasks;
using SkillcadeSDK.Connection;
using SkillcadeSDK.DI;
using UnityEngine;
using UnityEngine.Pool;

namespace SkillcadeSDK
{
    public static class Extensions
    {
        public static void SetLayerWithChildren(this GameObject target, int layer)
        {
            target.layer = layer;
            using var childrenPooled = ListPool<Transform>.Get(out var children);
            target.GetComponentsInChildren(children);
            foreach (var child in children)
            {
                child.gameObject.layer = layer;
            }
        }

        public static void InjectToMe(this object target)
        {
            if (ContainerSingletonWrapper.Instance != null)
                ContainerSingletonWrapper.Instance.Resolver.Inject(target);
        }

        public static bool IsConnectedOrHosting(this ConnectionState connectionState) =>
            connectionState is ConnectionState.Connected or ConnectionState.Hosting;

        public static void Destroy(this GameObject target)
        {
            if (target != null)
                GameObject.Destroy(target);
        }

        public static void DestroyImmediate(this GameObject target)
        {
            if (target != null)
                GameObject.DestroyImmediate(target);
        }

        public static void DestroyGameObject(this MonoBehaviour target)
        {
            if (target != null)
                GameObject.Destroy(target.gameObject);
        }

        public static void DestroyGameObjectImmediate(this MonoBehaviour target)
        {
            if (target != null)
                GameObject.DestroyImmediate(target.gameObject);
        }

        public static GameObject Instantiate(this GameObject prefab)
        {
            return GameObject.Instantiate(prefab);
        }

        public static GameObject Instantiate(this GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return GameObject.Instantiate(prefab, position, rotation);
        }

        public static T Instantiate<T>(this T prefab) where T : MonoBehaviour
        {
            return GameObject.Instantiate(prefab);
        }

        public static T Instantiate<T>(this T prefab, Vector3 position, Quaternion rotation) where T : MonoBehaviour
        {
            return GameObject.Instantiate(prefab, position, rotation);
        }

        public static void SetActive(this GameObject[] targets, bool value)
        {
            foreach (var target in targets)
            {
                target.SetActive(value);
            }
        }

        public static string SecondsToTimeString(this float secondsValue)
        {
            int seconds = Mathf.FloorToInt(secondsValue);
            if (seconds < 60)
                return $"{seconds}s";
            
            int minutes = seconds / 60;
            seconds %= 60;

            if (minutes < 60)
                return $"{minutes}m {seconds}s";

            int hours = minutes / 60;
            minutes %= 60;
            
            if (hours < 24)
                return $"{hours}h {minutes}m {seconds}s";
            
            int days = hours / 24;
            hours %= 24;
            return $"{days}d {hours}h {minutes}m {seconds}s";
        }

        // ReSharper disable once AsyncVoidMethod
        public static async void DoNotAwait(this Task task)
        {
            await AwaitAsync(task);
        }

        private static async Task AwaitAsync(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                if (e is not OperationCanceledException)
                {
                    Debug.LogError($"Error when executing task: {e}");
                }
            }
        }
    }
}