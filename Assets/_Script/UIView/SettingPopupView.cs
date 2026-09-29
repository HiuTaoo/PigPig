using System;
using UnityEngine;
using UnityEngine.UI;

namespace _Script.UI
{
    public class SettingPopupView: UIBaseView
    {
        [SerializeField] Button replayButton;
        [SerializeField] Button closeButton;
        
        [SerializeField] Slider soundVolumeSlider;
        [SerializeField] Slider sfxVolumeSlider;
        [SerializeField] Toggle vibrateToggle;

        public override void OnInit()
        {
            base.OnInit();
            replayButton.onClick.AddListener(OnReplayButtonClicked);
            closeButton.onClick.AddListener(OnClose);
        }

        public override void OnOpen(object args = null)
        {
            base.OnOpen(args);
            Time.timeScale = 0;
        }

        public override void OnClose()
        {
            base.OnClose();
            Time.timeScale = 1;
        }

        private void OnDestroy()
        {
            replayButton.onClick.RemoveListener(OnReplayButtonClicked);
            closeButton.onClick.RemoveListener(OnClose);
        }

        private void OnReplayButtonClicked()
        {
            OnClose();
            LevelManager.Instance.Replay();
        }
        
    }
}