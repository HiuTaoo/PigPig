using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using _Script.Data;

namespace _Script.Editor
{
    public class LevelEditorWindow : EditorWindow
    {
        private int levelIndex = 1;
        private int matrixSize = 9;

        private List<Color> customPalette = new List<Color>();
        private int selectedColorIndex = 0;

        // Ma trận màu (-1 biểu thị ô trống chưa tô màu)
        private int[,] gridColorIds;
        // Tọa độ các hạt giống (Seed Queens)
        private HashSet<Vector2Int> seedQueens = new HashSet<Vector2Int>();

        // Các biến nhập RGB trực tiếp
        private int inputR = 255;
        private int inputG = 255;
        private int inputB = 255;

        [MenuItem("Tools/Level Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelEditorWindow>("Level Editor");
            window.minSize = new Vector2(500, 720);
            window.Show();
        }

        private void OnEnable()
        {
            InitPalette();
            ResetGrid();
            SyncRgbFromSelectedColor();
        }

        private void InitPalette()
        {
            customPalette.Clear();
            float hueStep = 1f / 15f;
            float randomOffset = Random.value;
            for (int i = 0; i < 15; i++)
            {
                float hue = Mathf.Repeat(randomOffset + (i * hueStep), 1f);
                customPalette.Add(Color.HSVToRGB(hue, 0.65f, 0.9f));
            }
        }

        private void RandomizePalette()
        {
            float randomOffset = Random.value;
            for (int i = 0; i < customPalette.Count; i++)
            {
                float hue = Mathf.Repeat(randomOffset + ((float)i / matrixSize), 1f);
                customPalette[i] = Color.HSVToRGB(hue, Random.Range(0.55f, 0.85f), Random.Range(0.8f, 0.95f));
            }
            SyncRgbFromSelectedColor();
        }

        private void SyncRgbFromSelectedColor()
        {
            if (selectedColorIndex >= 0 && selectedColorIndex < customPalette.Count)
            {
                Color c = customPalette[selectedColorIndex];
                inputR = Mathf.RoundToInt(c.r * 255f);
                inputG = Mathf.RoundToInt(c.g * 255f);
                inputB = Mathf.RoundToInt(c.b * 255f);
            }
        }

        private void ResetGrid()
        {
            gridColorIds = new int[matrixSize, matrixSize];
            for (int r = 0; r < matrixSize; r++)
                for (int c = 0; c < matrixSize; c++)
                    gridColorIds[r, c] = -1;

            seedQueens.Clear();
        }

        private void OnGUI()
        {
            GUILayout.Label("CẤU HÌNH LEVEL", EditorStyles.boldLabel);
            levelIndex = EditorGUILayout.IntField("Số thứ tự Level:", levelIndex);
            
            int newSize = EditorGUILayout.IntSlider("Cấp ma trận (n):", matrixSize, 4, 15);
            if (newSize != matrixSize)
            {
                matrixSize = newSize;

                // 1. Tự động bù thêm màu vào Palette nếu thiếu
                EnsurePaletteCapacity();

                // 2. Xóa sạch dữ liệu ma trận và hạt giống cũ
                ResetGrid();

                // 3. Reset lại màu đang chọn về an toàn
                if (selectedColorIndex >= matrixSize)
                {
                    selectedColorIndex = 0;
                }
                SyncRgbFromSelectedColor();

                // Ép vẽ lại GUI ngay lập tức để không bị lệch state
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.Space(8);
            GUILayout.Label("THAO TÁC HẠT GIỐNG & MA TRẬN", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.3f, 0.8f, 1f);
            if (GUILayout.Button("👑 Tạo Hạt Giống Hợp Lệ", GUILayout.Height(30)))
            {
                GenerateValidSeeds();
            }

            GUI.backgroundColor = new Color(1f, 0.85f, 0.3f);
            if (GUILayout.Button("⚡ Tự Động Tạo Hoàn Chỉnh", GUILayout.Height(30)))
            {
                LevelJsonData autoData = LevelGenerator.GenerateLevel(matrixSize);
                if (autoData != null)
                {
                    LoadDataToEditor(autoData);
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            GUILayout.Label("QUẢN LÝ MÀU SẮC", EditorStyles.boldLabel);

            // Nút Random màu mới
            GUI.backgroundColor = new Color(0.9f, 0.7f, 1f);
            if (GUILayout.Button("🎲 Random Bảng Màu Mới", GUILayout.Height(28)))
            {
                RandomizePalette();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(4);
            DrawPaletteSelector();

            // Khu vực chỉnh sửa trực tiếp màu đang chọn
            DrawColorCustomizer();

            EditorGUILayout.Space(10);
            GUILayout.Label("BẢN ĐỒ Ô (CLICK ĐỂ TÔ MÀU)", EditorStyles.boldLabel);
            DrawGrid();

            EditorGUILayout.Space(12);
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button($"LƯU THÀNH LEVEL_{levelIndex}.JSON", GUILayout.Height(40)))
            {
                SaveCurrentEditorLevel();
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawPaletteSelector()
        {
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < matrixSize; i++)
            {
                Color prevCol = GUI.backgroundColor;
                GUI.backgroundColor = customPalette[i];

                string label = (selectedColorIndex == i) ? "✓" : i.ToString();
                if (GUILayout.Button(label, GUILayout.Width(35), GUILayout.Height(35)))
                {
                    selectedColorIndex = i;
                    SyncRgbFromSelectedColor();
                }

                GUI.backgroundColor = prevCol;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawColorCustomizer()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Chỉnh sửa Màu Số {selectedColorIndex}:", EditorStyles.boldLabel);

            // 1. Color field (Picker)
            EditorGUI.BeginChangeCheck();
            Color pickedColor = EditorGUILayout.ColorField("Color Picker:", customPalette[selectedColorIndex]);
            if (EditorGUI.EndChangeCheck())
            {
                customPalette[selectedColorIndex] = pickedColor;
                SyncRgbFromSelectedColor();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawGrid()
        {
            if (gridColorIds == null || gridColorIds.GetLength(0) != matrixSize)
            {
                ResetGrid();
            }

            for (int r = matrixSize - 1; r >= 0; r--)
            {
                EditorGUILayout.BeginHorizontal();
                for (int c = 0; c < matrixSize; c++)
                {
                    int colorId = gridColorIds[r, c];
                    Color prevCol = GUI.backgroundColor;

                    GUI.backgroundColor = (colorId >= 0 && colorId < customPalette.Count) 
                        ? customPalette[colorId] 
                        : Color.gray;

                    bool isSeed = seedQueens.Contains(new Vector2Int(r, c));
                    string label = isSeed ? "👑" : $"{r},{c}";

                    if (GUILayout.Button(label, GUILayout.Width(40), GUILayout.Height(40)))
                    {
                        gridColorIds[r, c] = selectedColorIndex;
                    }

                    GUI.backgroundColor = prevCol;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void GenerateValidSeeds()
        {
            ResetGrid();

            int[] queens = new int[matrixSize];
            if (!FindRandomValidQueens(0, queens, matrixSize))
            {
                EditorUtility.DisplayDialog("Lỗi", "Không tìm được cấu hình hạt giống hợp lệ!", "OK");
                return;
            }

            for (int r = 0; r < matrixSize; r++)
            {
                int c = queens[r];
                gridColorIds[r, c] = r;
                seedQueens.Add(new Vector2Int(r, c));
            }
        }

        private bool FindRandomValidQueens(int row, int[] queens, int n)
        {
            if (row >= n) return true;

            List<int> cols = new List<int>();
            for (int c = 0; c < n; c++) cols.Add(c);

            for (int i = cols.Count - 1; i > 0; i--)
            {
                int rnd = Random.Range(0, i + 1);
                int tmp = cols[i];
                cols[i] = cols[rnd];
                cols[rnd] = tmp;
            }

            foreach (int c in cols)
            {
                if (IsSafeSeed(row, c, queens))
                {
                    queens[row] = c;
                    if (FindRandomValidQueens(row + 1, queens, n)) return true;
                }
            }
            return false;
        }

        private bool IsSafeSeed(int row, int col, int[] queens)
        {
            for (int r = 0; r < row; r++)
            {
                int c = queens[r];
                if (c == col) return false;
                if (Mathf.Abs(r - row) <= 1 && Mathf.Abs(c - col) <= 1) return false;
            }
            return true;
        }

        private void LoadDataToEditor(LevelJsonData data)
        {
            matrixSize = data.n;
            gridColorIds = new int[matrixSize, matrixSize];
            seedQueens.Clear();

            Dictionary<int, int> colorIdToIndex = new Dictionary<int, int>();
            customPalette.Clear();

            for (int i = 0; i < data.requiredColors.Count; i++)
            {
                var req = data.requiredColors[i];
                customPalette.Add(req.ToUnityColor());
                colorIdToIndex[req.GetColorID()] = i;
            }

            foreach (var n in data.nodes)
            {
                int cId = n.GetColorID();
                if (colorIdToIndex.TryGetValue(cId, out int pIndex))
                {
                    gridColorIds[n.row, n.col] = pIndex;
                }
            }
            SyncRgbFromSelectedColor();
        }

        private void SaveCurrentEditorLevel()
        {
            for (int r = 0; r < matrixSize; r++)
            {
                for (int c = 0; c < matrixSize; c++)
                {
                    if (gridColorIds[r, c] == -1)
                    {
                        EditorUtility.DisplayDialog("Lỗi Chưa Xong", $"Ô ({r}, {c}) vẫn chưa được tô màu!", "OK");
                        return;
                    }
                }
            }

            LevelJsonData data = new LevelJsonData
            {
                n = matrixSize,
                requiredColors = new List<RequiredColorData>(),
                nodes = new List<NodeJsonData>()
            };

            for (int i = 0; i < matrixSize; i++)
            {
                Color c = customPalette[i];
                data.requiredColors.Add(new RequiredColorData
                {
                    r = Mathf.RoundToInt(c.r * 255),
                    g = Mathf.RoundToInt(c.g * 255),
                    b = Mathf.RoundToInt(c.b * 255),
                    requiredCount = 1
                });
            }

            for (int r = 0; r < matrixSize; r++)
            {
                for (int c = 0; c < matrixSize; c++)
                {
                    int pIndex = gridColorIds[r, c];
                    Color col = customPalette[pIndex];
                    data.nodes.Add(new NodeJsonData
                    {
                        row = r,
                        col = c,
                        r = Mathf.RoundToInt(col.r * 255),
                        g = Mathf.RoundToInt(col.g * 255),
                        b = Mathf.RoundToInt(col.b * 255)
                    });
                }
            }

            LevelDataReader.SaveLevelToJson(data, levelIndex);
            EditorUtility.DisplayDialog("Thành Công", $"Đã lưu Level_{levelIndex}.json thành công!", "OK");
        }
        
        private void EnsurePaletteCapacity()
        {
            if (customPalette == null) customPalette = new List<Color>();

            int minRequired = Mathf.Max(matrixSize, 15);
            while (customPalette.Count < minRequired)
            {
                float hue = (float)customPalette.Count / minRequired;
                customPalette.Add(Color.HSVToRGB(hue, 0.65f, 0.9f));
            }
        }
    }
}