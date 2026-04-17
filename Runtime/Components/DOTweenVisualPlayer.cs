using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using CNoom.DOTweenVisual.Data;
using CNoom.DOTweenVisual.Adapter;

namespace CNoom.DOTweenVisual.Components
{
    /// <summary>
    /// DOTween 可视化动画播放器
    /// 挂载在目标物体上，管理该物体的所有 Tween 动画
    /// </summary>
    [AddComponentMenu("DOTween Visual/DOTween Visual Player")]
    public class DOTweenVisualPlayer : MonoBehaviour
    {
        #region 序列化字段

        [Header("动画序列")]
        [Tooltip("动画步骤列表")]
        [SerializeField]
        private List<TweenStepData> _steps = new();

        [Header("播放设置")]
        [Tooltip("游戏开始时自动播放")]
        [SerializeField]
        private bool _playOnStart = false;

        [Tooltip("循环次数（-1 为无限循环）")]
        [SerializeField]
        private int _loops = 1;

        [Tooltip("循环类型")]
        [SerializeField]
        private LoopType _loopType = LoopType.Restart;

        [Header("调试")]
        [Tooltip("启用调试日志")]
        [SerializeField]
        private bool _debugMode = false;

        #endregion

        #region 属性

        /// <summary>动画步骤列表（只读）</summary>
        public IReadOnlyList<TweenStepData> Steps => _steps;

        /// <summary>是否正在播放</summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>步骤数量</summary>
        public int StepCount => _steps.Count;

        #endregion

        #region 私有字段

        private Sequence _currentSequence;
        private bool _isPlaying;

        #endregion

        #region Unity 生命周期

        private void Start()
        {
            DOTweenAdapter.Instance.Initialize();

            if (_playOnStart)
            {
                Play();
            }
        }

        private void OnDestroy()
        {
            _isPlaying = false;
            KillSequence();
        }

        private void OnDisable()
        {
            if (_isPlaying)
            {
                Pause();
            }
        }

        #endregion

        #region 公共 API

        /// <summary>
        /// 播放动画序列
        /// </summary>
        public void Play()
        {
            if (_isPlaying)
            {
                if (_debugMode) Debug.Log($"[{nameof(DOTweenVisualPlayer)}] 已在播放中，忽略 Play 调用");
                return;
            }

            BuildAndPlay();
        }

        /// <summary>
        /// 停止动画序列
        /// </summary>
        public void Stop()
        {
            KillSequence();
            if (_debugMode) Debug.Log($"[{nameof(DOTweenVisualPlayer)}] 动画已停止");
        }

        /// <summary>
        /// 暂停动画序列
        /// </summary>
        public void Pause()
        {
            if (_currentSequence != null && _currentSequence.IsPlaying())
            {
                _currentSequence.Pause();
                if (_debugMode) Debug.Log($"[{nameof(DOTweenVisualPlayer)}] 动画已暂停");
            }
        }

        /// <summary>
        /// 恢复播放
        /// </summary>
        public void Resume()
        {
            if (_currentSequence != null && !_currentSequence.IsPlaying())
            {
                _currentSequence.Play();
                if (_debugMode) Debug.Log($"[{nameof(DOTweenVisualPlayer)}] 动画已恢复");
            }
        }

        /// <summary>
        /// 重新播放
        /// </summary>
        public void Restart()
        {
            Stop();
            Play();
        }

        /// <summary>
        /// 完成（跳到动画末尾）
        /// </summary>
        public void Complete()
        {
            if (_currentSequence != null)
            {
                _currentSequence.Complete();
            }
        }

        #endregion

        #region 编辑器 API

        /// <summary>
        /// 添加动画步骤
        /// </summary>
        public void AddStep(TweenStepData step)
        {
            _steps.Add(step);
        }

        /// <summary>
        /// 移除动画步骤
        /// </summary>
        public void RemoveStep(int index)
        {
            if (index >= 0 && index < _steps.Count)
            {
                _steps.RemoveAt(index);
            }
        }

        /// <summary>
        /// 移动动画步骤位置
        /// </summary>
        public void MoveStep(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _steps.Count) return;
            if (toIndex < 0 || toIndex >= _steps.Count) return;

            var step = _steps[fromIndex];
            _steps.RemoveAt(fromIndex);
            _steps.Insert(toIndex, step);
        }

        /// <summary>
        /// 获取或创建步骤（用于编辑器）
        /// </summary>
        public TweenStepData GetOrCreateStep(int index)
        {
            if (index >= 0 && index < _steps.Count)
            {
                return _steps[index];
            }
            return null;
        }

