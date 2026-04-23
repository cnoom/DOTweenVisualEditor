using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using CNoom.DOTweenVisual.Data;

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
        [Tooltip("播放触发时机")]
        [SerializeField]
        private PlayTrigger _playTrigger = PlayTrigger.Manual;

        [Tooltip("禁用时的行为")]
        [SerializeField]
        private DisableAction _disableAction = DisableAction.Pause;

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

        #region 事件回调

        private event TweenCallback _onStart;
        private event TweenCallback _onComplete;
        private event TweenCallback _onUpdate;

        /// <summary>
        /// 注册动画开始回调
        /// </summary>
        public DOTweenVisualPlayer OnStart(TweenCallback callback)
        {
            _onStart += callback;
            return this;
        }

        /// <summary>
        /// 注册动画完成回调
        /// </summary>
        public DOTweenVisualPlayer OnComplete(TweenCallback callback)
        {
            _onComplete += callback;
            return this;
        }

        /// <summary>
        /// 注册动画每帧更新回调
        /// </summary>
        public DOTweenVisualPlayer OnUpdate(TweenCallback callback)
        {
            _onUpdate += callback;
            return this;
        }

        /// <summary>
        /// 注册动画完成/终止回调
        /// 参数为 true 表示正常完成，false 表示被终止
        /// </summary>
        public DOTweenVisualPlayer OnDone(Action<bool> callback)
        {
            _onDone += callback;
            return this;
        }

        #endregion

        #region 私有字段

        private Sequence _currentSequence;
        private bool _isPlaying;
        private event Action<bool> _onDone;

        #endregion

        #region Unity 生命周期

        private void Awake()
        {
            if (_playTrigger == PlayTrigger.OnAwake)
            {
                Play();
            }
        }

        private void Start()
        {
            if (_playTrigger == PlayTrigger.OnStart)
            {
                Play();
            }
        }

        private void OnEnable()
        {
            switch (_playTrigger)
            {
                case PlayTrigger.OnEnableRestart:
                    Stop();
                    Play();
                    break;
                case PlayTrigger.OnEnableResume:
                    Resume();
                    break;
            }
        }

        private void OnDisable()
        {
            switch (_disableAction)
            {
                case DisableAction.Pause:
                    if (_isPlaying) Pause();
                    break;
                case DisableAction.Stop:
                    if (_isPlaying) Stop();
                    break;
                // DisableAction.None: 不做处理
            }
        }

        private void OnDestroy()
        {
            _isPlaying = false;
            KillSequence();
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
                if (_debugMode) DOTweenLog.Debug("已在播放中，忽略 Play 调用");
                return;
            }

            BuildAndPlay();
        }

        /// <summary>
        /// 播放动画序列并返回只读等待包装器
        /// 外部只能等待动画完成，无法对内部 Tween 进行修改
        /// 支持协程：yield return player.PlayAsync();
        /// 支持 UniTask：await player.PlayAsync().ToUniTask();
        /// </summary>
        public TweenAwaitable PlayAsync()
        {
            if (_isPlaying)
            {
                if (_debugMode) DOTweenLog.Debug("已在播放中，返回当前播放的等待包装器");
                return new TweenAwaitable(_currentSequence, this);
            }

            BuildAndPlay();

            if (_currentSequence == null)
            {
                if (_debugMode) DOTweenLog.Warning("没有可播放的动画步骤");
            }

            return new TweenAwaitable(_currentSequence, this);
        }

        /// <summary>
        /// 停止动画序列
        /// </summary>
        public void Stop()
        {
            KillSequence();
            if (_debugMode) DOTweenLog.Debug("动画已停止");
        }

        /// <summary>
        /// 暂停动画序列
        /// </summary>
        public void Pause()
        {
            if (_currentSequence != null && _currentSequence.IsPlaying())
            {
                _currentSequence.Pause();
                if (_debugMode) DOTweenLog.Debug("动画已暂停");
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
                if (_debugMode) DOTweenLog.Debug("动画已恢复");
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
                if (_debugMode) DOTweenLog.Warning("没有启用的动画步骤可播放");
                return;
            }

            KillSequence();

            _currentSequence = DOTween.Sequence();
            _currentSequence.SetTarget(this);

            foreach (var step in _steps)
            {
                if (!step.IsEnabled) continue;
                TweenFactory.AppendToSequence(_currentSequence, step, transform);
            }

            _currentSequence.SetLoops(_loops, _loopType);

            // 绑定事件回调
            if (_onStart != null)
                _currentSequence.OnStart(() => _onStart?.Invoke());

            // 合并 OnComplete：用户回调 + 内部状态管理 + 完成通知
            _currentSequence.OnComplete(() =>
            {
                _onComplete?.Invoke();
                _isPlaying = false;
                _onDone?.Invoke(true);
                if (_debugMode) DOTweenLog.Debug("动画播放完成");
            });

            if (_onUpdate != null)
                _currentSequence.OnUpdate(() => _onUpdate?.Invoke());

            _currentSequence.OnPlay(() =>
            {
                _isPlaying = true;
                if (_debugMode) DOTweenLog.Debug($"开始播放 {_steps.Count} 个步骤");
            });

            // 处理被手动 Kill 的情况（Stop/Restart 等场景，OnComplete 不会触发）
            _currentSequence.OnKill(() =>
            {
                if (_isPlaying)
                {
                    _isPlaying = false;
                    _onDone?.Invoke(false);
                }
            });

            _currentSequence.Play();
        }

        private void KillSequence()
        {
            if (_currentSequence != null)
            {
                bool wasPlaying = _isPlaying;
                // 先标记为未播放，防止 OnKill 回调重复调用 _onDone
                _isPlaying = false;

                // 回滚所有子 Tween 到动画开始前的状态（恢复位置/颜色/缩放等属性）
                if (_currentSequence.IsActive())
                {
                    _currentSequence.Rewind();
                }
                _currentSequence.Kill();
                _currentSequence = null;

                // 直接触发 OnDone 回调（不依赖 DOTween 的 OnKill，确保可靠性）
                if (wasPlaying)
                {
                    _onDone?.Invoke(false);
                }
                _onDone = null;
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
