using System;
using _Script.Events;
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

        private void OnEnable()
        {
            EventManager.Subscribe<CellClickedEvent>(OnCellClicked);
        }
        
        private void OnDisable()
        {
            EventManager.Unsubscribe<CellClickedEvent>(OnCellClicked);
        }
        
        private void OnCellClicked(CellClickedEvent e)
        {
            Debug.Log($"Người chơi vừa click vào ô hàng {e.ClickedNode.row}, cột {e.ClickedNode.col}");
            /*var pig = gridManager.GetPigAtColumn(e.ClickedNode.col);
            if (pig == null) return;
            var pigComponent = pig.GetComponent<Pig>();
            pigComponent.MoveToNode(e.ClickedNode);*/

        }
        
        public GridManager GetGridManager()
        {
            return gridManager;
        }
    }
}