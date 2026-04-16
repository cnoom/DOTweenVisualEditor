using System;
using UnityEngine;
using CNoom.DOTweenVisual.Data;
using CNoom.DOTweenVisual.Adapter;

namespace CNoom.DOTweenVisual.Player
{
    /// <summary>
    /// 片段播放器
    /// 负责播放单个片段
    /// </summary>
    public class ClipPlayer : IDisposable
    {
        private readonly ClipData _clipData;
        private readonly Transform _target;
        private readonly IDOTweenAdapter _adapter;
        private TweenerAdapter _tweener;

        public event Action<ClipData> OnClipStart;
        public event Action<ClipData> OnClipComplete;

        public ClipData ClipData => _clipData;
        public bool IsPlaying => _tweener?.IsPlaying() ?? false;

        public ClipPlayer(ClipData clipData, Transform target, IDOTweenAdapter adapter = null)
        {
            _clipData = clipData;
            _target = target;
            _adapter = adapter ?? DOTweenAdapter.Instance;
        }

        /// <summary>
        /// 播放片段
        /// </summary>
        public void Play()
        {
            if (_target == null || _clipData == null) return;

            Kill();
            CreateTweener();

            _tweener?.Play();
            OnClipStart?.Invoke(_clipData);
        }

        /// <summary>
        /// 暂停片段
        /// </summary>
        public void Pause()
        {
            _tweener?.Pause();
        }

        /// <summary>
        /// 停止片段
        /// </summary>
        public void Stop()
        {
            Kill();
        }

        private void CreateTweener()
        {
            var stepData = _clipData.StepData;
            if (stepData == null) return;

            switch (stepData.StepType)
            {
                case AnimationStepType.Move:
                    _tweener = CreateMoveTweener(stepData);
                    break;

                case AnimationStepType.Rotation:
                    _tweener = CreateRotationTweener(stepData);
                    break;

                case AnimationStepType.Scale:
                    _tweener = CreateScaleTweener(stepData);
                    break;
            }

            if (_tweener != null)
            {
                ApplyCommonSettings(_tweener, stepData);
                _tweener.OnComplete(() => OnClipComplete?.Invoke(_clipData));
            }
        }

        private TweenerAdapter CreateMoveTweener(AnimationStepData stepData)
        {
            return stepData.MoveMode switch
            {
                MoveMode.Absolute => _adapter.CreateMoveTween(_target, stepData.TargetPosition, stepData.Duration),
                MoveMode.Relative => _adapter.CreateMoveTweenRelative(_target, stepData.TargetPosition, stepData.Duration),
                MoveMode.Follow => CreateFollowTweener(stepData),
                _ => null
            };
        }

        private TweenerAdapter CreateFollowTweener(AnimationStepData stepData)
        {
            // TODO: 实现跟随模式
            if (stepData.TargetTransform != null)
            {
                return _adapter.CreateMoveTween(_target, stepData.TargetTransform.position, stepData.Duration);
            }
            return null;
        }

        private TweenerAdapter CreateRotationTweener(AnimationStepData stepData)
        {
            return stepData.RotationMode switch
            {
                RotationMode.Absolute => _adapter.CreateRotateTween(_target, stepData.TargetEulerAngles, stepData.Duration),
                RotationMode.Relative => _adapter.CreateRotateTween(_target, stepData.TargetEulerAngles, stepData.Duration),
                RotationMode.TargetRotation => null, // TODO: 实现四元数旋转
                _ => null
            };
        }

        private TweenerAdapter CreateScaleTweener(AnimationStepData stepData)
        {
            return _adapter.CreateScaleTween(_target, stepData.TargetScale, stepData.Duration);
        }

        private void ApplyCommonSettings(TweenerAdapter tweener, AnimationStepData stepData)
        {
            // 缓动
            if (stepData.Easing.UseCustomCurve)
            {
                tweener.SetEase(stepData.Easing.Curve);
            }
            else
            {
                tweener.SetEase(stepData.Easing.GetActualEase());
            }

            // 循环
            if (stepData.Loops != 1)
            {
                tweener.SetLoops(stepData.Loops, stepData.LoopType);
            }

            // 可回收
            tweener.SetRecyclable(true);
        }

        private void Kill()
        {
            _tweener?.Kill();
            _tweener = null;
        }

        public void Dispose()
        {
            Kill();
        }
    }
}
