#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Infrastructure.Resources.Editor
{
    [CustomPropertyDrawer(typeof(PrefabFakeReference))]
    public sealed class PrefabFakeReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // locate backing fields on PrefabFakeReference (inherited private fields are serialized)
            SerializedProperty guidProp = property.FindPropertyRelative("_assetGuid");
            SerializedProperty pathProp = property.FindPropertyRelative("_assetPath");
            SerializedProperty editorPathProp = property.FindPropertyRelative("_editorAssetPath");

            GameObject current = null;
            if (editorPathProp != null && !string.IsNullOrEmpty(editorPathProp.stringValue))
            {
                current = AssetDatabase.LoadAssetAtPath<GameObject>(editorPathProp.stringValue);
            }
            else if (guidProp != null && !string.IsNullOrEmpty(guidProp.stringValue))
            {
                string pathFromGuid = AssetDatabase.GUIDToAssetPath(guidProp.stringValue);
                if (!string.IsNullOrEmpty(pathFromGuid))
                {
                    current = AssetDatabase.LoadAssetAtPath<GameObject>(pathFromGuid);
                }
            }

            EditorGUI.BeginChangeCheck();
            GameObject newObj = (GameObject)EditorGUI.ObjectField(position, label, current, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (newObj == null)
                {
                    if (guidProp != null) guidProp.stringValue = string.Empty;
                    if (pathProp != null) pathProp.stringValue = string.Empty;
                    if (editorPathProp != null) editorPathProp.stringValue = string.Empty;
                }
                else
                {
                    string newPath = AssetDatabase.GetAssetPath(newObj);
                    string newGuid = AssetDatabase.AssetPathToGUID(newPath);

                    if (guidProp != null) guidProp.stringValue = newGuid;
                    if (pathProp != null) pathProp.stringValue = newPath;
                    if (editorPathProp != null) editorPathProp.stringValue = newPath;
                }
            }

            EditorGUI.EndProperty();
        }
    }
}
#endif
