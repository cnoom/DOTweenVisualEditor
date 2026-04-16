using UnityEngine;
using CNoom.DOTweenVisual.Data;
using CNoom.DOTweenVisual.Adapter;

namespace CNoom.DOTweenVisual.Steps
{
    /// <summary>
    /// 动画步骤基类
    /// </summary>
    public abstract class AnimationStepBase : IAnimationStep
    {
        protected readonly AnimationStepData _data;

        protected AnimationStepBase(AnimationStepData data)
        {
            _data = data;
        }

        public abstract TweenerAdapter CreateTween(Transform target, IDOTweenAdapter adapter);

        public virtual bool Validate()
        {
            return _data != null && _data.Duration > 0;
        }

        public virtual float GetDuration()
        {
            return _data?.Duration ?? 0f;
        }

        protected void ApplyEasing(TweenerAdapter tweener)
        {
            if (tweener == null || _data?.Easing == null) return;

            if (_data.Easing.UseCustomCurve)
            {
                tweener.SetEase(_data.Easing.Curve);
            }
            else
            {
                tweener.SetEase(_data.Easing.GetActualEase());
            }
        }

        protected void ApplyLoop(TweenerAdapter tweener)
        {
            if (tweener == null || _data == null) return;

            if (_data.Loops != 1)
            {
                tweener.SetLoops(_data.Loops, _data.LoopType);
            }
        }
    }
}
