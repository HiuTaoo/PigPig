using System;
using _Script.Events;
using UnityEngine;

namespace _Script.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private const string MusicKey = "SETTING_MUSIC_MUTED";
        private const string SfxKey = "SETTING_SFX_MUTED";
        private const string MusicVolumeKey = "SETTING_MUSIC_VOLUME";
        private const string SfxVolumeKey = "SETTING_SFX_VOLUME";

        [SerializeField] private AudioConfig audioConfig;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        private bool IsMusicMuted { get; set; }
        private bool IsSFXMuted { get; set; }
        
        private float savedMusicVolume = 1f;
        private float savedSfxVolume = 1f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitSettings();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            EventManager.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventManager.Subscribe<CellInCorrectClickEvent>(OnCellInCorrectClick);
            EventManager.Subscribe<CellCorrectClickEvent>(OnCellCorrectClick);
            EventManager.Subscribe<GameOverEvent>(OnGameOver);
        }

        public float MusicVolume => musicSource.volume;
        public void SetMusicVolume(float volume)
        {
            musicSource.volume = Mathf.Clamp01(volume);
            musicSource.mute = musicSource.volume <= 0.001f;
            PlayerPrefs.SetFloat("SETTING_MUSIC_VOLUME", musicSource.volume);
            PlayerPrefs.Save();
        }

        public float SfxVolume => sfxSource.volume;
        public void SetSfxVolume(float volume)
        {
            sfxSource.volume = Mathf.Clamp01(volume);
            sfxSource.mute = sfxSource.volume <= 0.001f;
            PlayerPrefs.SetFloat("SETTING_SFX_VOLUME", sfxSource.volume);
            PlayerPrefs.Save();
        }

        public bool IsVibrateEnabled { get; private set; } = true;
        public void ToggleVibrate(bool isOn)
        {
            IsVibrateEnabled = isOn;
            PlayerPrefs.SetInt("SETTING_VIBRATE", isOn ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void InitSettings()
        {
            audioConfig.Initialize();

            IsMusicMuted = PlayerPrefs.GetInt(MusicKey, 0) == 1;
            IsSFXMuted = PlayerPrefs.GetInt(SfxKey, 0) == 1;
            
            savedMusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
            savedSfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);

            musicSource.volume = savedMusicVolume;
            musicSource.mute = IsMusicMuted || savedMusicVolume <= 0.001f;

            sfxSource.volume = savedSfxVolume;
            sfxSource.mute = IsSFXMuted || savedSfxVolume <= 0.001f;

            musicSource.mute = IsMusicMuted;
            sfxSource.mute = IsSFXMuted;
        }

        #region BGM Methods

        public void PlayBGM(MusicId id, bool loop = true)
        {
            var track = audioConfig.GetMusic(id);
            if (track == null || track.clip == null) return;

            if (musicSource.clip == track.clip && musicSource.isPlaying) return;

            musicSource.clip = track.clip;
            musicSource.loop = loop;
            musicSource.Play();
        }

        public void StopBGM() => musicSource.Stop();

        public void ToggleMusic(bool isOn)
        {
            IsMusicMuted = !isOn;
            musicSource.mute = IsMusicMuted;
            PlayerPrefs.SetInt(MusicKey, IsMusicMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        #endregion

        #region SFX Methods
        public void PlaySFX(SFXId id)
        {
            if (IsSFXMuted) return;

            var track = audioConfig.GetSFX(id);
            if (track == null || track.clip == null) return;

            sfxSource.pitch = track.pitch;
            sfxSource.PlayOneShot(track.clip, track.volume * sfxSource.volume);
        }

        public void ToggleSFX(bool isOn)
        {
            IsSFXMuted = !isOn;
            sfxSource.mute = IsSFXMuted;
            PlayerPrefs.SetInt(SfxKey, IsSFXMuted ? 1 : 0);
            PlayerPrefs.Save();
        }
        
        public void PlaySFXExclusive(SFXId id)
        {
            if (IsSFXMuted) return;

            var track = audioConfig.GetSFX(id);
            if (track == null || track.clip == null) return;

            sfxSource.Stop();

            sfxSource.clip = track.clip;
            sfxSource.pitch = track.pitch;
            sfxSource.Play();
        }

        public void StopSFX()
        {
            if (sfxSource != null && sfxSource.isPlaying)
            {
                sfxSource.Stop();
            }
        }

        #endregion

        #region Event Methods

        private void OnLevelCompleted(LevelCompletedEvent e)
        {
            PlaySFXExclusive(SFXId.Victory);
        }

        private void OnCellInCorrectClick(CellInCorrectClickEvent e)
        {
            PlaySFX(SFXId.IncorrectMove);
        }

        private void OnCellCorrectClick(CellCorrectClickEvent e)
        {
            PlaySFX(SFXId.PigPlaced);
        }

        private void OnGameOver(GameOverEvent e)
        {
            PlaySFXExclusive(SFXId.Defeat);
        }
        #endregion

        private void OnDestroy()
        {
            EventManager.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventManager.Unsubscribe<CellInCorrectClickEvent>(OnCellInCorrectClick);
            EventManager.Unsubscribe<CellCorrectClickEvent>(OnCellCorrectClick);
            EventManager.Unsubscribe<GameOverEvent>(OnGameOver);
        }
    }
}