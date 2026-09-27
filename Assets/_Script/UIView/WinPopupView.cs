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
        public int Reward;

        public WinPopupArgs(int level, int score, int reward)
        {
            Level = level;
            Score = score;
            Reward = reward;
        }
    }

    public class WinPopupView : UIBaseView
    {
        [Header("Dynamic Texts")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI scoreText;

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

            if (args is WinPopupArgs data)
            {
                if (levelText != null) levelText.text = $"LEVEL {data.Level}";
                if (scoreText != null) scoreText.text = data.Score.ToString("N0");
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