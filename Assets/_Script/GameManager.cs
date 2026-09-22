using System;
using UnityEngine;

namespace _Script
{
    public class GameManager: MonoBehaviour
    {
        public static GameManager Instance;
        private GridManager gridManager;
        private LevelManager levelManager;

        private void Awake()
        {
            if(Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
            }
            
            gridManager = gameObject.GetComponentInChildren<GridManager>();
            levelManager = gameObject.GetComponentInChildren<LevelManager>();

            levelManager.InitLevel();
            
        }

        public void HandleGridLoaded()
        {
            
        }
        public GridManager GetGridManager()
        {
            return gridManager;
        }
    }
}