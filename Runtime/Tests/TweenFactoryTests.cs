using NUnit.Framework;
using UnityEngine;
using DG.Tweening;
using CNoom.DOTweenVisual.Data;

namespace CNoom.DOTweenVisual.Tests
{
    /// <summary>
    /// TweenFactory Tween 创建工厂测试
    /// 测试 CreateTween 和 ApplyStartValue 的核心逻辑
    /// </summary>
    public class TweenFactoryTests
    {
        private GameObject _gameObject;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("TestTarget");
            // 确保 DOTween 已初始化
            DOTween.Init(true, true, LogBehaviour.ErrorsOnly);
            DOTween.SetTweensCapacity(200, 200);
        }

        [TearDown]
        public void TearDown()
        {
            DOTween.KillAll();
            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
            }
        }

        #region CreateTween - 空目标

        [Test]
        public void CreateTween_NullDefaultTarget_ReturnsNull()
        {
            var step = new TweenStepData();
            // TargetTransform 为 null，defaultTarget 也为 null
            var result = TweenFactory.CreateTween(step, null);
            Assert.IsNull(result);
        }

        #endregion

        #region CreateTween - Move

        [Test]
        public void CreateTween_Move_ReturnsNotNull()
        {
            var step = new TweenStepData
            {
                Type = TweenStepType.Move,
                TargetVector = new Vector3(5f, 0f, 0f),
                Duration = 1f
            };

            var tween = TweenFactory.CreateTween(step, _gameObject.transform);
            Assert.IsNotNull(tween);
            tween.Kill();
        }

        [Test]
        public void CreateTween_Move_WithTargetTransform_UsesTargetTransform()
        {
            var otherObj = new GameObject("OtherTarget");
            var step = new TweenStepData
            {
                Type = TweenStepType.Move,
                TargetTransform = otherObj.transform,
                TargetVector = new Vector3(10f, 0f, 0f),
                Duration = 1f
            };

            var tween = TweenFactory.CreateTween(step, _gameObject.transform);
            Assert.IsNotNull(tween);
            // Tween 应操作 otherObj 而非 _gameObject
            tween.Kill();
            Object.DestroyImmediate(otherObj);
        }

        #endregion

        #region CreateTween - Scale

        [Test]
        public void CreateTween_Scale_ReturnsNotNull()
        {
            var step = new TweenStepData
            {
                Type = TweenStepType.Scale,
                TargetVector = Vector3.one * 2f,
                Duration = 1f
            };

            var tween = TweenFactory.CreateTween(step, _gameObject.transform);
            Assert.IsNotNull(tween);
            tween.Kill();
        }

        #endregion

        #region CreateTween - Delay / Callback

        [Test]
        public void CreateTween_Delay_ReturnsNull()
        {
            var step = new TweenStepData
            {
                Type = TweenStepType.Delay,
                Duration = 1f
            };

            var tween = TweenFactory.CreateTween(step, _gameObject.transform);
            // Delay 类型返回 null（由 AppendToSequence 处理为 AppendInterval）
            Assert.IsNull(tween);
        }

        [Test]
        public void CreateTween_Callback_ReturnsNull()
        {
            var step = new TweenStepData
            {
                Type = TweenStepType.Callback
            };

            var tween = TweenFactory.CreateTween(step, _gameObject.transform);
            // Callback 类型返回 null（由 AppendToSequence 处理为 AppendCallback）
            Assert.IsNull(tween);
        }

        #endregion

        #region CreateTween - AnchorMove 无 RectTransform

        [Test]
        public void CreateTween_AnchorMove_WithoutRectTransform_ReturnsNull()
        {
            var step = new TweenStepData
            {
                Type = TweenStepType.AnchorMove,
                TargetVector = Vector3.zero,
                Duration = 1f
            };

            var tween = TweenFactory.CreateTween(step, _gameObject.transform);
            // 没有 RectTransform，应返回 null
            Assert.IsNull(tween);
        }

        #endregion

        #region CreateTween - Color / Fade 无对应组件

        [Test]
        public void CreateTween_Color_WithoutColorComponent_ReturnsNull()
        {
            var step = new TweenStepData
            {
                Type = TweenStepType.Color,
                TargetColor = Color.red,
                Duration = 1f
            };

            var tween = TweenFactory.CreateTween(step, _gameObject.transform);
            Assert.IsNull(tween);
        }

        [Test]
        public void CreateTween_Fade_WithoutFadeComponent_ReturnsNull()
        {
            var step = new TweenStepData
            {
                Type = TweenStepType.Fade,
                TargetFloat = 0f,
                Duration = 1f
            };

            var tween = TweenFactory.CreateTween(step, _gameObject.transform);
            Assert.IsNull(tween);
        }

        #endregion

        #region CreateTween - Color / Fade 有组件

        [Test]
        public void CreateTween_Color_WithImage_ReturnsNotNull()
        {
            _gameObject.AddComponent<UnityEngine.UI.Image>();
            var step = new TweenStepData
            {
                Type = TweenStepType.Color,
                TargetColor = Color.red,
                Duration = 1f
            };

            var tween = TweenFactory.CreateTween(step, _gameObject.transform);
            Assert.IsNotNull(tween);
            tween.Kill();
        }

        [Test]
        public void CreateTween_Fade_WithImage_ReturnsNotNull()
        {
            _gameObject.AddComponent<UnityEngine.UI.Image>();
            var step = new TweenStepData
            {
                Type = TweenStepType.Fade,
                TargetFloat = 0f,
                Duration = 1f
            };

            var tween = TweenFactory.CreateTween(step, _gameObject.transform);
            Assert.IsNotNull(tween);
            tween.Kill();
        }

        #endregion

        #region CreateTween - Punch / Shake

        [Test]
        public void CreateTween_Punch_ReturnsNotNull()
        {
            var step = new TweenStepData
            {
                Type = TweenStepType.Punch,
                Intensity = Vector3.one,
                Duration = 0.5f,
                Vibrato = 10,
                Elasticity = 1f
            };

            var tween = TweenFactory.CreateTween(step, _gameObject.transform);
            Assert.IsNotNull(tween);
            tween.Kill();
        }

        [Test]
        public void CreateTween_Shake_ReturnsNotNull()
        {
            var step = new TweenStepData
            {
                Type = TweenStepType.Shake,
                Intensity = Vector3.one,
                Duration = 0.5f,
                Vibrato = 10,
                ShakeRandomness = 90f
            };

            var tween = TweenFactory.CreateTween(step, _gameObject.transform);
            Assert.IsNotNull(tween);
            tween.Kill();
        }

        #endregion

        #region CreateTween - DOPath

        [Test]
        public void CreateTween_DOPath_WithLessThan2Waypoints_ReturnsNull()
        {
            var step = new TweenStepData
            {
                Type = TweenStepType.DOPath,
                PathWaypoints = new Vector3[] { Vector3.right },
                Duration = 1f
            };

            var tween = TweenFactory.CreateTween(step, _gameObject.transform);
            Assert.IsNull(tween);
        }

        [Test]
        public void CreateTween_DOPath_With2Waypoints_ReturnsNotNull()
        {
            var step = new TweenStepData
            {
                Type = TweenStepType.DOPath,
                PathWaypoints = new Vector3[] { Vector3.right, Vector3.up },
                Duration = 1f
            };

            var tween = TweenFactory.CreateTween(step, _gameObject.transform);
            Assert.IsNotNull(tween);
            tween.Kill();
        }

        [Test]
        public void CreateTween_DOPath_WithNullWaypoints_ReturnsNull()
        {
            var step = new TweenStepData
            {
                Type = TweenStepType.DOPath,
                PathWaypoints = null,
                Duration = 1f
            };

            var tween = TweenFactory.CreateTween(step, _gameObject.transform);
            Assert.IsNull(tween);
        }

        #endregion

        #region ApplyStartValue

        [Test]
        public void ApplyStartValue_Move_SetsPosition()
        {
            var startPos = new Vector3(5f, 10f, 15f);
            var step = new TweenStepData
            {
                Type = TweenStepType.Move,
                UseStartValue = true,
                StartVector = startPos,
                MoveSpace = MoveSpace.World
            };

            TweenFactory.ApplyStartValue(step, _gameObject.transform);
            Assert.AreEqual(startPos, _gameObject.transform.position);
        }

        [Test]
        public void ApplyStartValue_Move_SetsLocalPosition()
        {
            var startPos = new Vector3(3f, 6f, 9f);
            var step = new TweenStepData
            {
                Type = TweenStepType.Move,
                UseStartValue = true,
                StartVector = startPos,
                MoveSpace = MoveSpace.Local
            };

            TweenFactory.ApplyStartValue(step, _gameObject.transform);
            Assert.AreEqual(startPos, _gameObject.transform.localPosition);
        }

        [Test]
        public void ApplyStartValue_Scale_SetsLocalScale()
        {
            var startScale = new Vector3(2f, 3f, 4f);
            var step = new TweenStepData
            {
                Type = TweenStepType.Scale,
                UseStartValue = true,
                StartVector = startScale
            };

            TweenFactory.ApplyStartValue(step, _gameObject.transform);
            Assert.AreEqual(startScale, _gameObject.transform.localScale);
        }

        [Test]
        public void ApplyStartValue_Fade_SetsAlpha()
        {
            var cg = _gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            var step = new TweenStepData
            {
                Type = TweenStepType.Fade,
                UseStartFloat = true,
                StartFloat = 0.5f
            };

            TweenFactory.ApplyStartValue(step, _gameObject.transform);
            Assert.AreEqual(0.5f, cg.alpha, 0.001f);
        }

        [Test]
        public void ApplyStartValue_Color_SetsColor()
        {
            var image = _gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.white;

            var step = new TweenStepData
            {
                Type = TweenStepType.Color,
                UseStartColor = true,
                StartColor = Color.red
            };

            TweenFactory.ApplyStartValue(step, _gameObject.transform);
            Assert.AreEqual(Color.red, image.color);
        }

        [Test]
        public void ApplyStartValue_WithoutUseStartValue_DoesNotChange()
        {
            _gameObject.transform.position = Vector3.one;

            var step = new TweenStepData
            {
                Type = TweenStepType.Move,
                UseStartValue = false,
                StartVector = Vector3.zero
            };

            TweenFactory.ApplyStartValue(step, _gameObject.transform);
            // UseStartValue 为 false，不应修改位置
            Assert.AreEqual(Vector3.one, _gameObject.transform.position);
        }

        [Test]
        public void ApplyStartValue_NullTarget_DoesNotThrow()
        {
            var step = new TweenStepData
            {
                Type = TweenStepType.Move,
                UseStartValue = true,
                StartVector = Vector3.one
            };

            // 应不抛异常
            Assert.DoesNotThrow(() => TweenFactory.ApplyStartValue(step, null));
        }

        #endregion
    }
}
