using UnityEngine;

namespace Infrastructure.SceneManagement
{
    [CreateAssetMenu(fileName = "SceneLoaderConfig", menuName = "Project/Scene Loader Config")]
    public sealed class SceneLoaderConfig : ScriptableObject
    {
        [field: SerializeField] public int MenuSceneIndex { get; private set; }
        [field: SerializeField] public int MainSceneIndex { get; private set; }
    }
}