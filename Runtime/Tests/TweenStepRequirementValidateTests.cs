using NUnit.Framework;
using UnityEngine;
using CNoom.DOTweenVisual.Data;

namespace CNoom.DOTweenVisual.Tests
{
    /// <summary>
    /// TweenStepRequirement.Validate 组件校验测试
    /// 测试各种目标物体在不同动画类型下的校验逻辑
    /// </summary>
    public class TweenStepRequirementValidateTests
    {
        private GameObject _gameObject;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("TestTarget");
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
            }
        }

        #region 空目标

        [Test]
        public void Validate_NullTarget_ReturnsFalse()
        {
            var result = TweenStepRequirement.Validate(null, TweenStepType.Move, out var errorMsg);
            Assert.IsFalse(result);
            Assert.IsNotNull(errorMsg);
        }

        [Test]
        public void Validate_NullTarget_ErrorMessageContainsEmpty()
        {
            TweenStepRequirement.Validate(null, TweenStepType.Move, out var errorMsg);
            Assert.IsTrue(errorMsg.Contains("空"),
                $"空目标错误信息应包含'空'，实际：{errorMsg}");
        }

        #endregion

        #region 无额外组件需求的类型

        [Test]
        public void Validate_Move_WithPlainTransform_ReturnsTrue()
        {
            Assert.IsTrue(TweenStepRequirement.Validate(
                _gameObject.transform, TweenStepType.Move, out _));
        }

        [Test]
        public void Validate_Rotate_WithPlainTransform_ReturnsTrue()
        {
            Assert.IsTrue(TweenStepRequirement.Validate(
                _gameObject.transform, TweenStepType.Rotate, out _));
        }

        [Test]
        public void Validate_Scale_WithPlainTransform_ReturnsTrue()
        {
            Assert.IsTrue(TweenStepRequirement.Validate(
                _gameObject.transform, TweenStepType.Scale, out _));
        }

        [Test]
        public void Validate_Jump_WithPlainTransform_ReturnsTrue()
        {
            Assert.IsTrue(TweenStepRequirement.Validate(
                _gameObject.transform, TweenStepType.Jump, out _));
        }

        [Test]
        public void Validate_Punch_WithPlainTransform_ReturnsTrue()
        {
            Assert.IsTrue(TweenStepRequirement.Validate(
                _gameObject.transform, TweenStepType.Punch, out _));
        }

        [Test]
        public void Validate_Shake_WithPlainTransform_ReturnsTrue()
        {
            Assert.IsTrue(TweenStepRequirement.Validate(
                _gameObject.transform, TweenStepType.Shake, out _));
        }

        [Test]
        public void Validate_DOPath_WithPlainTransform_ReturnsTrue()
        {
            Assert.IsTrue(TweenStepRequirement.Validate(
                _gameObject.transform, TweenStepType.DOPath, out _));
        }

        #endregion

        #region 流程控制类型（无需求）

        [Test]
        public void Validate_Delay_ReturnsTrue()
        {
            Assert.IsTrue(TweenStepRequirement.Validate(
                _gameObject.transform, TweenStepType.Delay, out _));
        }

        [Test]
        public void Validate_Callback_ReturnsTrue()
        {
            Assert.IsTrue(TweenStepRequirement.Validate(
                _gameObject.transform, TweenStepType.Callback, out _));
        }

        #endregion

        #region Color 类型

        [Test]
        public void Validate_Color_WithoutColorComponent_ReturnsFalse()
        {
            var result = TweenStepRequirement.Validate(
                _gameObject.transform, TweenStepType.Color, out var errorMsg);
            Assert.IsFalse(result);
            Assert.IsNotNull(errorMsg);
        }

        [Test]
        public void Validate_Color_WithImage_ReturnsTrue()
        {
            _gameObject.AddComponent<UnityEngine.UI.Image>();
            Assert.IsTrue(TweenStepRequirement.Validate(
                _gameObject.transform, TweenStepType.Color, out _));
        }

        [Test]
        public void Validate_Color_WithSpriteRenderer_ReturnsTrue()
        {
            _gameObject.AddComponent<SpriteRenderer>();
            Assert.IsTrue(TweenStepRequirement.Validate(
                _gameObject.transform, TweenStepType.Color, out _));
        }

        #endregion

        #region Fade 类型

        [Test]
        public void Validate_Fade_WithoutFadeComponent_ReturnsFalse()
        {
            var result = TweenStepRequirement.Validate(
                _gameObject.transform, TweenStepType.Fade, out var errorMsg);
            Assert.IsFalse(result);
            Assert.IsNotNull(errorMsg);
        }

        [Test]
        public void Validate_Fade_WithCanvasGroup_ReturnsTrue()
        {
            _gameObject.AddComponent<CanvasGroup>();
            Assert.IsTrue(TweenStepRequirement.Validate(
                _gameObject.transform, TweenStepType.Fade, out _));
        }

        [Test]
        public void Validate_Fade_WithImage_ReturnsTrue()
        {
            _gameObject.AddComponent<UnityEngine.UI.Image>();
            Assert.IsTrue(TweenStepRequirement.Validate(
                _gameObject.transform, TweenStepType.Fade, out _));
        }

        [Test]
        public void Validate_Fade_WithSpriteRenderer_ReturnsTrue()
        {
            _gameObject.AddComponent<SpriteRenderer>();
            Assert.IsTrue(TweenStepRequirement.Validate(
                _gameObject.transform, TweenStepType.Fade, out _));
        }

        #endregion

        #region AnchorMove / SizeDelta 类型

        [Test]
        public void Validate_AnchorMove_WithoutRectTransform_ReturnsFalse()
        {
            var result = TweenStepRequirement.Validate(
                _gameObject.transform, TweenStepType.AnchorMove, out var errorMsg);
            Assert.IsFalse(result);
            Assert.IsNotNull(errorMsg);
        }

        [Test]
        public void Validate_AnchorMove_WithRectTransform_ReturnsTrue()
        {
            // 在 Canvas 下创建 UI 物体（RectTransform 需要 Canvas）
            var canvas = new GameObject("Canvas");
            canvas.AddComponent<Canvas>();
            var uiObj = new GameObject("UIElement");
            uiObj.transform.SetParent(canvas.transform);
            uiObj.AddComponent<UnityEngine.UI.Image>();

            Assert.IsTrue(TweenStepRequirement.Validate(
                uiObj.transform, TweenStepType.AnchorMove, out _));

            Object.DestroyImmediate(canvas);
        }

        [Test]
        public void Validate_SizeDelta_WithoutRectTransform_ReturnsFalse()
        {
            var result = TweenStepRequirement.Validate(
                _gameObject.transform, TweenStepType.SizeDelta, out var errorMsg);
            Assert.IsFalse(result);
            Assert.IsNotNull(errorMsg);
        }

        #endregion

        #region FillAmount 类型

        [Test]
        public void Validate_FillAmount_WithoutImage_ReturnsFalse()
        {
            var result = TweenStepRequirement.Validate(
                _gameObject.transform, TweenStepType.FillAmount, out var errorMsg);
            Assert.IsFalse(result);
            Assert.IsNotNull(errorMsg);
        }

        [Test]
        public void Validate_FillAmount_WithImage_ReturnsTrue()
        {
            _gameObject.AddComponent<UnityEngine.UI.Image>();
            Assert.IsTrue(TweenStepRequirement.Validate(
                _gameObject.transform, TweenStepType.FillAmount, out _));
        }

        #endregion

        #region 能力检测

        [Test]
        public void HasColorTarget_NullTarget_ReturnsFalse()
        {
            Assert.IsFalse(TweenStepRequirement.HasColorTarget(null));
        }

        [Test]
        public void HasColorTarget_PlainTransform_ReturnsFalse()
        {
            Assert.IsFalse(TweenStepRequirement.HasColorTarget(_gameObject.transform));
        }

        [Test]
        public void HasColorTarget_WithImage_ReturnsTrue()
        {
            _gameObject.AddComponent<UnityEngine.UI.Image>();
            Assert.IsTrue(TweenStepRequirement.HasColorTarget(_gameObject.transform));
        }

        [Test]
        public void HasFadeTarget_NullTarget_ReturnsFalse()
        {
            Assert.IsFalse(TweenStepRequirement.HasFadeTarget(null));
        }

        [Test]
        public void HasFadeTarget_PlainTransform_ReturnsFalse()
        {
            Assert.IsFalse(TweenStepRequirement.HasFadeTarget(_gameObject.transform));
        }

        [Test]
        public void HasFadeTarget_WithCanvasGroup_ReturnsTrue()
        {
            _gameObject.AddComponent<CanvasGroup>();
            Assert.IsTrue(TweenStepRequirement.HasFadeTarget(_gameObject.transform));
        }

        [Test]
        public void HasRectTransform_NullTarget_ReturnsFalse()
        {
            Assert.IsFalse(TweenStepRequirement.HasRectTransform(null));
        }

        [Test]
        public void HasRectTransform_PlainTransform_ReturnsFalse()
        {
            Assert.IsFalse(TweenStepRequirement.HasRectTransform(_gameObject.transform));
        }

        #endregion
    }
}
