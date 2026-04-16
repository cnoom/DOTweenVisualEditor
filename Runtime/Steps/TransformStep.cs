using UnityEngine;
using CNoom.DOTweenVisual.Data;
using CNoom.DOTweenVisual.Adapter;

namespace CNoom.DOTweenVisual.Steps
{
    /// <summary>
    /// 移动动画步骤
    /// </summary>
    public class MoveStep : AnimationStepBase
    {
        public MoveStep(AnimationStepData data) : base(data) { }

        public override TweenerAdapter CreateTween(Transform target, IDOTweenAdapter adapter)
        {
            if (!Validate() || target == null) return null;

            TweenerAdapter tweener = null;

            switch (_data.MoveMode)
            {
                case MoveMode.Absolute:
                    tweener = adapter.CreateMoveTween(target, _data.TargetPosition, _data.Duration);
                    break;

                case MoveMode.Relative:
                    tweener = adapter.CreateMoveTweenRelative(target, _data.TargetPosition, _data.Duration);
                    break;

                case MoveMode.Follow:
                    if (_data.TargetTransform != null)
                    {
                        tweener = adapter.CreateMoveTween(target, _data.TargetTransform.position, _data.Duration);
                    }
                    break;
            }

            if (tweener != null)
            {
                ApplyEasing(tweener);
                ApplyLoop(tweener);
                tweener.SetRecyclable(true);
            }

            return tweener;
        }

        public override bool Validate()
        {
            return base.Validate() && (_data.MoveMode != MoveMode.Follow || _data.TargetTransform != null);
        }
    }

    /// <summary>
    /// 旋转动画步骤
    /// </summary>
    public class RotationStep : AnimationStepBase
    {
        public RotationStep(AnimationStepData data) : base(data) { }

        public override TweenerAdapter CreateTween(Transform target, IDOTweenAdapter adapter)
        {
            if (!Validate() || target == null) return null;

            TweenerAdapter tweener = null;

            switch (_data.RotationMode)
            {
                case RotationMode.Absolute:
                case RotationMode.Relative:
                    tweener = adapter.CreateRotateTween(target, _data.TargetEulerAngles, _data.Duration);
                    break;

                case RotationMode.TargetRotation:
                    // TODO: 实现四元数旋转
                    break;
            }

            if (tweener != null)
            {
                ApplyEasing(tweener);
                ApplyLoop(tweener);
                tweener.SetRecyclable(true);
            }

            return tweener;
        }
    }

    /// <summary>
    /// 缩放动画步骤
    /// </summary>
    public class ScaleStep : AnimationStepBase
    {
        public ScaleStep(AnimationStepData data) : base(data) { }

        public override TweenerAdapter CreateTween(Transform target, IDOTweenAdapter adapter)
        {
            if (!Validate() || target == null) return null;

            var tweener = adapter.CreateScaleTween(target, _data.TargetScale, _data.Duration);

            if (tweener != null)
            {
                ApplyEasing(tweener);
                ApplyLoop(tweener);
                tweener.SetRecyclable(true);
            }

            return tweener;
        }
    }

    /// <summary>
    /// 动画步骤工厂
    /// </summary>
    public static class AnimationStepFactory
    {
        public static IAnimationStep Create(AnimationStepData data)
        {
            if (data == null) return null;

            return data.StepType switch
            {
                AnimationStepType.Move => new MoveStep(data),
                AnimationStepType.Rotation => new RotationStep(data),
                AnimationStepType.Scale => new ScaleStep(data),
                _ => null
            };
        }
    }
}
