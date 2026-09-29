using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Script.UI
{
    public struct LosePopupArg
    {
        public int Level;

        public LosePopupArg(int level)
        {
            this.Level = level;
        }
    }
    
    public class LosePopupView: UIBaseView
    {
        [Header("Dynamic Texts")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI scoreText;

        [Header("Buttons")]
        [SerializeField] private Button replayButton;
        [SerializeField] private Button mainMenuButton;
        
        public override void OnInit()
        {
            base.OnInit();
            replayButton.onClick.AddListener(OnReplayButtonClicked);
            mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
        }

        public override void OnOpen(object args = null)
        {
            base.OnOpen(args);

            if (args is LosePopupArg data)
            {
                if (levelText != null) levelText.text = $"LEVEL {data.Level}";
            }
        }

        private void OnReplayButtonClicked()
        {
            OnClose();
            LevelManager.Instance.Replay();
        }

        private void OnMainMenuButtonClicked()
        {
            GameManager.Instance.BackToMainMenu();
        }

        private void OnDestroy()
        {
            replayButton.onClick.RemoveListener(OnReplayButtonClicked);
            mainMenuButton.onClick.RemoveListener(OnMainMenuButtonClicked);
        }
    }
}