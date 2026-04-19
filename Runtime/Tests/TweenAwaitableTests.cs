using NUnit.Framework;
using UnityEngine;
using DG.Tweening;
using CNoom.DOTweenVisual.Components;
using CNoom.DOTweenVisual.Data;

namespace CNoom.DOTweenVisual.Tests
{
    /// <summary>
    /// TweenAwaitable 异步等待包装器测试
    /// 
    /// 测试重点：
    /// - Null Tween 的状态属性（纯逻辑，无 DOTween 依赖）
    /// - OnDone 回调路由（通过 DOTweenVisualPlayer 途径）
    /// - WaitForCompletion 返回自身
    /// 
    /// 不测试的内容：
    /// - 活跃 Tween 的 IsDone/IsPlaying/IsCompleted/IsActive 属性
    ///   这些是 DOTween API 的薄封装（一行代码），测试它们等于测试 DOTween 自身。
    ///   且 DOTween 在 EditMode 下内部状态属性不可靠（ManualUpdate 能触发回调但
    ///   IsActive()/IsComplete() 不一定正确更新）。
    /// </summary>
    public class TweenAwaitableTests
    {
        private GameObject _playerObj;
        private DOTweenVisualPlayer _player;

        /// <summary>
        /// 同步模拟时间推进，驱动 DOTween 更新
        /// </summary>
        private static void SimulateTimeSync(float totalTime, float step = 0.02f)
        {
            float elapsed = 0f;
            while (elapsed < totalTime)
            {
                DOTween.ManualUpdate(step, step);
                elapsed += step;
            }
        }

        [SetUp]
        public void SetUp()
        {
            _playerObj = new GameObject("TestPlayer");
            _player = _playerObj.AddComponent<DOTweenVisualPlayer>();
            DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
            DOTween.SetTweensCapacity(200, 200);
            DOTween.defaultAutoPlay = AutoPlay.None;
            DOTween.defaultUpdateType = UpdateType.Manual;
        }

        [TearDown]
        public void TearDown()
        {
            DOTween.KillAll();
            DOTween.defaultAutoPlay = AutoPlay.All;
            DOTween.defaultUpdateType = UpdateType.Normal;
            if (_playerObj != null)
            {
                Object.DestroyImmediate(_playerObj);
            }
        }

        #region Null Tween 状态属性

        [Test]
        public void IsDone_WithNullTween_ReturnsTrue()
        {
            var awaitable = new TweenAwaitable(null);
            Assert.IsTrue(awaitable.IsDone);
        }

        [Test]
        public void IsCompleted_WithNullTween_ReturnsFalse()
        {
            var awaitable = new TweenAwaitable(null);
            Assert.IsFalse(awaitable.IsCompleted);
        }

        [Test]
        public void IsPlaying_WithNullTween_ReturnsFalse()
        {
            var awaitable = new TweenAwaitable(null);
            Assert.IsFalse(awaitable.IsPlaying);
        }

        [Test]
        public void IsActive_WithNullTween_ReturnsFalse()
        {
            var awaitable = new TweenAwaitable(null);
            Assert.IsFalse(awaitable.IsActive);
        }

        [Test]
        public void KeepWaiting_WithNullTween_ReturnsFalse()
        {
            var awaitable = new TweenAwaitable(null);
            Assert.IsFalse(awaitable.keepWaiting);
        }

        #endregion

        #region WaitForCompletion

        [Test]
        public void WaitForCompletion_ReturnsSelf()
        {
            var awaitable = new TweenAwaitable(null);
            var result = awaitable.WaitForCompletion();
            Assert.AreSame(awaitable, result);
        }

        #endregion

        #region OnDone 回调路由

        [Test]
        public void OnDone_WithNullTween_InvokesCallbackWithFalse()
        {
            bool? result = null;
            var awaitable = new TweenAwaitable(null);
            awaitable.OnDone(completed => result = completed);

            Assert.IsTrue(result.HasValue);
            Assert.IsFalse(result.Value);
        }

        [Test]
        public void OnDone_WithNullTween_NoPlayer_InvokesCallbackWithFalse()
        {
            bool? result = null;
            // 有 Tween 但无 Player 的情况（直接构造无 player 参数）
            var awaitable = new TweenAwaitable(null, null);
            awaitable.OnDone(completed => result = completed);

            Assert.IsTrue(result.HasValue);
            Assert.IsFalse(result.Value);
        }

        [Test]
        public void OnDone_WithPlayer_OnCompleteInvokesCallbackWithTrue()
        {
            _player.AddStep(new TweenStepData
            {
                Type = TweenStepType.Move,
                TargetVector = new Vector3(5f, 0f, 0f),
                Duration = 0.2f
            });

            var awaitable = _player.PlayAsync();
            bool? doneResult = null;
            awaitable.OnDone(completed => doneResult = completed);

            // 推进时间使动画完成
            SimulateTimeSync(0.5f);

            Assert.IsTrue(doneResult.HasValue, "OnDone 回调应被调用");
            Assert.IsTrue(doneResult.Value, "正常完成应传入 true");
        }

        [Test]
        public void OnDone_WithPlayer_OnStopInvokesCallbackWithFalse()
        {
            _player.AddStep(new TweenStepData
            {
                Type = TweenStepType.Move,
                TargetVector = new Vector3(5f, 0f, 0f),
                Duration = 2f
            });

            var awaitable = _player.PlayAsync();
            bool? doneResult = null;
            awaitable.OnDone(completed => doneResult = completed);

            // 先让动画真正启动
            DOTween.ManualUpdate(0.01f, 0.01f);

            _player.Stop();

            Assert.IsTrue(doneResult.HasValue, "OnDone 回调应被调用");
            Assert.IsFalse(doneResult.Value, "被停止应传入 false");
        }

        [Test]
        public void OnDone_Chainable_ReturnsSameAwaitable()
        {
            var awaitable = new TweenAwaitable(null);
            var result = awaitable.OnDone(_ => { });
            Assert.AreSame(awaitable, result);
        }

        #endregion
    }
}
