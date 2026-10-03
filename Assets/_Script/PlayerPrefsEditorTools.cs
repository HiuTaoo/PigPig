#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class PlayerPrefsEditorTools
{
    [MenuItem("Tools/PlayerPrefs/Set Level to 1")]
    public static void ResetLevelToOne()
    {
        PlayerPrefs.SetInt("CURRENT_LEVEL", 1);
        PlayerPrefs.Save();
        Debug.Log(">>> Đã đặt CURRENT_LEVEL = 1");
    }

    [MenuItem("Tools/PlayerPrefs/Delete All PlayerPrefs")]
    public static void ClearAllData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log(">>> Đã xóa toàn bộ PlayerPrefs!");
    }

    [MenuItem("Tools/PlayerPrefs/Delete Level Key Only")]
    public static void DeleteLevelKey()
    {
        PlayerPrefs.DeleteKey("CURRENT_LEVEL");
        PlayerPrefs.Save();
        Debug.Log(">>> Đã xóa key CURRENT_LEVEL");
    }
}
#endif