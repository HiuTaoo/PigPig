using System;
using System.Collections.Generic;
using _Script.Events;
using UnityEngine;

namespace _Script.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Danh sách View được kéo thả sẵn hoặc nạp động")]
        [SerializeField] private List<UIBaseView> initialViews = new List<UIBaseView>();

        private readonly Dictionary<UIID, UIBaseView> viewRegistry = new Dictionary<UIID, UIBaseView>();
        private readonly Stack<UIBaseView> viewHistory = new Stack<UIBaseView>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                RegisterInitialViews();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void RegisterInitialViews()
        {
            foreach (var view in initialViews)
            {
                if (view != null && !viewRegistry.ContainsKey(view.ViewID))
                {
                    view.OnInit();
                    view.OnClose(); // Ẩn mặc định ban đầu
                    viewRegistry.Add(view.ViewID, view);
                }
            }
        }

        public T GetView<T>(UIID id) where T : UIBaseView
        {
            if (viewRegistry.TryGetValue(id, out var view))
            {
                return view as T;
            }
            return null;
        }

        /// <summary>
        /// Mở một màn hình/popup và truyền data nếu có
        /// </summary>
        public T OpenView<T>(UIID id, object args = null) where T : UIBaseView
        {
            if (viewRegistry.TryGetValue(id, out var view))
            {
                view.OnOpen(args);
                viewHistory.Push(view);
                return view as T;
            }

            Debug.LogError($"[UIManager] Không tìm thấy UI với ID: {id}");
            return null;
        }

        public void CloseView(UIID id)
        {
            if (viewRegistry.TryGetValue(id, out var view) && view.IsOpen)
            {
                view.OnClose();
            }
        }

        /// <summary>
        /// Đóng màn hình/popup trên cùng (Hỗ trợ nút Back / Escape)
        /// </summary>
        public void CloseTopView()
        {
            while (viewHistory.Count > 0)
            {
                var top = viewHistory.Pop();
                if (top != null && top.IsOpen)
                {
                    top.OnClose();
                    break;
                }
            }
        }

        private void OnGameOver(GameOverEvent gameOverEvent)
        {
            var data = new LosePopupArg(gameOverEvent.Level);
            OpenView<LosePopupView>(UIID.LosePopup, data);
        }

        private void OnEnable()
        {
            EventManager.Subscribe<GameOverEvent>(OnGameOver);
        }

        public void OnDisable()
        {
            EventManager.Unsubscribe<GameOverEvent>(OnGameOver);
        }
    }
}