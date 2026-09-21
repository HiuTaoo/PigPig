using System;
using UnityEngine;

namespace _Script
{
    public class GameManager: MonoBehaviour
    {
        private GridManager gridManager;

        private void Awake()
        {
            gridManager = gameObject.GetComponentInChildren<GridManager>();
            //gridManager.SetSize(5);
            gridManager.InitializeGrid();
        }
    }
}