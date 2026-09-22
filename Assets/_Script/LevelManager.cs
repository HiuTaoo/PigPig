using System;
using UnityEngine;

namespace _Script
{
    public class LevelManager: MonoBehaviour
    {
        public int currentLevel { get; private set; } = 1;
        
        private GridManager gridManager;
        private void Start()
        {
            gridManager = GameManager.Instance.GetGridManager();
        }

        public void InitLevel()
        {
            if (gridManager == null)
            {
                gridManager = GameManager.Instance.GetGridManager();
            }
            gridManager.InitializeGrid();
        }

        public void HandleLevelUp()
        {
            currentLevel++;
        }

        public void LoadLevel(int level)
        {
            currentLevel = level;
        }
    }
}