        /// <summary>
        /// 清空所有步骤
        /// </summary>
        public void ClearAllSteps()
        {
            _steps.Clear();
        }

        #endregion

        #region 私有方法

        private void BuildAndPlay()
        {
            // 检查是否有启用的步骤
            bool hasEnabledSteps = false;
            foreach (var step in _steps)
            {
                if (step.IsEnabled)
                {
                    hasEnabledSteps = true;
                    break;
                }
            }

            if (!hasEnabledSteps)
            {
                if (_debugMode) Debug.LogWarning($"[{nameof(DOTweenVisualPlayer)}] 没有启用的动画步骤可播放");
                return;
            }

            KillSequence();

            _currentSequence = DOTween.Sequence();
            _currentSequence.SetTarget(this);

            foreach (var step in _steps)
            {
                if (!step.IsEnabled) continue;
                AppendStepToSequence(step);
            }

            _currentSequence.SetLoops(_loops, _loopType);

            _currentSequence.OnComplete(() =>
            {
                _isPlaying = false;
                if (_debugMode) Debug.Log($"[{nameof(DOTweenVisualPlayer)}] 动画播放完成");
            });

            _currentSequence.Play();
            _isPlaying = true;

            if (_debugMode)
            {
                Debug.Log($"[{nameof(DOTweenVisualPlayer)}] 开始播放 {_steps.Count} 个步骤");
            }
        }

        private void AppendStepToSequence(TweenStepData step)
        {
            Tweener tweener = null;

            switch (step.Type)
            {
                case TweenStepType.Move:
                    tweener = CreateMoveTween(step);
                    break;
                case TweenStepType.Rotate:
                    tweener = CreateRotateTween(step);
                    break;
                case TweenStepType.Scale:
                    tweener = CreateScaleTween(step);
                    break;
                case TweenStepType.Color:
                    tweener = CreateColorTween(step);
                    break;
                case TweenStepType.Fade:
                    tweener = CreateFadeTween(step);
                    break;
                case TweenStepType.Delay:
                    _currentSequence.AppendInterval(Mathf.Max(0.001f, step.Duration));
                    return;
                case TweenStepType.Callback:
                    var callback = step.OnComplete;
                    _currentSequence.AppendCallback(() => callback?.Invoke());
                    return;
            }

            if (tweener == null) return;

            // 设置缓动
            if (step.UseCustomCurve && step.CustomCurve != null)
            {
                tweener.SetEase(step.CustomCurve);
            }
            else
            {
                tweener.SetEase(step.Ease);
            }

            // 设置延迟
            if (step.Delay > 0)
            {
                tweener.SetDelay(step.Delay);
            }

            // 设置可回收
            tweener.SetRecyclable(true);

            // 添加到序列
            switch (step.ExecutionMode)
            {
                case ExecutionMode.Append:
                    _currentSequence.Append(tweener);
                    break;
                case ExecutionMode.Join:
                    _currentSequence.Join(tweener);
                    break;
                case ExecutionMode.Insert:
                    _currentSequence.Insert(Mathf.Max(0f, step.InsertTime), tweener);
                    break;
            }
        }

        #region Transform Tweens

        private Tweener CreateMoveTween(TweenStepData step)
        {
            var target = step.TargetTransform != null ? step.TargetTransform : transform;
            if (target == null) return null;

            // 应用起始值
            if (step.UseStartValue)
            {
                ApplyMoveValue(target, step.TransformTarget, step.StartVector);
            }

            float duration = Mathf.Max(0.001f, step.Duration);

            switch (step.TransformTarget)
            {
                case TransformTarget.LocalPosition:
                    if (step.IsRelative)
                    {
                        return target.DOLocalMove(step.TargetVector, duration).From(isRelative: true);
                    }
                    return target.DOLocalMove(step.TargetVector, duration);

                default: // Position
                    if (step.IsRelative)
                    {
                        return target.DOMove(step.TargetVector, duration).From(isRelative: true);
                    }
                    return target.DOMove(step.TargetVector, duration);
            }
        }

