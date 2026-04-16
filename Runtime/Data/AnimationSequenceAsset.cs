using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace CNoom.DOTweenVisual.Data
{
    /// <summary>
    /// 动画序列资源
    /// 存储动画序列的纯数据，可复用、可版本控制
    /// </summary>
    [CreateAssetMenu(fileName = "NewAnimationSequence", menuName = "DOTween Visual/Animation Sequence")]
    public class AnimationSequenceAsset : ScriptableObject
    {
        [Header("基本信息")]
        [Tooltip("序列名称")]
        public string SequenceName = "Animation Sequence";

        [Header("循环设置")]
        [Tooltip("循环次数，-1 表示无限循环")]
        public int Loops = 1;

        [Tooltip("循环类型")]
        public LoopType LoopType = LoopType.Restart;

        [Header("轨道")]
        [Tooltip("轨道列表")]
        public List<TrackData> Tracks = new();

        /// <summary>
        /// 计算序列总持续时间
        /// </summary>
        public float CalculateTotalDuration()
        {
            if (Tracks == null || Tracks.Count == 0) return 0f;

            float maxDuration = 0f;
            foreach (var track in Tracks)
            {
                var trackDuration = track.CalculateDuration();
                if (trackDuration > maxDuration)
                    maxDuration = trackDuration;
            }
            return maxDuration;
        }

        /// <summary>
        /// 获取所有非静音轨道
        /// </summary>
        public List<TrackData> GetActiveTracks()
        {
            var result = new List<TrackData>();

            // 检查是否有独奏轨道
            bool hasSolo = false;
            foreach (var track in Tracks)
            {
                if (track.IsSolo && !track.IsMuted)
                {
                    hasSolo = true;
                    break;
                }
            }

            // 返回活动轨道
            foreach (var track in Tracks)
            {
                if (track.IsMuted) continue;

                if (hasSolo)
                {
                    if (track.IsSolo)
                        result.Add(track);
                }
                else
                {
                    result.Add(track);
                }
            }

            return result;
        }
    }
}
