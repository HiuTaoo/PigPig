using System.Collections.Generic;
using UnityEngine;
using _Script.Data;
using _Script.UI;

namespace _Script
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        private int currentLevel { get; set; } = 1;
        private int heart = 3;

        public LevelJsonData CurrentLevelData { get; private set; }

        // Bảng màu & mục tiêu màu cho level
        private Dictionary<int, int> levelColorTargets = new Dictionary<int, int>(); 
        private Dictionary<int, Color> levelColorPalette = new Dictionary<int, Color>();

        private Dictionary<Node, Pig> placedPigs = new Dictionary<Node, Pig>(); 
        private GridManager gridManager; 

        private void Awake()
        {
            if (Instance == null) Instance = this; 
            else Destroy(gameObject); 
        }

        private void Start()
        {
            gridManager = GameManager.Instance.GetGridManager(); 
            InitLevel(); 
        }

        public void InitLevel()
        {
            if (gridManager == null)
                gridManager = GameManager.Instance.GetGridManager(); 

            placedPigs.Clear(); 
            levelColorTargets.Clear(); 
            levelColorPalette.Clear();

            CurrentLevelData = LevelDataReader.LoadLevelFromResources(currentLevel);
            if (CurrentLevelData == null) return;

            foreach (var reqColor in CurrentLevelData.requiredColors)
            {
                int colorId = reqColor.GetColorID();
                levelColorTargets[colorId] = reqColor.requiredCount;
                levelColorPalette[colorId] = reqColor.ToUnityColor();
            }

            gridManager.InitializeGrid(CurrentLevelData);
            ApplyNodeColorsFromData();
            var colors = new List<Color>();
            foreach (var color in levelColorPalette.Values)
            {
                colors.Add(color);
            }
            var data = new HUDViewData(currentLevel, colors);
            UIManager.Instance.OpenView<HUDView>(UIID.GameplayHUD, data);
        }

        /// <summary>
        /// Tô màu và gán ID cho từng ô theo mảng nodes trong JSON
        /// </summary>
        private void ApplyNodeColorsFromData()
        {
            if (CurrentLevelData == null || CurrentLevelData.nodes == null) return;

            foreach (var nodeData in CurrentLevelData.nodes)
            {
                Node node = gridManager.GetNode(nodeData.row, nodeData.col); 
                if (node != null)
                {
                    int colorId = nodeData.GetColorID();
                    Color color = nodeData.ToUnityColor();
                    node.Init(nodeData.row, nodeData.col, colorId, color); 
                }
            }
        }

        public Queue<GameObject> SpawnHealthUI(GameObject parent)
        {
            var healthIcons = new Queue<GameObject>();
            for (int i = 0; i < heart; i++)
            {
                var health = ObjectPooler.Instance.Get(PrefabConfig.Instance.pigUIIcon, parent.transform.position,
                    Quaternion.identity, parent.transform);
                healthIcons.Enqueue(health);
            }

            return healthIcons;
        }
        #region Rule
        public bool ValidatePlacement(Node targetNode)
        {
            if (targetNode == null || targetNode.nodeStatus == NodeStatus.Incorrect) return false; 

            if (placedPigs.ContainsKey(targetNode)) return false; 

            int currentCountInRegion = GetPlacedCountByColor(targetNode.colorRegionID); 
            if (levelColorTargets.TryGetValue(targetNode.colorRegionID, out int maxRequired)) 
            {
                if (currentCountInRegion >= maxRequired)
                {
                    Debug.Log($"[Luật] Vùng màu {targetNode.colorRegionID} đã đủ {maxRequired} con!"); 
                    return false; 
                }
            }

            foreach (var node in placedPigs.Keys) 
            {
                if (node.row == targetNode.row || node.col == targetNode.col) 
                {
                    Debug.Log($"[Luật] Đã có lợn cùng hàng {targetNode.row} hoặc cùng cột {targetNode.col}!"); 
                    return false; 
                }

                int rowDiff = Mathf.Abs(node.row - targetNode.row); 
                int colDiff = Mathf.Abs(node.col - targetNode.col); 
                if (rowDiff <= 1 && colDiff <= 1) 
                {
                    Debug.Log($"[Luật] Không thể đặt sát cạnh lợn ở ({node.row}, {node.col}) trong phạm vi 1 ô!"); 
                    return false; 
                }
            }

            return true; 
        }
        
        /// <summary>
        /// Kiểm tra xem giả lập đặt lợn tại ô candidateNode thì sau đó bàn cờ có thể giải thắng được không
        /// </summary>
        public bool CanLeadToSolution(Node candidateNode)
        {
            if (candidateNode == null) return false;

            int n = CurrentLevelData != null ? CurrentLevelData.n : gridManager.Size;

            // 1. Lưu trạng thái các hàng đã có lợn
            int[] queensPerRow = new int[n];
            for (int r = 0; r < n; r++) queensPerRow[r] = -1;

            // Nạp các con lợn hiện đã đặt trên bàn cờ
            foreach (var node in placedPigs.Keys)
            {
                if (node.row >= 0 && node.row < n)
                {
                    queensPerRow[node.row] = node.col;
                }
            }

            // Đặt thử ô ứng viên vào
            queensPerRow[candidateNode.row] = candidateNode.col;

            // Đếm số lượng lợn theo màu hiện tại (bao gồm cả ô ứng viên)
            Dictionary<int, int> simulatedColorCounts = new Dictionary<int, int>();
            foreach (var node in placedPigs.Keys)
            {
                if (!simulatedColorCounts.ContainsKey(node.colorRegionID))
                    simulatedColorCounts[node.colorRegionID] = 0;
                simulatedColorCounts[node.colorRegionID]++;
            }

            if (!simulatedColorCounts.ContainsKey(candidateNode.colorRegionID))
                simulatedColorCounts[candidateNode.colorRegionID] = 0;
            simulatedColorCounts[candidateNode.colorRegionID]++;

            // Kiểm tra ngay ô ứng viên có làm vượt chỉ tiêu màu không
            if (levelColorTargets.TryGetValue(candidateNode.colorRegionID, out int maxRequired))
            {
                if (simulatedColorCounts[candidateNode.colorRegionID] > maxRequired)
                    return false;
            }

            // 2. Chạy đệ quy quay lui để tìm xem có nghiệm cho các hàng còn lại không
            return SolveRow(0, queensPerRow, simulatedColorCounts, n);
        }

        private bool SolveRow(int row, int[] queensPerRow, Dictionary<int, int> colorCounts, int n)
        {
            // Đã xếp thành công cho cả n hàng
            if (row >= n)
            {
                // Kiểm tra xem tất cả các màu có đạt đúng số lượng mục tiêu chưa
                foreach (var kvp in levelColorTargets)
                {
                    colorCounts.TryGetValue(kvp.Key, out int current);
                    if (current != kvp.Value) return false;
                }
                return true;
            }

            // Hàng này đã có lợn (từ bàn cờ cũ hoặc chính là candidateNode) -> nhảy sang hàng tiếp theo
            if (queensPerRow[row] != -1)
            {
                return SolveRow(row + 1, queensPerRow, colorCounts, n);
            }

            // Thử từng cột trong hàng này
            for (int col = 0; col < n; col++)
            {
                Node node = gridManager.GetNode(row, col);
                if (node == null) continue;

                if (IsSafePlacement(row, col, node.colorRegionID, queensPerRow, colorCounts, n))
                {
                    // Đặt thử
                    queensPerRow[row] = col;
                    if (!colorCounts.ContainsKey(node.colorRegionID))
                        colorCounts[node.colorRegionID] = 0;
                    colorCounts[node.colorRegionID]++;

                    // Tiếp tục giải hàng kế
                    if (SolveRow(row + 1, queensPerRow, colorCounts, n))
                    {
                        return true; // Tìm thấy ít nhất 1 nghiệm thắng
                    }

                    // Backtrack (hoàn tác)
                    queensPerRow[row] = -1;
                    colorCounts[node.colorRegionID]--;
                }
            }

            return false; // Ngõ cụt
        }

        private bool IsSafePlacement(int row, int col, int colorId, int[] queensPerRow, Dictionary<int, int> colorCounts, int n)
        {
            // Kiểm tra chỉ tiêu màu
            if (levelColorTargets.TryGetValue(colorId, out int maxRequired))
            {
                colorCounts.TryGetValue(colorId, out int current);
                if (current >= maxRequired) return false;
            }

            // Kiểm tra va chạm với các quân lợn khác ở các hàng đã đặt
            for (int r = 0; r < n; r++)
            {
                int c = queensPerRow[r];
                if (c == -1) continue;

                // 1. Trùng cột
                if (c == col) return false;

                // 2. Phạm vi lân cận 1 ô (ngang, dọc, chéo 8 hướng)
                if (Mathf.Abs(r - row) <= 1 && Mathf.Abs(c - col) <= 1)
                    return false;
            }

            return true;
        }

        public void ConfirmPlacement(Pig pig, Node targetNode)
        {
            if (pig == null || targetNode == null) return; 

            placedPigs[targetNode] = pig; 

            pig.PlaceOnNode(targetNode);

            CheckWinCondition(); 
        }

        private void CheckWinCondition()
        {
            foreach (var kvp in levelColorTargets) 
            {
                if (GetPlacedCountByColor(kvp.Key) < kvp.Value) return; 
            }

            Debug.Log(">>> CHIẾN THẮNG LEVEL! <<<"); 
            HandleLevelUp(); 
        }
        
        #endregion

        public void HandleLevelUp()
        {
            currentLevel++; 
            Debug.Log($"[LevelManager] Bắt đầu màn chơi mới: Level {currentLevel}");
            InitLevel();
        }

        public void Replay()
        {
            InitLevel();
        }

        private int GetPlacedCountByColor(int colorRegionID)
        {
            int count = 0; 
            foreach (Node node in placedPigs.Keys) 
            {
                if (node != null && node.colorRegionID == colorRegionID) 
                    count++;
            }
            return count; 
        }
        
        public Color GetColorByRegionId(int regionId)
        {
            if (levelColorPalette.TryGetValue(regionId, out Color color))
            {
                return color;
            }

            Debug.LogWarning($"[LevelManager] Không tìm thấy màu cho ID: {regionId}");
            return Color.white;
        }
    }
}