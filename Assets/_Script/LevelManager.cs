using System.Collections.Generic;
using UnityEngine;
using _Script.Data;

namespace _Script
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        public int currentLevel { get; private set; } = 1; 

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

            gridManager.SetSize(CurrentLevelData.n); 
            ApplyNodeColorsFromData();
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

        public bool ValidatePlacement(Node targetNode)
        {
            if (targetNode == null) return false; 

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

        public void HandleLevelUp()
        {
            currentLevel++; 
            InitLevel();
        }

        public int GetPlacedCountByColor(int colorRegionID)
        {
            int count = 0; 
            foreach (Node node in placedPigs.Keys) 
            {
                if (node != null && node.colorRegionID == colorRegionID) 
                    count++;
            }
            return count; 
        }
    }
}