using System.Threading.Tasks;
using UnityEngine;

namespace Infrastructure.Resources
{
    public sealed class UnityResourceService : IResourceService
    {
        public T LoadPrefab<T>(string resourcesPath) where T : Component
        {
            if (string.IsNullOrWhiteSpace(resourcesPath))
            {
                return null;
            }
            var prefab = UnityEngine.Resources.Load<GameObject>(resourcesPath);
            if (prefab == null)
            {
                return null;
            }
            return prefab.GetComponent<T>();
        }

        public async Task<T> LoadPrefabAsync<T>(string resourcesPath) where T : Component
        {
            if (string.IsNullOrWhiteSpace(resourcesPath))
            {
                return null;
            }
            var request = UnityEngine.Resources.LoadAsync<GameObject>(resourcesPath);
            while (!request.isDone)
            {
                await Task.Yield();
            }
            var prefab = request.asset as GameObject;
            if (prefab == null)
            {
                return null;
            }
            return prefab.GetComponent<T>();
        }

        public GameObject LoadGameObject(string resourcesPath)
        {
            if (string.IsNullOrWhiteSpace(resourcesPath))
            {
                return null;
            }
            return UnityEngine.Resources.Load<GameObject>(resourcesPath);
        }

        public async Task<GameObject> LoadGameObjectAsync(string resourcesPath)
        {
            if (string.IsNullOrWhiteSpace(resourcesPath))
            {
                return null;
            }
            var request = UnityEngine.Resources.LoadAsync<GameObject>(resourcesPath);
            while (!request.isDone)
            {
                await Task.Yield();
            }
            return request.asset as GameObject;
        }
    }
}
