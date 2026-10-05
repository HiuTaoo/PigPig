using System.Collections.Generic;
using UnityEngine;
using _Script.Data;

namespace _Script
{
    public static class LevelGenerator
    {
        private static readonly Vector2Int[] Directions = new Vector2Int[]
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        /// <summary>
        /// Tạo tự động 1 LevelJsonData hợp lệ với cấp ma trận n
        /// </summary>
        public static LevelJsonData GenerateLevel(int n)
        {
            if (n < 4)
            {
                Debug.LogError("Cấp ma trận n phải từ 4 trở lên!");
                return null;
            }

            // 1. Tìm vị trí n con lợn thỏa mãn luật
            int[] queens = new int[n]; // queens[r] = c
            if (!FindRandomValidQueens(0, queens, n))
            {
                Debug.LogError("Không tìm được thế cờ hợp lệ!");
                return null;
            }

            // 2. Khởi tạo ma trận màu n x n (-1 là chưa tô màu)
            int[,] gridColors = new int[n, n];
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    gridColors[r, c] = -1;

            // 3. Đặt n hạt giống màu tại vị trí n con lợn
            List<Vector2Int>[] regionCells = new List<Vector2Int>[n];
            Queue<Vector2Int> frontier = new Queue<Vector2Int>();

            for (int i = 0; i < n; i++)
            {
                int r = i;
                int c = queens[i];
                gridColors[r, c] = i; // Vùng màu i
                regionCells[i] = new List<Vector2Int> { new Vector2Int(r, c) };
            }

            // 4. Lan tỏa màu để phủ kín bàn cờ (Multi-source Flood Fill)
            List<Vector2Int> emptyNeighbors = new List<Vector2Int>();
            int uncoloredCount = (n * n) - n;

            while (uncoloredCount > 0)
            {
                // Chọn ngẫu nhiên 1 vùng màu để mở rộng
                int randomRegion = Random.Range(0, n);
                emptyNeighbors.Clear();

                foreach (var cell in regionCells[randomRegion])
                {
                    foreach (var dir in Directions)
                    {
                        int nr = cell.x + dir.x;
                        int nc = cell.y + dir.y;

                        if (nr >= 0 && nr < n && nc >= 0 && nc < n && gridColors[nr, nc] == -1)
                        {
                            emptyNeighbors.Add(new Vector2Int(nr, nc));
                        }
                    }
                }

                if (emptyNeighbors.Count > 0)
                {
                    Vector2Int chosen = emptyNeighbors[Random.Range(0, emptyNeighbors.Count)];
                    if (gridColors[chosen.x, chosen.y] == -1)
                    {
                        gridColors[chosen.x, chosen.y] = randomRegion;
                        regionCells[randomRegion].Add(chosen);
                        uncoloredCount--;
                    }
                }
            }

            // 5. Đóng gói thành LevelJsonData
            return BuildLevelJson(n, gridColors);
        }

        private static bool FindRandomValidQueens(int row, int[] queens, int n)
        {
            if (row >= n) return true;

            List<int> cols = new List<int>();
            for (int c = 0; c < n; c++) cols.Add(c);
            
            // Xáo trộn ngẫu nhiên cột để sinh level khác nhau mỗi lần chạy
            for (int i = cols.Count - 1; i > 0; i--)
            {
                int rnd = Random.Range(0, i + 1);
                int tmp = cols[i];
                cols[i] = cols[rnd];
                cols[rnd] = tmp;
            }

            foreach (int c in cols)
            {
                if (IsSafe(row, c, queens))
                {
                    queens[row] = c;
                    if (FindRandomValidQueens(row + 1, queens, n)) return true;
                }
            }
            return false;
        }

        private static bool IsSafe(int row, int col, int[] queens)
        {
            for (int r = 0; r < row; r++)
            {
                int c = queens[r];
                // Cùng cột
                if (c == col) return false;
                // Chạm nhau 8 hướng (kể cả chéo)
                if (Mathf.Abs(r - row) <= 1 && Mathf.Abs(c - col) <= 1) return false;
            }
            return true;
        }

        private static LevelJsonData BuildLevelJson(int n, int[,] gridColors)
        {
            LevelJsonData data = new LevelJsonData
            {
                n = n,
                requiredColors = new List<RequiredColorData>(),
                nodes = new List<NodeJsonData>()
            };

            // Tạo n màu riêng biệt phân bổ đều theo dải màu HSV
            ColorRGB[] palette = new ColorRGB[n];
            for (int i = 0; i < n; i++)
            {
                Color c = Color.HSVToRGB((float)i / n, 0.65f, 0.9f);
                palette[i] = new ColorRGB
                {
                    r = Mathf.RoundToInt(c.r * 255),
                    g = Mathf.RoundToInt(c.g * 255),
                    b = Mathf.RoundToInt(c.b * 255)
                };

                data.requiredColors.Add(new RequiredColorData
                {
                    r = palette[i].r,
                    g = palette[i].g,
                    b = palette[i].b,
                    requiredCount = 1
                });
            }

            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++)
                {
                    int region = gridColors[r, c];
                    data.nodes.Add(new NodeJsonData
                    {
                        row = r,
                        col = c,
                        r = palette[region].r,
                        g = palette[region].g,
                        b = palette[region].b
                    });
                }
            }

