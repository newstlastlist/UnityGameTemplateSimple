using System;
using System.IO;
using UnityEngine;

namespace Infrastructure.Resources
{
    [Serializable]
    public class FakeReference<T> where T : UnityEngine.Object
    {
        [SerializeField] private string _assetGuid;
        [SerializeField] private string _assetPath;

        private string _folderInResources;

        public string AssetGuid => _assetGuid;
        public string AssetPath => _assetPath;

        public string DefaultSearchFolder => "Assets/Resources";

        public string FolderInResources
        {
            get
            {
                if (string.IsNullOrEmpty(_folderInResources))
                {
                    if (string.IsNullOrEmpty(_assetPath))
                    {
                        return string.Empty;
                    }

                    int resourcesIndex = _assetPath.IndexOf("Resources", StringComparison.Ordinal);
                    if (resourcesIndex < 0)
                    {
                        return string.Empty;
                    }

                    int start = resourcesIndex + "Resources".Length + 1;
                    int lastSlash = _assetPath.LastIndexOf("/", StringComparison.Ordinal);
                    if (lastSlash < start)
                    {
                        _folderInResources = string.Empty;
                    }
                    else
                    {
                        _folderInResources = _assetPath
                            .Substring(start, lastSlash - start)
                            .Replace("Resources_moved/", string.Empty)
                            .Replace("Resources/", string.Empty);
                    }
                }

                return _folderInResources;
            }
        }

        public string AssetName => string.IsNullOrEmpty(_assetPath) ? null : Path.GetFileNameWithoutExtension(_assetPath);

#if UNITY_EDITOR
        public void Editor_SetObject(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                _assetGuid = null;
                _assetPath = null;
                _folderInResources = null;
                return;
            }

            string path = UnityEditor.AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            _assetPath = path;
            _assetGuid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            _folderInResources = null;
        }
#endif

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(_assetPath) && !string.IsNullOrEmpty(AssetName);
        }

        public string GetResourcesRelativePath()
        {
            if (!IsValid())
            {
                return null;
            }

            if (string.IsNullOrEmpty(FolderInResources))
            {
                return AssetName;
            }

            return FolderInResources + "/" + AssetName;
        }
    }
}