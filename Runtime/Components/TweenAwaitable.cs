using System;
using DG.Tweening;
using UnityEngine;

namespace CNoom.DOTweenVisual.Components
{
    /// <summary>
    /// Tween 只读等待包装器
    /// 仅暴露异步等待相关能力，防止外部对内部 Tween 进行修改
    /// Tween 的生命周期由 DOTweenVisualPlayer 管理，本类仅作为观察者
    /// </summary>
    public class TweenAwaitable : CustomYieldInstruction
    {
        #region 私有字段

        private readonly Tween _tween;

        #endregion

        #region 属性

        /// <summary>动画是否已完成（完成或被杀死）</summary>
        public bool IsDone => _tween == null || !_tween.IsActive();

        /// <summary>动画是否正常完成（非被杀死）</summary>
        public bool IsCompleted => _tween != null && _tween.IsComplete();

        /// <summary>动画是否正在播放</summary>
        public bool IsPlaying => _tween != null && _tween.IsPlaying();

        /// <summary>动画是否已激活</summary>
        public bool IsActive => _tween != null && _tween.IsActive();

        /// <summary>
        /// CustomYieldInstruction 实现
        /// keepWaiting 返回 true 表示继续等待，false 表示完成
        /// </summary>
        public override bool keepWaiting => !IsDone;

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建 TweenAwaitable 包装器
        /// </summary>
        /// <param name="tween">要包装的 Tween 对象</param>
        public TweenAwaitable(Tween tween)
        {
            _tween = tween;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 作为协程等待使用
        /// 用法：yield return awaitable.WaitForCompletion();
        /// </summary>
        public CustomYieldInstruction WaitForCompletion()
        {
            return this;
        }

        /// <summary>
        /// 注册完成回调（不影响内部 Tween 的 OnComplete）
        /// </summary>
        public TweenAwaitable OnDone(Action<bool> onDone)
        {
            if (_tween == null || !_tween.IsActive())
            {
                onDone?.Invoke(false);
                return this;
            }

            _tween.OnComplete(() => onDone?.Invoke(true));
            _tween.OnKill(() =>
            {
                if (!_tween.IsComplete())
                {
                    onDone?.Invoke(false);
                }
            });
            return this;
        }

        #endregion
    }
}
