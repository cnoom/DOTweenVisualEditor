using System;
using System.Collections.Generic;
using UnityEngine;

namespace CNoom.DOTweenVisual.Data
{
    /// <summary>
    /// 轨道数据
    /// </summary>
    [Serializable]
    public class TrackData
    {
        [Tooltip("轨道名称")]
        public string TrackName = "Track";

        [Tooltip("轨道颜色")]
        public Color TrackColor = new Color(0.3f, 0.7f, 1f);

        [Tooltip("静音（不播放）")]
        public bool IsMuted;

        [Tooltip("独奏（只播放此轨道）")]
        public bool IsSolo;

        [Tooltip("片段列表")]
        public List<ClipData> Clips = new();

        /// <summary>
        /// 轨道实际持续时间（根据片段计算）
        /// </summary>
        public float CalculateDuration()
        {
            if (Clips == null || Clips.Count == 0) return 0f;

            float maxEnd = 0f;
            foreach (var clip in Clips)
            {
                if (clip.EndTime > maxEnd)
                    maxEnd = clip.EndTime;
            }
            return maxEnd;
        }
    }
}
