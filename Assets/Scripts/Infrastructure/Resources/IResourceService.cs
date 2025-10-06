using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Infrastructure.Resources
{
    public interface IResourceService
    {
        T LoadPrefab<T>(string resourcesPath) where T : Component;
        Task<T> LoadPrefabAsync<T>(string resourcesPath) where T : Component;
        GameObject LoadGameObject(string resourcesPath);
        Task<GameObject> LoadGameObjectAsync(string resourcesPath);
    }
}
