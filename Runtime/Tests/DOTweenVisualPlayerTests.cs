using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DG.Tweening;
using CNoom.DOTweenVisual.Components;
using CNoom.DOTweenVisual.Data;

namespace CNoom.DOTweenVisual.Tests
{
    /// <summary>
    /// DOTweenVisualPlayer 播放器组件测试
    /// 测试播放/暂停/恢复/停止生命周期和回调触发
    /// 使用 DOTween.ManualUpdate 同步驱动时间（EditMode 兼容）
    /// 所有涉及 DOTween 的测试使用 [Test] + 同步 ManualUpdate 循环
    /// </summary>
    public class DOTweenVisualPlayerTests
    {
        private GameObject _gameObject;
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
            _gameObject = new GameObject("TestPlayer");
            _player = _gameObject.AddComponent<DOTweenVisualPlayer>();
            // 使用手动更新模式，确保 EditMode 下可精确控制时间
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
            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
            }
        }

        #region 初始状态

        [Test]
        public void InitialState_IsNotPlaying()
        {
            Assert.IsFalse(_player.IsPlaying);
        }

        [Test]
        public void InitialState_StepCountIsZero()
        {
            Assert.AreEqual(0, _player.StepCount);
        }

        [Test]
        public void InitialState_StepsIsNotNull()
        {
            Assert.IsNotNull(_player.Steps);
        }

        #endregion

        #region 编辑器 API

        [Test]
        public void AddStep_IncreasesStepCount()
        {
            _player.AddStep(new TweenStepData());
            Assert.AreEqual(1, _player.StepCount);
        }

        [Test]
        public void RemoveStep_DecreasesStepCount()
        {
            _player.AddStep(new TweenStepData());
            _player.AddStep(new TweenStepData());
            _player.RemoveStep(0);
            Assert.AreEqual(1, _player.StepCount);
        }

        [Test]
        public void RemoveStep_InvalidIndex_DoesNotThrow()
        {
            _player.AddStep(new TweenStepData());
            Assert.DoesNotThrow(() => _player.RemoveStep(-1));
            Assert.DoesNotThrow(() => _player.RemoveStep(5));
            Assert.AreEqual(1, _player.StepCount);
        }

        [Test]
        public void ClearAllSteps_SetsStepCountToZero()
        {
            _player.AddStep(new TweenStepData());
            _player.AddStep(new TweenStepData());
            _player.ClearAllSteps();
            Assert.AreEqual(0, _player.StepCount);
        }

        [Test]
        public void GetOrCreateStep_ValidIndex_ReturnsStep()
        {
            var step = new TweenStepData { Duration = 2.5f };
            _player.AddStep(step);
            var result = _player.GetOrCreateStep(0);
            Assert.IsNotNull(result);
            Assert.AreEqual(2.5f, result.Duration);
        }

        [Test]
        public void GetOrCreateStep_InvalidIndex_ReturnsNull()
        {
            Assert.IsNull(_player.GetOrCreateStep(-1));
            Assert.IsNull(_player.GetOrCreateStep(0));
            Assert.IsNull(_player.GetOrCreateStep(5));
        }

        [Test]
        public void MoveStep_ChangesOrder()
        {
            _player.AddStep(new TweenStepData { Duration = 1f });
            _player.AddStep(new TweenStepData { Duration = 2f });
            _player.AddStep(new TweenStepData { Duration = 3f });

            _player.MoveStep(0, 2);

            Assert.AreEqual(2f, _player.Steps[0].Duration);
            Assert.AreEqual(3f, _player.Steps[1].Duration);
            Assert.AreEqual(1f, _player.Steps[2].Duration);
        }

        [Test]
        public void MoveStep_InvalidIndices_DoesNotThrow()
        {
            _player.AddStep(new TweenStepData());
            Assert.DoesNotThrow(() => _player.MoveStep(-1, 0));
            Assert.DoesNotThrow(() => _player.MoveStep(0, -1));
            Assert.DoesNotThrow(() => _player.MoveStep(5, 0));
            Assert.AreEqual(1, _player.StepCount);
        }

        #endregion

        #region Play / Stop

        [Test]
        public void Play_WithNoSteps_DoesNotStartPlaying()
        {
            _player.Play();
            Assert.IsFalse(_player.IsPlaying);
        }

        [Test]
        public void Play_WithDisabledSteps_DoesNotStartPlaying()
        {
            var step = new TweenStepData { IsEnabled = false };
            _player.AddStep(step);
            _player.Play();
            Assert.IsFalse(_player.IsPlaying);
        }

        [Test]
        public void Play_WithStep_SetsIsPlaying()
        {
            _player.AddStep(new TweenStepData
            {
                Type = TweenStepType.Move,
                TargetVector = new Vector3(5f, 0f, 0f),
                Duration = 0.5f
            });

            _player.Play();

            // 推进一帧让 DOTween 启动 OnPlay 回调
            DOTween.ManualUpdate(0.01f, 0.01f);

            Assert.IsTrue(_player.IsPlaying);

            // 推进到动画完成
            SimulateTimeSync(1f);
        }

        [Test]
        public void Stop_SetsIsPlayingFalse()
        {
            _player.AddStep(new TweenStepData
            {
                Type = TweenStepType.Move,
                TargetVector = new Vector3(5f, 0f, 0f),
                Duration = 2f
            });

            _player.Play();
            DOTween.ManualUpdate(0.01f, 0.01f);

            _player.Stop();
            Assert.IsFalse(_player.IsPlaying);
        }

        [Test]
        public void Complete_CompletesAnimation()
        {
            _player.AddStep(new TweenStepData
            {
                Type = TweenStepType.Move,
                TargetVector = new Vector3(5f, 0f, 0f),
                Duration = 2f
            });

            _player.Play();
            DOTween.ManualUpdate(0.01f, 0.01f);

            _player.Complete();
            DOTween.ManualUpdate(0.01f, 0.01f);

            // Complete 后 IsPlaying 应变为 false（OnComplete 回调中重置）
            Assert.IsFalse(_player.IsPlaying);
        }

        #endregion

        #region Pause / Resume

        [Test]
        public void Pause_StopsAnimationProgress()
        {
            _player.AddStep(new TweenStepData
            {
                Type = TweenStepType.Move,
                TargetVector = new Vector3(10f, 0f, 0f),
                Duration = 2f
            });

            _player.Play();
            // 推进一点时间让动画运行
            DOTween.ManualUpdate(0.1f, 0.1f);

            float posBeforePause = _gameObject.transform.position.x;

            _player.Pause();

            // 暂停后继续推进时间，位置不应变化
            DOTween.ManualUpdate(0.5f, 0.5f);

            float posAfterPause = _gameObject.transform.position.x;
            Assert.AreEqual(posBeforePause, posAfterPause, 0.01f,
                "暂停后位置不应变化");
        }

        [Test]
        public void Resume_ContinuesAnimation()
        {
            _player.AddStep(new TweenStepData
            {
                Type = TweenStepType.Move,
                TargetVector = new Vector3(10f, 0f, 0f),
                Duration = 1f
            });

            _player.Play();
            DOTween.ManualUpdate(0.1f, 0.1f);

            _player.Pause();
            DOTween.ManualUpdate(0.1f, 0.1f);

            _player.Resume();
            DOTween.ManualUpdate(0.01f, 0.01f);

            // 恢复后应继续播放
            Assert.IsTrue(_player.IsPlaying);

            // 推进到动画完成
            SimulateTimeSync(1.5f);
        }

        #endregion

        #region 回调触发

        [Test]
        public void OnComplete_CallbackIsInvoked()
        {
            bool completeCalled = false;
            _player.AddStep(new TweenStepData
            {
                Type = TweenStepType.Move,
                TargetVector = new Vector3(5f, 0f, 0f),
                Duration = 0.3f
            });

            _player.OnComplete(() => completeCalled = true);
            _player.Play();

            // 同步推进时间使动画完成
            SimulateTimeSync(0.6f);

            Assert.IsTrue(completeCalled, "OnComplete 回调应被调用");
            Assert.IsFalse(_player.IsPlaying);
        }

        [Test]
        public void OnDone_Completed_RecievesTrue()
        {
            bool? doneResult = null;
            _player.AddStep(new TweenStepData
            {
                Type = TweenStepType.Move,
                TargetVector = new Vector3(5f, 0f, 0f),
                Duration = 0.3f
            });

            _player.OnDone(completed => doneResult = completed);
            _player.Play();

            SimulateTimeSync(0.6f);

            Assert.IsTrue(doneResult.HasValue, "OnDone 应被调用");
            Assert.IsTrue(doneResult.Value, "正常完成应传入 true");
        }

        [Test]
        public void OnDone_Stopped_RecievesFalse()
        {
            bool? doneResult = null;
            _player.AddStep(new TweenStepData
            {
                Type = TweenStepType.Move,
                TargetVector = new Vector3(5f, 0f, 0f),
                Duration = 2f
            });

            _player.OnDone(completed => doneResult = completed);
            _player.Play();

            // 先让动画真正启动
            DOTween.ManualUpdate(0.01f, 0.01f);

            _player.Stop();
            // Stop 内部 KillSequence 会触发 OnKill 回调
            DOTween.ManualUpdate(0.01f, 0.01f);

            Assert.IsTrue(doneResult.HasValue, "OnDone 应被调用");
            Assert.IsFalse(doneResult.Value, "被停止应传入 false");
        }

        #endregion

        #region Restart

        [Test]
        public void Restart_StopsAndReplays()
        {
            _player.AddStep(new TweenStepData
            {
                Type = TweenStepType.Move,
                TargetVector = new Vector3(5f, 0f, 0f),
                Duration = 1f
            });

            _player.Play();
            DOTween.ManualUpdate(0.1f, 0.1f);

            _player.Restart();
            DOTween.ManualUpdate(0.01f, 0.01f);

            Assert.IsTrue(_player.IsPlaying, "Restart 后应正在播放");

            // 推进到动画完成
            SimulateTimeSync(1.5f);
        }

        #endregion

        #region 生命周期控制

        [Test]
        public void PlayTrigger_Manual_DoesNotAutoPlay()
        {
            // 默认 PlayTrigger.Manual，不自动播放
            _player.AddStep(new TweenStepData
            {
                Type = TweenStepType.Move,
                TargetVector = new Vector3(5f, 0f, 0f),
                Duration = 1f
            });

            DOTween.ManualUpdate(0.1f, 0.1f);

            Assert.IsFalse(_player.IsPlaying, "Manual 模式不应自动播放");
        }

        [Test]
        public void DisableAction_Pause_PausesOnDisable()
        {
            _player.AddStep(new TweenStepData
            {
                Type = TweenStepType.Move,
                TargetVector = new Vector3(10f, 0f, 0f),
                Duration = 2f
            });

            _player.Play();
            DOTween.ManualUpdate(0.1f, 0.1f);
            Assert.IsTrue(_player.IsPlaying);

            // 模拟 OnDisable
            _player.SendMessage("OnDisable");
            DOTween.ManualUpdate(0.1f, 0.1f);

            // 暂停后位置应不变
            float posAfterDisable = _gameObject.transform.position.x;
            DOTween.ManualUpdate(0.5f, 0.5f);
            Assert.AreEqual(posAfterDisable, _gameObject.transform.position.x, 0.01f,
                "DisableAction.Pause 暂停后动画不应继续");
        }

        [Test]
        public void DisableAction_Stop_StopsOnDisable()
        {
            _player.AddStep(new TweenStepData
            {
                Type = TweenStepType.Move,
                TargetVector = new Vector3(10f, 0f, 0f),
                Duration = 2f
            });

            _player.Play();
            DOTween.ManualUpdate(0.1f, 0.1f);

            // 模拟 OnDisable
            _player.SendMessage("OnDisable");
            DOTween.ManualUpdate(0.1f, 0.1f);

            Assert.IsFalse(_player.IsPlaying, "DisableAction.Stop 应停止动画");
        }

        [Test]
        public void DisableAction_None_DoesNotAffectAnimation()
        {
            // 通过反射设置 _disableAction = None
            var field = typeof(DOTweenVisualPlayer).GetField("_disableAction",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(_player, DisableAction.None);

            _player.AddStep(new TweenStepData
            {
                Type = TweenStepType.Move,
                TargetVector = new Vector3(10f, 0f, 0f),
                Duration = 2f
            });

            _player.Play();
            DOTween.ManualUpdate(0.1f, 0.1f);

            float posBefore = _gameObject.transform.position.x;

            // 模拟 OnDisable
            _player.SendMessage("OnDisable");
            DOTween.ManualUpdate(0.1f, 0.1f);

            float posAfter = _gameObject.transform.position.x;
            Assert.Greater(Mathf.Abs(posAfter - posBefore), 0.01f,
                "DisableAction.None 不应影响动画播放");
        }

        [Test]
        public void OnEnableResume_ResumesPausedAnimation()
        {
            // 通过反射设置 _playTrigger
            var field = typeof(DOTweenVisualPlayer).GetField("_playTrigger",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(_player, PlayTrigger.OnEnableResume);

            _player.AddStep(new TweenStepData
            {
                Type = TweenStepType.Move,
                TargetVector = new Vector3(10f, 0f, 0f),
                Duration = 2f
            });

            _player.Play();
            DOTween.ManualUpdate(0.1f, 0.1f);
            _player.Pause();
            DOTween.ManualUpdate(0.1f, 0.1f);

            float posBefore = _gameObject.transform.position.x;

            // 模拟 OnEnable
            _player.SendMessage("OnEnable");
            DOTween.ManualUpdate(0.2f, 0.2f);

            float posAfter = _gameObject.transform.position.x;
            Assert.Greater(Mathf.Abs(posAfter - posBefore), 0.01f,
                "OnEnableResume 应恢复暂停的动画");
        }

        [Test]
        public void OnEnableRestart_RestartsAnimation()
        {
            var field = typeof(DOTweenVisualPlayer).GetField("_playTrigger",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(_player, PlayTrigger.OnEnableRestart);

            _player.AddStep(new TweenStepData
            {
                Type = TweenStepType.Move,
                TargetVector = new Vector3(10f, 0f, 0f),
                Duration = 2f
            });

            _player.Play();
            DOTween.ManualUpdate(0.5f, 0.5f);

            float posBefore = _gameObject.transform.position.x;
            Assert.Greater(posBefore, 0f, "应已移动");

            // 模拟 OnEnable → Stop + Play
            _player.SendMessage("OnEnable");
            DOTween.ManualUpdate(0.01f, 0.01f);

            Assert.IsTrue(_player.IsPlaying, "OnEnableRestart 应重新播放");

            // 推进完成
            SimulateTimeSync(2.5f);
        }

        #endregion
    }
}
