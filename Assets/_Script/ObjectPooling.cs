using System.Collections.Generic;
using UnityEngine;

namespace _Script
{
    public class ObjectPooler : MonoBehaviour
    {
        public static ObjectPooler Instance { get; private set; }

        [Header("Số lượng khởi tạo trước (Preload)")]
        [SerializeField] private int initialCubeCount = 128;
        [SerializeField] private int initialPigCountPerType = 10;

        private readonly Queue<GameObject> cubePool = new Queue<GameObject>();

        private readonly Dictionary<int, Queue<GameObject>> pigPools = new Dictionary<int, Queue<GameObject>>();

        private Transform poolContainer;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                poolContainer = new GameObject("[ObjectPool_Container]").transform;
                poolContainer.SetParent(transform);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            PrewarmPools();
        }

        /// <summary>
        /// Tạo sẵn các đối tượng vào pool khi bắt đầu game
        /// </summary>
        private void PrewarmPools()
        {
            if (PrefabConfig.Instance == null) return; 

            // 1. Tạo trước Cube
            if (PrefabConfig.Instance.cube != null) 
            {
                for (int i = 0; i < initialCubeCount; i++)
                {
                    GameObject obj = CreateNewInstance(PrefabConfig.Instance.cube); 
                    cubePool.Enqueue(obj);
                }
            }

            // 2. Tạo trước từng loại Pig
            if (PrefabConfig.Instance.pig != null) 
            {
                for (int i = 0; i < PrefabConfig.Instance.pig.Length; i++) 
                {
                    GameObject pigPrefab = PrefabConfig.Instance.pig[i]; 
                    if (pigPrefab == null) continue;

                    int prefabId = pigPrefab.GetInstanceID();
                    if (!pigPools.ContainsKey(prefabId))
                    {
                        pigPools[prefabId] = new Queue<GameObject>();
                    }

                    for (int j = 0; j < initialPigCountPerType; j++)
                    {
                        GameObject obj = CreateNewInstance(pigPrefab);
                        pigPools[prefabId].Enqueue(obj);
                    }
                }
            }
        }

        #region CUBE POOLING

        public GameObject GetCube(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            GameObject obj;

            if (cubePool.Count > 0)
            {
                obj = cubePool.Dequeue();
            }
            else
            {
                obj = CreateNewInstance(PrefabConfig.Instance.cube); 
            }

            obj.transform.SetParent(parent);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);

            return obj;
        }

        public void ReturnCube(GameObject cubeObj)
        {
            if (cubeObj == null) return;

            cubeObj.SetActive(false);
            cubeObj.transform.SetParent(poolContainer);
            cubePool.Enqueue(cubeObj);
        }

        #endregion

        #region PIG POOLING

        /// <summary>
        /// Lấy lợn từ pool theo index trong mảng PrefabConfig.Instance.pig
        /// </summary>
        public GameObject GetPig(int pigTypeIndex, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (PrefabConfig.Instance == null || PrefabConfig.Instance.pig == null || PrefabConfig.Instance.pig.Length == 0) //
            {
                Debug.LogWarning("[ObjectPooler] Danh sách Pig Prefab trống!");
                return null;
            }

            int safeIndex = Mathf.Clamp(pigTypeIndex, 0, PrefabConfig.Instance.pig.Length - 1); 
            GameObject pigPrefab = PrefabConfig.Instance.pig[safeIndex]; 
            return GetPig(pigPrefab, position, rotation, parent);
        }

        /// <summary>
        /// Lấy lợn từ pool theo trực tiếp Prefab tham chiếu
        /// </summary>
        public GameObject GetPig(GameObject pigPrefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (pigPrefab == null) return null;

            int prefabId = pigPrefab.GetInstanceID();
            if (!pigPools.ContainsKey(prefabId))
            {
                pigPools[prefabId] = new Queue<GameObject>();
            }

            GameObject obj;
            Queue<GameObject> pool = pigPools[prefabId];

            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
            }
            else
            {
                obj = CreateNewInstance(pigPrefab);
            }

            obj.transform.SetParent(parent);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);

            return obj;
        }

        /// <summary>
        /// Trả lợn về pool. Cần truyền đúng prefab gốc của nó để đưa vào đúng hàng đợi.
        /// </summary>
        public void ReturnPig(GameObject pigObj, GameObject originalPrefab)
        {
            if (pigObj == null || originalPrefab == null) return;

            int prefabId = originalPrefab.GetInstanceID();
            if (!pigPools.ContainsKey(prefabId))
            {
                pigPools[prefabId] = new Queue<GameObject>();
            }

            pigObj.SetActive(false);
            pigObj.transform.SetParent(poolContainer);
            pigPools[prefabId].Enqueue(pigObj);
        }

        #endregion

        private GameObject CreateNewInstance(GameObject prefab)
        {
            GameObject obj = Instantiate(prefab, poolContainer);
            obj.SetActive(false);
            return obj;
        }
    }
}