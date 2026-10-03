using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Script.Audio
{
    [Serializable]
    public class MusicTrack
    {
        public MusicId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Serializable]
    public class SFXTrack
    {
        public SFXId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.8f, 1.2f)] public float pitch = 1f; // Chống nhàm chán khi spam click
    }

    [CreateAssetMenu(fileName = "AudioConfig", menuName = "Config/AudioConfig")]
    public class AudioConfig : ScriptableObject
    {
        public List<MusicTrack> musicTracks = new List<MusicTrack>();
        public List<SFXTrack> sfxTracks = new List<SFXTrack>();

        private Dictionary<MusicId, MusicTrack> _musicDict;
        private Dictionary<SFXId, SFXTrack> _sfxDict;

        public void Initialize()
        {
            _musicDict = new Dictionary<MusicId, MusicTrack>();
            foreach (var m in musicTracks)
            {
                if (!_musicDict.ContainsKey(m.id)) _musicDict.Add(m.id, m);
            }

            _sfxDict = new Dictionary<SFXId, SFXTrack>();
            foreach (var s in sfxTracks)
            {
                if (!_sfxDict.ContainsKey(s.id)) _sfxDict.Add(s.id, s);
            }
        }

        public MusicTrack GetMusic(MusicId id)
        {
            if (_musicDict == null) Initialize();
            _musicDict.TryGetValue(id, out var track);
            return track;
        }

        public SFXTrack GetSFX(SFXId id)
        {
            if (_sfxDict == null) Initialize();
            _sfxDict.TryGetValue(id, out var track);
            return track;
        }
    }
}