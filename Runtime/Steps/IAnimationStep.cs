using UnityEngine;
using CNoom.DOTweenVisual.Adapter;

namespace CNoom.DOTweenVisual.Steps
{
    /// <summary>
    /// 动画步骤接口
    /// </summary>
    public interface IAnimationStep
    {
        /// <summary>
        /// 创建 Tween
        /// </summary>
        TweenerAdapter CreateTween(Transform target, IDOTweenAdapter adapter);

        /// <summary>
        /// 验证步骤数据
        /// </summary>
        bool Validate();

        /// <summary>
        /// 获取持续时间
        /// </summary>
        float GetDuration();
    }
}
