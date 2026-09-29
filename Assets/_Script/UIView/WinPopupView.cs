using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _Script.Events;

namespace _Script.UI
{
    public struct WinPopupArgs
    {
        public int Level;
        public int Score;
        public int Health;

        public WinPopupArgs(int level, int score, int health)
        {
            Level = level;
            Score = score;
            Health = health;
        }
    }

    public class WinPopupView : UIBaseView
    {
        [Header("Dynamic Texts")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private GameObject starParent;

        [Header("Buttons")]
        [SerializeField] private Button okButton;

        public override void OnInit()
        {
            base.OnInit();
            okButton.onClick.AddListener(OnOkButtonClicked);
        }

        public override void OnOpen(object args = null)
        {
            base.OnOpen(args);

            if (args is not WinPopupArgs data) return;
            if (levelText != null) levelText.text = $"LEVEL {data.Level}";
            if (scoreText != null) scoreText.text = data.Score.ToString("N0");
            Debug.Log($"Health: {data.Health}");
            for (var i = 0; i < data.Health; i++)
            {
                var star = ObjectPooler.Instance.Get(PrefabConfig.Instance.starUIPrefab, 
                    starParent.transform.position, Quaternion.identity, starParent.transform);
                star.SetActive(true);
            }
        }

        private void OnOkButtonClicked()
        {
            OnClose();
            LevelManager.Instance.HandleLevelUp();
        }

        private void OnDestroy()
        {
            okButton.onClick.RemoveListener(OnOkButtonClicked);
        }
    }
}