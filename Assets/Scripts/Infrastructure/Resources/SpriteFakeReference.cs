using System;
using System.IO;
using UnityEngine;

namespace Infrastructure.Resources
{
	[Serializable]
	public sealed class SpriteFakeReference : FakeReference<Sprite>
	{
		[SerializeField] private string _editorAssetPath;

		public string EditorAssetPath => _editorAssetPath;

#if UNITY_EDITOR
		public Sprite GetSpriteInEditor()
		{
			if (!string.IsNullOrEmpty(_editorAssetPath))
			{
				return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(_editorAssetPath);
			}
			else if (!string.IsNullOrEmpty(AssetGuid))
			{
				string path = UnityEditor.AssetDatabase.GUIDToAssetPath(AssetGuid);
				return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
			}

			return null;
		}

		public void Editor_SetSprite(Sprite sprite)
		{
			Editor_SetObject(sprite);
			_editorAssetPath = sprite != null ? UnityEditor.AssetDatabase.GetAssetPath(sprite) : null;
		}

		public string GetSpriteNameForBuild()
		{
			return !string.IsNullOrEmpty(_editorAssetPath) ? Path.GetFileNameWithoutExtension(_editorAssetPath) : null;
		}
#endif
	}
}


