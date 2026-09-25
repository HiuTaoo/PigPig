using System.IO;
using UnityEngine;
using _Script.Data;

namespace _Script
{
    public static class LevelDataReader
    {
        private const string LevelFolderInResources = "Levels";
        public static LevelJsonData LoadLevelFromResources(int levelIndex)
        {
            string path = $"Levels/Level_{levelIndex}";
            TextAsset jsonFile = Resources.Load<TextAsset>(path);

            if (jsonFile == null)
            {
                Debug.LogError($"[LevelDataReader] Không tìm thấy file JSON tại Resources/{path}");
                return null;
            }

            return JsonUtility.FromJson<LevelJsonData>(jsonFile.text);
        }

        public static LevelJsonData LoadLevelFromRawJson(string jsonText)
        {
            if (string.IsNullOrEmpty(jsonText)) return null;
            return JsonUtility.FromJson<LevelJsonData>(jsonText);
        }
        
        public static void SaveLevelToJson(LevelJsonData levelData, int levelIndex)
        {
            if (levelData == null) return;

            string jsonString = JsonUtility.ToJson(levelData, true);

            // Ưu tiên lưu vào thư mục Assets/Resources/Levels khi đang trong Unity Editor để quản lý dễ dàng
#if UNITY_EDITOR
            string dirPath = Path.Combine(Application.dataPath, "Resources", LevelFolderInResources);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            string filePath = Path.Combine(dirPath, $"Level_{levelIndex}.json");
            File.WriteAllText(filePath, jsonString);
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log($"[LevelDataReader] Đã lưu thành công tại: {filePath}");
#else
            // Khi chạy trên thiết bị (Android, iOS...), lưu vào persistentDataPath
            string filePath = Path.Combine(Application.persistentDataPath, $"Level_{levelIndex}.json");
            File.WriteAllText(filePath, jsonString);
            Debug.Log($"[LevelDataReader] Đã lưu file tại persistentDataPath: {filePath}");
#endif
        }
    }
}