using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Script.UI
{
    public struct LosePopupArg
    {
        public int level;
        public int score;

        public LosePopupArg(int level, int score)
        {
            this.level = level;
            this.score = score;
        }
    }
    
    public class LosePopupView: UIBaseView
    {
        [Header("Dynamic Texts")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI scoreText;

        [Header("Buttons")]
        [SerializeField] private Button replayButton;
        
        public override void OnInit()
        {
            base.OnInit();
            replayButton.onClick.AddListener(OnReplayButtonClicked);
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

        private void OnReplayButtonClicked()
        {
            OnClose();
            LevelManager.Instance.Replay();
        }

        private void OnDestroy()
        {
            replayButton.onClick.RemoveListener(OnReplayButtonClicked);
        }
    }
}