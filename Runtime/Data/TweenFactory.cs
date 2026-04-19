using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace CNoom.DOTweenVisual.Data
{
    /// <summary>
    /// Tween 创建工厂
    /// 统一运行时和编辑器预览的 Tween 创建逻辑，消除代码重复
    /// </summary>
    public static class TweenFactory
    {
        #region 公共方法

        /// <summary>
        /// 根据步骤数据创建对应的 Tween
        /// </summary>
        /// <param name="step">动画步骤数据</param>
        /// <param name="defaultTarget">默认目标 Transform（组件所在物体）</param>
        /// <returns>创建的 Tween，Delay/Callback 类型返回 null</returns>
        public static Tween CreateTween(TweenStepData step, Transform defaultTarget)
        {
            var target = step.TargetTransform != null ? step.TargetTransform : defaultTarget;
            if (target == null) return null;

            return step.Type switch
            {
                TweenStepType.Move => CreateMoveTween(step, target),
                TweenStepType.Rotate => CreateRotateTween(step, target),
                TweenStepType.Scale => CreateScaleTween(step, target),
                TweenStepType.Color => CreateColorTween(step, target),
                TweenStepType.Fade => CreateFadeTween(step, target),
                TweenStepType.AnchorMove => CreateAnchorMoveTween(step, target),
                TweenStepType.SizeDelta => CreateSizeDeltaTween(step, target),
                TweenStepType.Jump => CreateJumpTween(step, target),
                TweenStepType.Punch => CreatePunchTween(step, target),
                TweenStepType.Shake => CreateShakeTween(step, target),
                TweenStepType.FillAmount => CreateFillAmountTween(step, target),
                TweenStepType.DOPath => CreateDOPathTween(step, target),
                _ => null
            };
        }

        /// <summary>
        /// 将 Tween 配置并添加到 Sequence
        /// 处理缓动、延迟、可回收、执行模式
        /// </summary>
        public static void AppendToSequence(Sequence sequence, TweenStepData step, Transform defaultTarget)
        {
            // Delay 和 Callback 特殊处理
            if (step.Type == TweenStepType.Delay)
            {
                sequence.AppendInterval(Mathf.Max(0.001f, step.Duration));
                return;
            }

            if (step.Type == TweenStepType.Callback)
            {
                var callback = step.OnComplete;
                sequence.AppendCallback(() => callback?.Invoke());
                return;
            }

            var tween = CreateTween(step, defaultTarget);
            if (tween == null) return;

            // 设置缓动（Punch/Shake 有内置振荡缓动，不覆盖）
            if (step.Type != TweenStepType.Punch && step.Type != TweenStepType.Shake)
            {
                if (step.UseCustomCurve && step.CustomCurve != null)
                {
                    tween.SetEase(step.CustomCurve);
                }
                else
                {
                    tween.SetEase(step.Ease);
                }
            }

            // 设置延迟
            if (step.Delay > 0)
            {
                tween.SetDelay(step.Delay);
            }

            // 设置可回收
            tween.SetRecyclable(true);

            // 添加到序列
            switch (step.ExecutionMode)
            {
                case ExecutionMode.Append:
                    sequence.Append(tween);
                    break;
                case ExecutionMode.Join:
                    sequence.Join(tween);
                    break;
                case ExecutionMode.Insert:
                    sequence.Insert(Mathf.Max(0f, step.InsertTime), tween);
                    break;
            }
        }

        /// <summary>
        /// 应用步骤的起始值到目标物体（用于预览前恢复/设置状态）
        /// </summary>
        public static void ApplyStartValue(TweenStepData step, Transform defaultTarget)
        {
            var target = step.TargetTransform != null ? step.TargetTransform : defaultTarget;
            if (target == null) return;

            switch (step.Type)
            {
                case TweenStepType.Move:
                    if (step.UseStartValue)
                    {
                        ApplyMoveValue(target, step.MoveSpace, step.StartVector);
                    }
                    break;

                case TweenStepType.Rotate:
                    if (step.UseStartValue)
                    {
                        var quat = Quaternion.Euler(step.StartVector);
                        ApplyRotationValue(target, step.RotateSpace, quat);
                    }
                    break;

                case TweenStepType.Scale:
                    if (step.UseStartValue)
                    {
                        target.localScale = step.StartVector;
                    }
                    break;

                case TweenStepType.Color:
                    if (step.UseStartColor)
                    {
                        TweenValueHelper.TrySetColor(target, step.StartColor);
                    }
                    break;

                case TweenStepType.Fade:
                    if (step.UseStartFloat)
                    {
                        TweenValueHelper.TrySetAlpha(target, step.StartFloat);
                    }
                    break;

                case TweenStepType.FillAmount:
                    if (step.UseStartFloat)
                    {
                        var image = target.GetComponent<Image>();
                        if (image != null) image.fillAmount = step.StartFloat;
                    }
                    break;

                case TweenStepType.AnchorMove:
                    if (step.UseStartValue && TweenValueHelper.TryGetRectTransform(target, out var rt1))
                    {
                        rt1.anchoredPosition = step.StartVector;
                    }
                    break;

                case TweenStepType.SizeDelta:
                    if (step.UseStartValue && TweenValueHelper.TryGetRectTransform(target, out var rt2))
                    {
                        rt2.sizeDelta = step.StartVector;
                    }
                    break;

                case TweenStepType.DOPath:
                    if (step.UseStartValue)
                    {
                        target.position = step.StartVector;
                    }
                    break;

                case TweenStepType.Jump:
                    if (step.UseStartValue)
                    {
                        target.position = step.StartVector;
                    }
                    break;
            }
        }

        #endregion

        #region Transform Tweens

        private static Tweener CreateMoveTween(TweenStepData step, Transform target)
        {
            if (step.UseStartValue)
            {
                ApplyMoveValue(target, step.MoveSpace, step.StartVector);
            }

            float duration = Mathf.Max(0.001f, step.Duration);

            if (step.MoveSpace == MoveSpace.Local)
            {
                var tween = target.DOLocalMove(step.TargetVector, duration);
                if (step.IsRelative) tween.SetRelative(true);
                return tween;
            }

            var moveTween = target.DOMove(step.TargetVector, duration);
            if (step.IsRelative) moveTween.SetRelative(true);
            return moveTween;
        }

        private static Tweener CreateRotateTween(TweenStepData step, Transform target)
        {
            // 旋转始终使用四元数插值，避免万向锁
            Quaternion startQuat;
            Quaternion targetQuat = Quaternion.Euler(step.TargetVector);

            if (step.UseStartValue)
            {
                startQuat = Quaternion.Euler(step.StartVector);
                ApplyRotationValue(target, step.RotateSpace, startQuat);
            }
            else
            {
                startQuat = step.RotateSpace == RotateSpace.Local
                    ? target.localRotation
                    : target.rotation;
            }

            float duration = Mathf.Max(0.001f, step.Duration);

            if (step.RotateSpace == RotateSpace.Local)
            {
                return step.IsRelative
                    ? target.DOLocalRotateQuaternion(startQuat * targetQuat, duration)
                    : target.DOLocalRotateQuaternion(targetQuat, duration);
            }

            return step.IsRelative
                ? target.DORotateQuaternion(startQuat * targetQuat, duration)
                : target.DORotateQuaternion(targetQuat, duration);
        }

        private static Tweener CreateScaleTween(TweenStepData step, Transform target)
        {
            if (step.UseStartValue)
            {
                target.localScale = step.StartVector;
            }

            float duration = Mathf.Max(0.001f, step.Duration);

            var tween = target.DOScale(step.TargetVector, duration);
            if (step.IsRelative) tween.SetRelative(true);
            return tween;
        }

        #endregion

        #region Color/Fade Tweens

        private static Tweener CreateColorTween(TweenStepData step, Transform target)
        {
            if (!TweenStepRequirement.Validate(target, TweenStepType.Color, out _)) return null;

            if (step.UseStartColor)
            {
                TweenValueHelper.TrySetColor(target, step.StartColor);
            }

            float duration = Mathf.Max(0.001f, step.Duration);
            return TweenValueHelper.CreateColorTween(target, step.TargetColor, duration);
        }

        private static Tweener CreateFadeTween(TweenStepData step, Transform target)
        {
            if (!TweenStepRequirement.Validate(target, TweenStepType.Fade, out _)) return null;

            if (step.UseStartFloat)
            {
                TweenValueHelper.TrySetAlpha(target, step.StartFloat);
            }

            float duration = Mathf.Max(0.001f, step.Duration);
            return TweenValueHelper.CreateFadeTween(target, step.TargetFloat, duration);
        }

        #endregion

        #region UI Tweens

        private static Tweener CreateAnchorMoveTween(TweenStepData step, Transform target)
        {
            if (!TweenValueHelper.TryGetRectTransform(target, out var rectTransform)) return null;

            if (step.UseStartValue)
            {
                rectTransform.anchoredPosition = step.StartVector;
            }

            float duration = Mathf.Max(0.001f, step.Duration);

            var tween = rectTransform.DOAnchorPos(step.TargetVector, duration);
            if (step.IsRelative) tween.SetRelative(true);
            return tween;
        }

        private static Tweener CreateSizeDeltaTween(TweenStepData step, Transform target)
        {
            if (!TweenValueHelper.TryGetRectTransform(target, out var rectTransform)) return null;

            if (step.UseStartValue)
            {
                rectTransform.sizeDelta = step.StartVector;
            }

            float duration = Mathf.Max(0.001f, step.Duration);

            var tween = rectTransform.DOSizeDelta(step.TargetVector, duration);
            if (step.IsRelative) tween.SetRelative(true);
            return tween;
        }

        #endregion

        #region 特效 Tweens

        private static Sequence CreateJumpTween(TweenStepData step, Transform target)
        {
            if (step.UseStartValue)
            {
                target.position = step.StartVector;
            }

            float duration = Mathf.Max(0.001f, step.Duration);
            return target.DOJump(step.TargetVector, step.JumpHeight, step.JumpNum, duration);
        }

        private static Tweener CreatePunchTween(TweenStepData step, Transform target)
        {
            float duration = Mathf.Max(0.001f, step.Duration);
            int vibrato = Mathf.Max(1, step.Vibrato);
            float elasticity = Mathf.Clamp01(step.Elasticity);

            return step.PunchTarget switch
            {
                PunchTarget.Rotation => target.DOPunchRotation(step.Intensity, duration, vibrato, elasticity),
                PunchTarget.Scale => target.DOPunchScale(step.Intensity, duration, vibrato, elasticity),
                _ => target.DOPunchPosition(step.Intensity, duration, vibrato, elasticity)
            };
        }

        private static Tweener CreateShakeTween(TweenStepData step, Transform target)
        {
            float duration = Mathf.Max(0.001f, step.Duration);
            int vibrato = Mathf.Max(1, step.Vibrato);
            float randomness = Mathf.Clamp(step.ShakeRandomness, 0f, 90f);

            return step.ShakeTarget switch
            {
                ShakeTarget.Rotation => target.DOShakeRotation(duration, step.Intensity, vibrato, randomness),
                ShakeTarget.Scale => target.DOShakeScale(duration, step.Intensity, vibrato, randomness),
                _ => target.DOShakePosition(duration, step.Intensity, vibrato, randomness)
            };
        }

        private static Tweener CreateFillAmountTween(TweenStepData step, Transform target)
        {
            var image = target.GetComponent<Image>();
            if (image == null) return null;
            

            if (step.UseStartFloat)
            {
                image.fillAmount = step.StartFloat;
            }

            float duration = Mathf.Max(0.001f, step.Duration);
            return image.DOFillAmount(step.TargetFloat, duration);
        }

        private static Tweener CreateDOPathTween(TweenStepData step, Transform target)
        {
            if (step.PathWaypoints == null || step.PathWaypoints.Length < 2) return null;

            if (step.UseStartValue)
            {
                target.position = step.StartVector;
            }

            float duration = Mathf.Max(0.001f, step.Duration);
            var pathType = (PathType)step.PathType;
            var pathMode = (PathMode)step.PathMode;

            return target.DOPath(step.PathWaypoints, duration, pathType, pathMode, step.PathResolution);
        }

        #endregion

        #region 工具方法

        private static void ApplyMoveValue(Transform target, MoveSpace moveSpace, Vector3 value)
        {
            switch (moveSpace)
            {
                case MoveSpace.World:
                    target.position = value;
                    break;
                case MoveSpace.Local:
                    target.localPosition = value;
                    break;
            }
        }

        private static void ApplyRotationValue(Transform target, RotateSpace rotateSpace, Quaternion value)
        {
            switch (rotateSpace)
            {
                case RotateSpace.World:
                    target.rotation = value;
                    break;
                case RotateSpace.Local:
                    target.localRotation = value;
                    break;
            }
        }

        #endregion
    }
}
