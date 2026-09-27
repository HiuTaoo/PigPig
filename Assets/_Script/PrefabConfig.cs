using System;
using UnityEngine;

namespace _Script
{
    public class PrefabConfig: MonoBehaviour
    {
        public static PrefabConfig Instance;
        public GameObject[] pig;
        public GameObject cube;
        public GameObject whiteFlower;
        public GameObject redFlower;
        public GameObject colorUIIcon;
        public GameObject pigUIIcon;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
            }
        }
    }
}