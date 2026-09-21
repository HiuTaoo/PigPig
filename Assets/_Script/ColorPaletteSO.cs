using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Script
{
    [Serializable]
    public struct RegionColorEntry
    {
        public int regionId;
        public string regionName; 
        public Color color;       
    }

    [CreateAssetMenu(fileName = "GlobalColorPalette", menuName = "Puzzle/Color Palette")]
    public class ColorPaletteSO : ScriptableObject
    {
        [Header("Danh sách màu RGB theo ID")]
        [SerializeField] private List<RegionColorEntry> colorEntries = new List<RegionColorEntry>();

        private Dictionary<int, Color> colorDict;

        private void OnEnable()
        {
            InitDictionary();
        }

        private void OnValidate()
        {
            InitDictionary();
        }

        private void InitDictionary()
        {
            colorDict = new Dictionary<int, Color>();
            foreach (var entry in colorEntries)
            {
                colorDict[entry.regionId] = entry.color;
            }
        }

        /// <summary>
        /// Lấy màu RGB dựa theo regionID. Nếu không tìm thấy trả về màu mặc định (trắng/xám).
        /// </summary>
        public Color GetColor(int regionId)
        {
            if (colorDict == null || colorDict.Count == 0)
            {
                InitDictionary();
            }

            if (colorDict.TryGetValue(regionId, out Color foundColor))
            {
                return foundColor;
            }

            Debug.LogWarning($"[ColorPaletteSO] Không tìm thấy màu cho ID: {regionId}. Sử dụng màu trắng mặc định.");
            return Color.white;
        }

        /// <summary>
        /// Thêm màu RGB mới bằng code (nếu muốn thêm động lúc runtime)
        /// </summary>
        public void AddColor(int id, Color newColor, string name = "")
        {
            colorEntries.Add(new RegionColorEntry
            {
                regionId = id,
                color = newColor,
                regionName = string.IsNullOrEmpty(name) ? $"Color_{id}" : name
            });
            InitDictionary();
        }

        /// <summary>
        /// Thêm màu bằng hệ RGB nguyên (0 - 255)
        /// </summary>
        public void AddColorRGB255(int id, byte r, byte g, byte b, string name = "")
        {
            Color c = new Color32(r, g, b, 255);
            AddColor(id, c, name);
        }
    }
}