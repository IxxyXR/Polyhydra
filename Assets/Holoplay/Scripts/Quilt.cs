//Copyright 2017-2019 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System;
using UnityEngine;

namespace LookingGlass {
    public static class Quilt {
            
        // classes
        [Serializable]
        public struct Settings {
            [Range(256, 8192)] public int quiltWidth;
            [Range(256, 8192)] public int quiltHeight;
            [Range(1, 32)] public int viewColumns;
            [Range(1, 32)] public int viewRows;
            [Range(1, 128)] public int numViews;
            [System.NonSerialized] public int viewWidth;
            [System.NonSerialized] public int viewHeight;
            [System.NonSerialized] public int paddingHorizontal;
            [System.NonSerialized] public int paddingVertical;
            [System.NonSerialized] public float viewPortionHorizontal;
            [System.NonSerialized] public float viewPortionVertical;
            [Tooltip("To use the default aspect for the current Looking Glass, keep at -1")]
            public float aspect;
            [Tooltip("If custom aspect differs from current Looking Glass aspect, " +
                "this will toggle between overscan (zoom w/ crop) or letterbox (black borders)")]
            public bool overscan;

            public Settings(int quiltWidth, int quiltHeight, int viewColumns, int viewRows, 
                int numViews, float aspect = -1, bool overscan = false) : this() 
            {
                this.quiltWidth = quiltWidth;
                this.quiltHeight = quiltHeight;
                this.viewColumns = viewColumns;
                this.viewRows = viewRows;
                this.numViews = numViews;
                this.aspect = aspect;
                this.overscan = overscan;
                Setup(); 
            }
            public void Setup() {
                viewWidth = quiltWidth / viewColumns;
                viewHeight = quiltHeight / viewRows;
                viewPortionHorizontal = (float)viewColumns * viewWidth / quiltWidth;
                viewPortionVertical = (float)viewRows * viewHeight / quiltHeight;
                paddingHorizontal = quiltWidth - viewColumns * viewWidth;
                paddingVertical = quiltHeight - viewRows * viewHeight;
            }
            // todo: have an override that only takes view count, width, and height
            // and creates as square as possible quilt settings from that
        }
        public enum Preset {
            ExtraLow = 0,
            Standard = 1, 
            HiRes = 2, 
            UltraHi = 3,
            Automatic = -1,
            Custom = -2,
        }

        // variables
        public static readonly Settings[] presets = new Settings[] {
            new Settings(1600, 1440, 4, 6, 24), // extra low
            new Settings(2048, 2048, 4, 8, 32), // standard
            new Settings(4096, 4096, 5, 9, 45), // hi res
            new Settings(7680, 6400, 6, 8, 48), // ultra hi
        };

        // functions
        public static Settings GetPreset(Preset preset) {
            if (preset != Preset.Automatic) return presets[(int)preset];
            if (QualitySettings.lodBias > 2f) return presets[(int)Preset.UltraHi];
            if (QualitySettings.lodBias > 1f) return presets[(int)Preset.HiRes];
            if (QualitySettings.lodBias > 0.5f) return presets[(int)Preset.Standard];
            return presets[(int)Preset.ExtraLow];
        }
    }
}