        private Tweener CreateRotateTween(TweenStepData step)
        {
            var target = step.TargetTransform != null ? step.TargetTransform : transform;
            if (target == null) return null;

            // 旋转始终使用四元数插值，避免万向锁
            Quaternion startQuat;
            Quaternion targetQuat;

            if (step.UseStartValue)
            {
                startQuat = Quaternion.Euler(step.StartVector);
                ApplyRotationValue(target, step.TransformTarget, startQuat);
            }
            else
            {
                // 获取当前旋转作为起始值
                startQuat = step.TransformTarget == TransformTarget.LocalRotation
                    ? target.localRotation
                    : target.rotation;
            }

            targetQuat = Quaternion.Euler(step.TargetVector);

            float duration = Mathf.Max(0.001f, step.Duration);

            if (step.TransformTarget == TransformTarget.LocalRotation)
            {
                if (step.IsRelative)
                {
                    return target.DOLocalRotateQuaternion(startQuat * targetQuat, duration);
                }
                return target.DOLocalRotateQuaternion(targetQuat, duration);
            }
            else
            {
                if (step.IsRelative)
                {
                    return target.DORotateQuaternion(startQuat * targetQuat, duration);
                }
                return target.DORotateQuaternion(targetQuat, duration);
            }
        }

        private Tweener CreateScaleTween(TweenStepData step)
        {
            var target = step.TargetTransform != null ? step.TargetTransform : transform;
            if (target == null) return null;

            if (step.UseStartValue)
            {
                target.localScale = step.StartVector;
            }

            float duration = Mathf.Max(0.001f, step.Duration);

            if (step.IsRelative)
            {
                return target.DOScale(step.TargetVector, duration).From(isRelative: true);
            }
            return target.DOScale(step.TargetVector, duration);
        }

        #endregion

        #region Color/Fade Tweens

        private Tweener CreateColorTween(TweenStepData step)
        {
            var target = step.TargetTransform != null ? step.TargetTransform : transform;
            if (target == null) return null;

            // 尝试获取 Renderer 或 SpriteRenderer
            var renderer = target.GetComponent<Renderer>();
            if (renderer == null || renderer.material == null)
            {
                if (_debugMode) Debug.LogWarning($"[{nameof(DOTweenVisualPlayer)}] 目标物体没有 Renderer 组件，无法播放颜色动画");
                return null;
            }

            // 应用起始颜色
            if (step.UseStartColor)
            {
                renderer.material.color = step.StartColor;
            }

            float duration = Mathf.Max(0.001f, step.Duration);
            return renderer.material.DOColor(step.TargetColor, duration);
        }

        private Tweener CreateFadeTween(TweenStepData step)
        {
            var target = step.TargetTransform != null ? step.TargetTransform : transform;
            if (target == null) return null;

            // 尝试获取 CanvasGroup（UI）或 Renderer（3D/Sprite）
            var canvasGroup = target.GetComponent<CanvasGroup>();
            var renderer = target.GetComponent<Renderer>();

            if (canvasGroup != null)
            {
                if (step.UseStartFloat)
                {
                    canvasGroup.alpha = step.StartFloat;
                }
                float duration = Mathf.Max(0.001f, step.Duration);
                return canvasGroup.DOFade(step.TargetFloat, duration);
            }

            if (renderer != null && renderer.material != null)
            {
                if (step.UseStartFloat)
                {
                    Color c = renderer.material.color;
                    c.a = step.StartFloat;
                    renderer.material.color = c;
                }
                float duration = Mathf.Max(0.001f, step.Duration);
                return renderer.material.DOFade(step.TargetFloat, duration);
            }

            if (_debugMode) Debug.LogWarning($"[{nameof(DOTweenVisualPlayer)}] 目标物体没有 CanvasGroup 或 Renderer，无法播放透明度动画");
            return null;
        }

        #endregion

        #region 起始值应用

        private void ApplyMoveValue(Transform target, TransformTarget transformTarget, Vector3 value)
        {
            switch (transformTarget)
            {
                case TransformTarget.Position:
                    target.position = value;
                    break;
                case TransformTarget.LocalPosition:
                    target.localPosition = value;
                    break;
            }
        }

        private void ApplyRotationValue(Transform target, TransformTarget transformTarget, Quaternion value)
        {
            switch (transformTarget)
            {
                case TransformTarget.Rotation:
                    target.rotation = value;
                    break;
                case TransformTarget.LocalRotation:
                    target.localRotation = value;
                    break;
            }
        }

        #endregion

        private void KillSequence()
        {
            if (_currentSequence != null)
            {
                _currentSequence.Kill();
                _currentSequence = null;
            }
            _isPlaying = false;
        }

        #endregion

        #region 编辑器调试

#if UNITY_EDITOR
        [ContextMenu("播放")]
        private void ContextMenuPlay() => Play();

        [ContextMenu("停止")]
        private void ContextMenuStop() => Stop();

        [ContextMenu("暂停")]
        private void ContextMenuPause() => Pause();

        [ContextMenu("重播")]
        private void ContextMenuRestart() => Restart();

        [ContextMenu("完成")]
        private void ContextMenuComplete() => Complete();
#endif

        #endregion
    }
}
