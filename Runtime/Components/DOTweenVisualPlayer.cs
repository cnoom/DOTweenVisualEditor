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
            // 确保 DOTween 已初始化
            DOTweenAdapter.Instance.Initialize();

            if (_playOnStart)
            {
                Play();
            }
        }

        private void OnDestroy()
        {
            KillSequence();
        }

        private void OnDisable()
        {
            Pause();
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
            if (_steps.Count == 0)
            {
                if (_debugMode) Debug.LogWarning($"[{nameof(DOTweenVisualPlayer)}] 没有动画步骤可播放");
                return;
            }

            KillSequence();

            // 创建序列
            _currentSequence = DOTween.Sequence();
            _currentSequence.SetTarget(this);

            // 构建序列
            foreach (var step in _steps)
            {
                if (!step.IsEnabled) continue;
                AppendStepToSequence(step);
            }

            // 设置循环
            _currentSequence.SetLoops(_loops, _loopType);

            // 播放完成回调
            _currentSequence.OnComplete(() =>
            {
                _isPlaying = false;
                if (_debugMode) Debug.Log($"[{nameof(DOTweenVisualPlayer)}] 动画播放完成");
            });

            // 开始播放
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
                case TweenType.Move:
                    tweener = CreateMoveTween(step);
                    break;
                case TweenType.Rotate:
                    tweener = CreateRotateTween(step);
                    break;
                case TweenType.Scale:
                    tweener = CreateScaleTween(step);
                    break;
                case TweenType.Delay:
                    // Delay 使用 AppendInterval
                    _currentSequence.AppendInterval(step.Duration);
                    return;
                case TweenType.Callback:
                    // Callback 使用 AppendCallback
                    _currentSequence.AppendCallback(() => step.OnComplete?.Invoke());
                    return;
                case TweenType.Property:
                    // Property 动画留待后续实现
                    if (_debugMode)
                    {
                        Debug.LogWarning($"[{nameof(DOTweenVisualPlayer)}] Property 动画暂未实现");
                    }
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
                    _currentSequence.Insert(step.InsertTime, tweener);
                    break;
            }

            // 完成回调
            if (step.OnComplete != null && step.OnComplete.GetPersistentEventCount() > 0)
            {
                tweener.OnComplete(() => step.OnComplete?.Invoke());
            }
        }

        private Tweener CreateMoveTween(TweenStepData step)
        {
            if (step.IsRelative)
            {
                return transform.DOMove(step.TargetValue, step.Duration).From(isRelative: true);
            }
            return transform.DOMove(step.TargetValue, step.Duration);
        }

        private Tweener CreateRotateTween(TweenStepData step)
        {
            if (step.IsRelative)
            {
                return transform.DORotate(step.TargetValue, step.Duration).From(isRelative: true);
            }
            return transform.DORotate(step.TargetValue, step.Duration);
        }

        private Tweener CreateScaleTween(TweenStepData step)
        {
            if (step.IsRelative)
            {
                return transform.DOScale(step.TargetValue, step.Duration).From(isRelative: true);
            }
            return transform.DOScale(step.TargetValue, step.Duration);
        }

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
