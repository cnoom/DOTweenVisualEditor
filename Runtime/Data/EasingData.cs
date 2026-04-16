using System;
using UnityEngine;

namespace CNoom.DOTweenVisual.Data
{
    /// <summary>
    /// 缓动曲线数据，支持内置 Ease 和自定义 AnimationCurve
    /// </summary>
    [Serializable]
    public class EasingData
    {
        public EasingType Type = EasingType.Ease;
        public Ease Ease = Ease.OutQuad;
        public AnimationCurve Curve;

        /// <summary>
        /// 获取实际的 Ease 值（如果是自定义曲线则返回 INTERNAL_Custom）
        /// </summary>
        public Ease GetActualEase()
        {
            if (Type == EasingType.Curve && Curve != null)
            {
                return Ease.INTERNAL_Custom;
            }
            return Ease;
        }

        /// <summary>
        /// 是否使用自定义曲线
        /// </summary>
        public bool UseCustomCurve => Type == EasingType.Curve && Curve != null;
    }

    /// <summary>
    /// 缓动类型
    /// </summary>
    public enum EasingType
    {
        /// <summary>使用内置 Ease</summary>
        Ease,
        /// <summary>使用自定义 AnimationCurve</summary>
        Curve
    }
}
