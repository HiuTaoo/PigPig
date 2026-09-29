using System.Collections.Generic;
using UnityEngine;
using _Script.Data;
using _Script.Events;
using _Script.UI;

namespace _Script
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        private const string CurrentLevelKey = "CURRENT_LEVEL";
        private int currentLevel { get; set; } = 1;
        private int _currentHealth = 3;

        public int currentHealth
        {
            get => _currentHealth;
            private set
            {
                _currentHealth = Mathf.Max(0, value);
                if (_currentHealth == 0)
                {
                    Debug.LogWarning("GameOver");
                    EventManager.Raise(new GameOverEvent(GetCurrentLevel()));
                }
            }
        }
        
        private float levelStartTime;
        private int toolUsedCount = 0;
        private const int TargetSolveTime = 120;

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
            
            currentLevel = PlayerPrefs.GetInt(CurrentLevelKey, 1);
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
            currentHealth = 3;

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
            for (int i = 0; i < currentHealth; i++)
            {
                var health = ObjectPooler.Instance.Get(PrefabConfig.Instance.pigUIIcon, parent.transform.position,
                    Quaternion.identity, parent.transform);
                healthIcons.Enqueue(health );
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
            int finalScore = CalculateFinalScore();
            var data = new WinPopupArgs(currentLevel, finalScore, currentHealth);
            UIManager.Instance.OpenView<WinPopupView>(UIID.WinPopup, data);
        }
        
        #endregion
        
        #region Tool Utilities

        /// <summary>
        /// TOOL 1: Tìm ra 1 vị trí có thể đặt lợn (hợp lệ và dẫn tới nghiệm thắng)
        /// </summary>
        public void Tool_FindSingleValidCell()
        {
            int n = CurrentLevelData != null ? CurrentLevelData.n : gridManager.Size;

            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++)
                {
                    Node node = gridManager.GetNode(r, c);
                    if (node == null || placedPigs.ContainsKey(node) || node.nodeStatus == NodeStatus.Incorrect) 
                        continue;

                    // Kiểm tra vừa đúng luật vừa dẫn tới chiến thắng
                    if (ValidatePlacement(node) && CanLeadToSolution(node))
                    {
                        GameManager.Instance.PlacePig(node);
                        return;
                    }
                }
            }

            Debug.LogWarning("[Tool 1] Không tìm thấy ô nào khả thi để đặt lợn!");
        }

        /// <summary>
        /// TOOL 2 Cải tiến:
        /// Vòng lặp hỗ trợ:
        /// 1. Nếu chưa có lợn: Đặt 1 lợn.
        /// 2. Nếu có lợn: Tìm xem có con lợn nào mà các ô xung quanh/cùng hàng/cột chưa được đánh dấu không -> Đánh dấu ô vi phạm quanh nó.
        /// 3. Nếu mọi con lợn hiện tại đều đã được đánh dấu sạch xung quanh -> Tự động đặt thêm 1 lợn mới.
        /// </summary>
        public void Tool_MarkInvalidCellsFromPigOrSuggest()
        {
            int n = CurrentLevelData != null ? CurrentLevelData.n : gridManager.Size;

            if (placedPigs.Count == 0)
            {
                Tool_FindSingleValidCell();
                return;
            }

            Node pigToMark = null;
            foreach (var pigNode in placedPigs.Keys)
            {
                if (HasUnmarkedViolatedCells(n, pigNode))
                {
                    pigToMark = pigNode;
                    break; 
                }
            }

            if (pigToMark != null)
            {
                MarkedAroundPig(n, pigToMark);
            }
            else
            {
                Debug.Log("[Tool 2] Tất cả lợn hiện tại đã được đánh dấu xung quanh. Đặt tiếp lợn mới!");
                Tool_FindSingleValidCell();
            }
        }

        /// <summary>
        /// Kiểm tra xem quanh con lợn này còn ô nào vi phạm luật mà chưa được gán Marked không
        /// </summary>
        private bool HasUnmarkedViolatedCells(int n, Node pigNode)
        {
            int pigR = pigNode.row;
            int pigC = pigNode.col;

            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++)
                {
                    Node target = gridManager.GetNode(r, c);
                    if (target == null || target == pigNode || placedPigs.ContainsKey(target))
                        continue;

                    bool isViolated = false;

                    if (target.row == pigR || target.col == pigC)
                    {
                        isViolated = true;
                    }
                    else if (Mathf.Abs(target.row - pigR) <= 1 && Mathf.Abs(target.col - pigC) <= 1)
                    {
                        isViolated = true;
                    }

                    if (isViolated && target.nodeStatus != NodeStatus.Marked)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void MarkedAroundPig(int n, Node chosenPigNode)
        {
            int pigR = chosenPigNode.row;
            int pigC = chosenPigNode.col;

            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++)
                {
                    Node target = gridManager.GetNode(r, c);
                    if (target == null || target == chosenPigNode || placedPigs.ContainsKey(target)) 
                        continue;

                    bool isViolated = false;

                    if (target.row == pigR || target.col == pigC)
                    {
                        isViolated = true;
                    }
                    else if (Mathf.Abs(target.row - pigR) <= 1 && Mathf.Abs(target.col - pigC) <= 1)
                    {
                        isViolated = true;
                    }

                    if (isViolated && target.nodeStatus != NodeStatus.Marked)
                    {
                        target.SetStatus(NodeStatus.Marked);
                        gridManager.SpawnMarker(target);
                    }
                }
            }
            
            Debug.Log($"[Tool 2] Đã đánh dấu toàn bộ ô phạm luật từ con lợn tại ô ({pigR}, {pigC})");
        }

        #region Tool 3 Solution-Based
        /// <summary>
        /// TOOL 3:
        /// 1. Tìm trước 1 nghiệm chiến thắng hoàn chỉnh cho cả bàn cờ.
        /// 2. Xác định các ô cần đặt lợn trong nghiệm đó (Target Placements).
        /// 3. Chọn ngẫu nhiên 3 ô xung quanh các vị trí đó mà KHÔNG THUỘC nghiệm để đánh dấu.
        /// -> Khi spam liên tục, chỉ duy nhất các ô chiến thắng còn lại!
        /// </summary>
        public void Tool_Mark3InvalidCellsNearValidPlacements()
        {
            int n = CurrentLevelData != null ? CurrentLevelData.n : gridManager.Size;

            // 1. Tìm toàn bộ nghiệm chuẩn của bàn cờ từ trạng thái lợn đã đặt hiện tại
            HashSet<Node> fullSolutionNodes = FindFullWinningSolution(n);
            if (fullSolutionNodes == null || fullSolutionNodes.Count == 0)
            {
                Debug.LogWarning("[Tool 3] Bàn cờ hiện tại đang ở thế ngõ cụt, không thể tìm nghiệm chiến thắng!");
                return;
            }

            // 2. Gom tất cả các ô xung quanh (8 ô lân cận) của các vị trí nghiệm mà CHƯA bị đánh dấu
            // Lưu ý: Tuyệt đối KHÔNG đánh dấu vào các ô nằm trong fullSolutionNodes!
            List<Node> candidatesToMark = new List<Node>();

            foreach (var winNode in fullSolutionNodes)
            {
                // Bỏ qua các ô đã được người chơi đặt lợn lên rồi
                if (placedPigs.ContainsKey(winNode)) continue;

                for (int dr = -1; dr <= 1; dr++)
                {
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        if (dr == 0 && dc == 0) continue;

                        int nr = winNode.row + dr;
                        int nc = winNode.col + dc;

                        if (nr >= 0 && nr < n && nc >= 0 && nc < n)
                        {
                            Node neighbor = gridManager.GetNode(nr, nc);
                            if (neighbor != null 
                                && neighbor.nodeStatus != NodeStatus.Marked 
                                && neighbor.nodeStatus != NodeStatus.Incorrect
                                && !placedPigs.ContainsKey(neighbor)
                                && !fullSolutionNodes.Contains(neighbor)) 
                            {
                                if (!candidatesToMark.Contains(neighbor))
                                {
                                    candidatesToMark.Add(neighbor);
                                }
                            }
                        }
                    }
                }
            }

            // 3. Nếu các ô 8 hướng quanh nghiệm đã bị đánh dấu hết, lấy bất kỳ ô rác nào trên bàn cờ
            if (candidatesToMark.Count < 3)
            {
                for (int r = 0; r < n; r++)
                {
                    for (int c = 0; c < n; c++)
                    {
                        Node node = gridManager.GetNode(r, c);
                        if (node != null 
                            && node.nodeStatus != NodeStatus.Marked 
                            && node.nodeStatus != NodeStatus.Incorrect
                            && !placedPigs.ContainsKey(node)
                            && !fullSolutionNodes.Contains(node))
                        {
                            if (!candidatesToMark.Contains(node))
                            {
                                candidatesToMark.Add(node);
                            }
                        }
                    }
                }
            }

            if (candidatesToMark.Count == 0)
            {
                Debug.Log("[Tool 3] Đã dọn sạch toàn bộ ô rác! Chỉ còn lại các ô chiến thắng.");
                return;
            }

            // 4. Chọn ngẫu nhiên tối đa 3 ô để spawn marker
            int countToMark = Mathf.Min(3, candidatesToMark.Count);
            for (int i = 0; i < countToMark; i++)
            {
                int randIdx = Random.Range(0, candidatesToMark.Count);
                Node target = candidatesToMark[randIdx];
                candidatesToMark.RemoveAt(randIdx);

                target.SetStatus(NodeStatus.Marked);
                gridManager.SpawnMarker(target);
            }

            Debug.Log($"[Tool 3] Đã đánh dấu {countToMark} ô vi phạm mà không chạm vào nghiệm chuẩn.");
        }

        /// <summary>
        /// Giải và trả về tập hợp toàn bộ vị trí Node cần đặt lợn để thắng màn chơi
        /// </summary>
        private HashSet<Node> FindFullWinningSolution(int n)
        {
            int[] queensPerRow = new int[n];
            for (int r = 0; r < n; r++) queensPerRow[r] = -1;

            Dictionary<int, int> simulatedColorCounts = new Dictionary<int, int>();

            // Đưa lợn đã đặt vào bàn cờ giải lập
            foreach (var node in placedPigs.Keys)
            {
                if (node.row >= 0 && node.row < n)
                {
                    queensPerRow[node.row] = node.col;
                    if (!simulatedColorCounts.ContainsKey(node.colorRegionID))
                        simulatedColorCounts[node.colorRegionID] = 0;
                    simulatedColorCounts[node.colorRegionID]++;
                }
            }

            // Chạy đệ quy tìm ra nghiệm
            if (SolveRowDetailed(0, queensPerRow, simulatedColorCounts, n))
            {
                HashSet<Node> solutionNodes = new HashSet<Node>();
                for (int r = 0; r < n; r++)
                {
                    if (queensPerRow[r] != -1)
                    {
                        Node solvedNode = gridManager.GetNode(r, queensPerRow[r]);
                        if (solvedNode != null)
                        {
                            solutionNodes.Add(solvedNode);
                        }
                    }
                }
                return solutionNodes;
            }

            return null;
        }

        private bool SolveRowDetailed(int row, int[] queensPerRow, Dictionary<int, int> colorCounts, int n)
        {
            if (row >= n)
            {
                foreach (var kvp in levelColorTargets)
                {
                    colorCounts.TryGetValue(kvp.Key, out int current);
                    if (current != kvp.Value) return false;
                }
                return true;
            }

            if (queensPerRow[row] != -1)
            {
                return SolveRowDetailed(row + 1, queensPerRow, colorCounts, n);
            }

            for (int col = 0; col < n; col++)
            {
                Node node = gridManager.GetNode(row, col);
                if (node == null || node.nodeStatus == NodeStatus.Incorrect) continue;

                if (IsSafePlacement(row, col, node.colorRegionID, queensPerRow, colorCounts, n))
                {
                    queensPerRow[row] = col;
                    if (!colorCounts.ContainsKey(node.colorRegionID))
                        colorCounts[node.colorRegionID] = 0;
                    colorCounts[node.colorRegionID]++;

                    if (SolveRowDetailed(row + 1, queensPerRow, colorCounts, n))
                    {
                        return true;
                    }

                    queensPerRow[row] = -1;
                    colorCounts[node.colorRegionID]--;
                }
            }

            return false;
        }

        #endregion
        #endregion

        public void HandleLevelUp()
        {
            currentLevel++; 
            PlayerPrefs.SetInt(CurrentLevelKey, currentLevel);
            PlayerPrefs.Save();
            
            Debug.Log($"[LevelManager] Bắt đầu màn chơi mới: Level {currentLevel}");
            InitLevel();
        }

        public void HandleIncorrectClick()
        {
            currentHealth--;
            Debug.Log($"Current Health: {currentHealth}");
        }

        public void Replay()
        {
            InitLevel();
        }
        
        public int CalculateFinalScore()
        {
            int baseScore = 1000 + (currentLevel * 100);

            int healthBonus = currentHealth * 300;

            float elapsedTime = Time.time - levelStartTime;
            int remainingSeconds = Mathf.Max(0, Mathf.FloorToInt(TargetSolveTime - elapsedTime));
            int timeBonus = remainingSeconds * 10;

            int toolPenalty = toolUsedCount * 150;

            int finalScore = Mathf.Max(baseScore / 2, baseScore + healthBonus + timeBonus - toolPenalty);

            Debug.Log($"[Score Breakdown] Base: {baseScore} | Health: +{healthBonus} | Time: +{timeBonus} | Penalty: -{toolPenalty} => Total: {finalScore}");
            return finalScore;
        }
        
        public void RegisterToolUsed()
        {
            toolUsedCount++;
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

        private int GetCurrentLevel()
        {
            return currentLevel;
        }
    }
}