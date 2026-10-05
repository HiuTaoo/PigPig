#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using _Script.Data;

namespace _Script.Editor
{
    public class BatchLevelGeneratorWindow : EditorWindow
    {
        private int startLevel = 30;
        private int endLevel = 100;

        [MenuItem("Tools/Batch Level Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<BatchLevelGeneratorWindow>("Batch Generator");
            window.minSize = new Vector2(380, 200);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("TỰ ĐỘNG TẠO NHIỀU MÀN CHƠI", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            startLevel = EditorGUILayout.IntField("Level bắt đầu:", startLevel);
            endLevel = EditorGUILayout.IntField("Level kết thúc:", endLevel);

            EditorGUILayout.Space(10);

            EditorGUILayout.Space(15);
            GUI.backgroundColor = new Color(0.2f, 1f, 0.4f);

            if (GUILayout.Button($"Sinh từ Level {startLevel} đến {endLevel}", GUILayout.Height(40)))
            {
                GenerateBatch();
            }

            GUI.backgroundColor = Color.white;
        }

        private void GenerateBatch()
        {
            if (startLevel > endLevel || startLevel < 1)
            {
                EditorUtility.DisplayDialog("Lỗi", "Khoảng Level không hợp lệ!", "OK");
                return;
            }

            int total = endLevel - startLevel + 1;
            int successCount = 0;

            for (int lvl = startLevel; lvl <= endLevel; lvl++)
            {
                // Hiển thị thanh tiến trình trong Unity Editor
                float progress = (float)(lvl - startLevel) / total;
                EditorUtility.DisplayProgressBar("Đang tạo Level...", $"Đang tạo file Level_{lvl}.json", progress);

                LevelJsonData data = LevelGenerator.GenerateLevelByDifficulty(lvl);
                if (data != null)
                {
                    LevelDataReader.SaveLevelToJson(data, lvl);
                    successCount++;
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Hoàn tất!", $"Đã tạo và lưu thành công {successCount}/{total} màn chơi vào Assets/Resources/Levels!", "OK");
        }
    }
}
#endif