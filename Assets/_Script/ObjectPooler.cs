using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace _Script
{
    public class ObjectPooler : MonoBehaviour
    {
        public static ObjectPooler Instance { get; private set; }

        [Header("Số lượng khởi tạo mặc định (Preload)")]
        [SerializeField] private int defaultPoolCount = 32;
        [SerializeField] private int cubePreloadCount = 100;

        // Lưu trữ tất cả các pool theo InstanceID của Prefab gốc
        private readonly Dictionary<int, Queue<GameObject>> poolDictionary = new Dictionary<int, Queue<GameObject>>();
        // Giúp tra cứu xem 1 instance đang chạy thuộc về prefab gốc nào khi cần Return
        private readonly Dictionary<int, int> instanceToPrefabMap = new Dictionary<int, int>();

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
            AutoPrewarmAllPrefabConfig();
        }

        /// <summary>
        /// Tự động quét toàn bộ các biến GameObject và GameObject[] trong PrefabConfig để tạo pool
        /// </summary>
        private void AutoPrewarmAllPrefabConfig()
        {
            if (PrefabConfig.Instance == null) return;

            FieldInfo[] fields = typeof(PrefabConfig).GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                // 1. Nếu biến là GameObject đơn (cube, whiteFlower, redFlower, ...)
                if (field.FieldType == typeof(GameObject))
                {
                    GameObject prefab = field.GetValue(PrefabConfig.Instance) as GameObject;
                    if (prefab != null)
                    {
                        int count = (field.Name.ToLower().Contains("cube")) ? cubePreloadCount : defaultPoolCount;
                        PrewarmPrefab(prefab, count);
                    }
                }
                // 2. Nếu biến là mảng GameObject[] (mảng pig, ...)
                else if (field.FieldType == typeof(GameObject[]))
                {
                    GameObject[] prefabArray = field.GetValue(PrefabConfig.Instance) as GameObject[];
                    if (prefabArray != null)
                    {
                        foreach (GameObject prefab in prefabArray)
                        {
                            if (prefab != null)
                            {
                                PrewarmPrefab(prefab, defaultPoolCount);
                            }
                        }
                    }
                }
            }
        }

        private void PrewarmPrefab(GameObject prefab, int count)
        {
            int prefabId = prefab.GetInstanceID();
            if (!poolDictionary.ContainsKey(prefabId))
            {
                poolDictionary[prefabId] = new Queue<GameObject>();
            }

            for (int i = 0; i < count; i++)
            {
                GameObject obj = CreateNewInstance(prefab, prefabId);
                poolDictionary[prefabId].Enqueue(obj);
            }
        }

        #region GENERAL POOL API

        /// <summary>
        /// Lấy đối tượng từ pool dựa trên bất kỳ Prefab nào
        /// </summary>
        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;

            int prefabId = prefab.GetInstanceID();
            if (!poolDictionary.ContainsKey(prefabId))
            {
                poolDictionary[prefabId] = new Queue<GameObject>();
            }

            GameObject obj;
            Queue<GameObject> pool = poolDictionary[prefabId];

            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
            }
            else
            {
                obj = CreateNewInstance(prefab, prefabId);
            }

            obj.transform.SetParent(parent);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);

            return obj;
        }

        /// <summary>
        /// Thu hồi đối tượng về pool (Tự động nhận diện không cần truyền prefab gốc)
        /// </summary>
        public void Return(GameObject instanceObj)
        {
            if (instanceObj == null) return;

            int instanceId = instanceObj.GetInstanceID();

            // Tìm prefab ID gốc đã tạo ra instance này
            if (instanceToPrefabMap.TryGetValue(instanceId, out int prefabId))
            {
                if (poolDictionary.TryGetValue(prefabId, out var pool))
                {
                    instanceObj.SetActive(false);
                    instanceObj.transform.SetParent(poolContainer);
                    pool.Enqueue(instanceObj);
                    return;
                }
            }

            // Nếu đối tượng không xuất phát từ pool thì Destroy thông thường
            Destroy(instanceObj);
        }

        #endregion

        #region CÁC HÀM TIỆN ÍCH CŨ ĐỂ KHÔNG BỊ LỖI DỰ ÁN

        public GameObject GetCube(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            return Get(PrefabConfig.Instance.cube, position, rotation, parent);
        }

        public GameObject GetPig(int pigTypeIndex, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (PrefabConfig.Instance == null || PrefabConfig.Instance.pig == null || PrefabConfig.Instance.pig.Length == 0)
                return null;

            int safeIndex = Mathf.Clamp(pigTypeIndex, 0, PrefabConfig.Instance.pig.Length - 1);
            return Get(PrefabConfig.Instance.pig[safeIndex], position, rotation, parent);
        }

        public GameObject GetWhiteFlower(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            return Get(PrefabConfig.Instance.whiteFlower, position, rotation, parent);
        }

        public GameObject GetRedFlower(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            return Get(PrefabConfig.Instance.redFlower, position, rotation, parent);
        }

        public void ReturnCube(GameObject cubeObj) => Return(cubeObj);

        public void ReturnPig(GameObject pigObj, GameObject originalPrefab = null) => Return(pigObj);

        #endregion

        private GameObject CreateNewInstance(GameObject prefab, int prefabId)
        {
            GameObject obj = Instantiate(prefab, poolContainer);
            obj.SetActive(false);
            // Ghi nhớ đối tượng này sinh ra từ Prefab nào
            instanceToPrefabMap[obj.GetInstanceID()] = prefabId;
            return obj;
        }
    }
}