            return data;
        }
        
        /// <summary>
        /// Sinh level hoàn chỉnh tự động dựa trên chỉ số level (từ 30 đến 100)
        /// Tự tính kích thước ma trận và bảng màu riêng biệt.
        /// </summary>
        public static LevelJsonData GenerateLevelByDifficulty(int levelIndex)
        {
            int n = GetPacingMatrixSize(levelIndex);

            int[] queens = new int[n];
            if (!FindRandomValidQueens(0, queens, n))
            {
                Debug.LogError($"[LevelGenerator] Không tìm được thế cờ hợp lệ cho Level {levelIndex} (n = {n})!");
                return null;
            }

            int[,] gridColors = new int[n, n];
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    gridColors[r, c] = -1;

            List<Vector2Int>[] regionCells = new List<Vector2Int>[n];
            for (int i = 0; i < n; i++)
            {
                int r = i;
                int c = queens[i];
                gridColors[r, c] = i;
                regionCells[i] = new List<Vector2Int> { new Vector2Int(r, c) };
            }

            List<Vector2Int> emptyNeighbors = new List<Vector2Int>();
            int uncoloredCount = (n * n) - n;

            while (uncoloredCount > 0)
            {
                int randomRegion = Random.Range(0, n);
                emptyNeighbors.Clear();

                foreach (var cell in regionCells[randomRegion])
                {
                    foreach (var dir in Directions)
                    {
                        int nr = cell.x + dir.x;
                        int nc = cell.y + dir.y;

                        if (nr >= 0 && nr < n && nc >= 0 && nc < n && gridColors[nr, nc] == -1)
                        {
                            emptyNeighbors.Add(new Vector2Int(nr, nc));
                        }
                    }
                }

                if (emptyNeighbors.Count > 0)
                {
                    Vector2Int chosen = emptyNeighbors[Random.Range(0, emptyNeighbors.Count)];
                    if (gridColors[chosen.x, chosen.y] == -1)
                    {
                        gridColors[chosen.x, chosen.y] = randomRegion;
                        regionCells[randomRegion].Add(chosen);
                        uncoloredCount--;
                    }
                }
            }

            return BuildLevelJsonWithCustomPalette(n, gridColors, levelIndex);
        }

        private static LevelJsonData BuildLevelJsonWithCustomPalette(int n, int[,] gridColors, int levelIndex)
        {
            LevelJsonData data = new LevelJsonData
            {
                n = n,
                requiredColors = new List<RequiredColorData>(),
                nodes = new List<NodeJsonData>()
            };

            // Tạo bảng màu độc bản cho từng level dựa trên chỉ số levelIndex
            // Sử dụng Golden Ratio (0.618033988749895f) để các màu cách đều nhau và không bị trùng lặp
            float baseHue = Mathf.Repeat(levelIndex * 0.618033988749895f, 1f);
            ColorRGB[] palette = new ColorRGB[n];

            for (int i = 0; i < n; i++)
            {
                float hue = Mathf.Repeat(baseHue + ((float)i / n), 1f);
                // Giữ độ bão hòa và độ sáng trong ngưỡng màu tươi tắn
                float sat = (i % 2 == 0) ? 0.65f : 0.8f;
                float val = (i % 2 == 0) ? 0.95f : 0.85f;

                Color c = Color.HSVToRGB(hue, sat, val);
                palette[i] = new ColorRGB
                {
                    r = Mathf.RoundToInt(c.r * 255),
                    g = Mathf.RoundToInt(c.g * 255),
                    b = Mathf.RoundToInt(c.b * 255)
                };

                data.requiredColors.Add(new RequiredColorData
                {
                    r = palette[i].r,
                    g = palette[i].g,
                    b = palette[i].b,
                    requiredCount = 1
                });
            }

            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++)
                {
                    int region = gridColors[r, c];
                    data.nodes.Add(new NodeJsonData
                    {
                        row = r,
                        col = c,
                        r = palette[region].r,
                        g = palette[region].g,
                        b = palette[region].b
                    });
                }
            }

            return data;
        }
        
        /// <summary>
        /// Tính toán kích thước bàn cờ theo nhịp lượn sóng (ví dụ: 7 -> 8 -> 9 -> 7 -> 9 -> 8...)
        /// </summary>
        private static int GetPacingMatrixSize(int levelIndex)
        {
            if (levelIndex < 55)
            {
                int[] cycle = { 7, 8, 9, 7, 9, 8 };
                return cycle[(levelIndex - 30) % cycle.Length];
            }
            else if (levelIndex < 80)
            {
                int[] cycle = { 8, 9, 10, 8, 7, 10, 9 };
                return cycle[(levelIndex - 55) % cycle.Length];
            }
            else
            {
                int[] cycle = { 9, 10, 11, 8, 10, 9, 11 };
                return cycle[(levelIndex - 80) % cycle.Length];
            }
        }
    }
}