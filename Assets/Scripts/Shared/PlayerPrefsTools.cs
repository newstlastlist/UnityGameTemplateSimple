#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Shared
{
    public static class PlayerPrefsTools
    {
        [MenuItem("PlayerPrefs/Clear PlayerPrefs %#r")] // Ctrl+Shift+R (Win) / Cmd+Shift+R (Mac)
        public static void ClearAllPlayerPrefs()
        {
            if (EditorUtility.DisplayDialog(
                    "Clear PlayerPrefs",
                    "Очистить все сохранения PlayerPrefs?",
                    "Да", "Отмена"))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Debug.Log("[PlayerPrefsTools] Все PlayerPrefs очищены");
            }
        }
    }
}
#endif