using System;
using System.IO;
using UnityEngine;

namespace Infrastructure.Resources
{
    [Serializable]
    public sealed class PrefabFakeReference : FakeReference<GameObject>
    {
        [SerializeField] private string _editorAssetPath;

        public string EditorAssetPath => _editorAssetPath;

#if UNITY_EDITOR
        public GameObject GetPrefabInEditor()
        {
            if (!string.IsNullOrEmpty(_editorAssetPath))
            {
                return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(_editorAssetPath);
            }
            else if (!string.IsNullOrEmpty(AssetGuid))
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(AssetGuid);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            return null;
        }

        public void Editor_SetPrefab(GameObject prefab)
        {
            Editor_SetObject(prefab);
            _editorAssetPath = prefab != null ? UnityEditor.AssetDatabase.GetAssetPath(prefab) : null;
        }

        public string GetPrefabNameForBuild()
        {
            return !string.IsNullOrEmpty(_editorAssetPath) ? Path.GetFileNameWithoutExtension(_editorAssetPath) : null;
        }
#endif
    }
}
