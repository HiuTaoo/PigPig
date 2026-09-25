using UnityEngine;
using _Script.Data;

namespace _Script
{
    public static class LevelDataReader
    {
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
    }
}