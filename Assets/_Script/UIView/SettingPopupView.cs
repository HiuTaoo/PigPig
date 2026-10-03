using System;
using _Script.Audio;
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
            
            if (soundVolumeSlider != null)
                soundVolumeSlider.onValueChanged.AddListener(OnSoundVolumeChanged);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

            if (vibrateToggle != null)
                vibrateToggle.onValueChanged.AddListener(OnVibrateToggled);
        }

        public override void OnOpen(object args = null)
        {
            base.OnOpen(args);
            Time.timeScale = 0;
            
            if (AudioManager.Instance != null)
            {
                if (soundVolumeSlider != null)
                    soundVolumeSlider.value = AudioManager.Instance.MusicVolume;

                if (sfxVolumeSlider != null)
                    sfxVolumeSlider.value = AudioManager.Instance.SfxVolume;

                if (vibrateToggle != null)
                    vibrateToggle.isOn = AudioManager.Instance.IsVibrateEnabled;
            }
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
            
            if (soundVolumeSlider != null)
                soundVolumeSlider.onValueChanged.RemoveListener(OnSoundVolumeChanged);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);

            if (vibrateToggle != null)
                vibrateToggle.onValueChanged.RemoveListener(OnVibrateToggled);
        }

        private void OnReplayButtonClicked()
        {
            OnClose();
            LevelManager.Instance.Replay();
        }
        
        private void OnSoundVolumeChanged(float value)
        {
            AudioManager.Instance?.SetMusicVolume(value);
        }

        private void OnSFXVolumeChanged(float value)
        {
            AudioManager.Instance?.SetSfxVolume(value);
        }

        private void OnVibrateToggled(bool isOn)
        {
            AudioManager.Instance?.ToggleVibrate(isOn);
        }
    }
}