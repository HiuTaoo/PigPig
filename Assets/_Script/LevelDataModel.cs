using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Script.Data
{
    [Serializable]
    public class ColorRGB
    {
        public int r;
        public int g;
        public int b;

        // Sinh ID định danh duy nhất từ bộ 3 (R, G, B) từ 0-255
        public int GetColorID()
        {
            return (r << 16) | (g << 8) | b;
        }

        public Color ToUnityColor()
        {
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }
    }

    [Serializable]
    public class RequiredColorData : ColorRGB
    {
        public int requiredCount = 1; 
    }

    [Serializable]
    public class NodeJsonData : ColorRGB
    {
        public int row;
        public int col;
    }

    [Serializable]
    public class LevelJsonData
    {
        public int n; 
        public List<RequiredColorData> requiredColors;
        public List<NodeJsonData> nodes;
    }
}