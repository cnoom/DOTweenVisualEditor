using System;
using UnityEngine;

namespace CNoom.DOTweenVisual.Data
{
    /// <summary>
    /// 片段数据
    /// </summary>
    [Serializable]
    public class ClipData
    {
        [Tooltip("片段名称")]
        public string ClipName = "New Clip";

        [Tooltip("起始时间（秒）")]
        public float StartTime;

        [Tooltip("持续时间（秒）")]
        public float Duration = 1f;

        [Tooltip("缓动曲线")]
        public EasingData Easing = new();

        [Tooltip("动画步骤数据")]
        public AnimationStepData StepData = new();

        /// <summary>
        /// 片段结束时间
        /// </summary>
        public float EndTime => StartTime + Duration;
    }
}
