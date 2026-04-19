using NUnit.Framework;
using CNoom.DOTweenVisual.Data;
using CNoom.DOTweenVisual.Editor;
using UnityEngine;

namespace CNoom.DOTweenVisual.Editor.Tests
{
    /// <summary>
    /// DOTweenEditorStyle 样式配置测试
    /// 测试显示名称映射、CSS 类名映射、颜色映射
    /// </summary>
    public class DOTweenEditorStyleTests
    {
        #region GetStepDisplayName - 全类型覆盖

        [Test]
        public void GetStepDisplayName_Move_ReturnsMove()
        {
            Assert.AreEqual("Move", DOTweenEditorStyle.GetStepDisplayName(TweenStepType.Move));
        }

        [Test]
        public void GetStepDisplayName_Rotate_ReturnsRotate()
        {
            Assert.AreEqual("Rotate", DOTweenEditorStyle.GetStepDisplayName(TweenStepType.Rotate));
        }

        [Test]
        public void GetStepDisplayName_Scale_ReturnsScale()
        {
            Assert.AreEqual("Scale", DOTweenEditorStyle.GetStepDisplayName(TweenStepType.Scale));
        }

        [Test]
        public void GetStepDisplayName_Color_ReturnsColor()
        {
            Assert.AreEqual("Color", DOTweenEditorStyle.GetStepDisplayName(TweenStepType.Color));
        }

        [Test]
        public void GetStepDisplayName_Fade_ReturnsFade()
        {
            Assert.AreEqual("Fade", DOTweenEditorStyle.GetStepDisplayName(TweenStepType.Fade));
        }

        [Test]
        public void GetStepDisplayName_AnchorMove_ReturnsAnchorMove()
        {
            Assert.AreEqual("AnchorMove", DOTweenEditorStyle.GetStepDisplayName(TweenStepType.AnchorMove));
        }

        [Test]
        public void GetStepDisplayName_SizeDelta_ReturnsSizeDelta()
        {
            Assert.AreEqual("SizeDelta", DOTweenEditorStyle.GetStepDisplayName(TweenStepType.SizeDelta));
        }

        [Test]
        public void GetStepDisplayName_Jump_ReturnsJump()
        {
            Assert.AreEqual("Jump", DOTweenEditorStyle.GetStepDisplayName(TweenStepType.Jump));
        }

        [Test]
        public void GetStepDisplayName_Punch_ReturnsPunch()
        {
            Assert.AreEqual("Punch", DOTweenEditorStyle.GetStepDisplayName(TweenStepType.Punch));
        }

        [Test]
        public void GetStepDisplayName_Shake_ReturnsShake()
        {
            Assert.AreEqual("Shake", DOTweenEditorStyle.GetStepDisplayName(TweenStepType.Shake));
        }

        [Test]
        public void GetStepDisplayName_FillAmount_ReturnsFillAmount()
        {
            Assert.AreEqual("FillAmount", DOTweenEditorStyle.GetStepDisplayName(TweenStepType.FillAmount));
        }

        [Test]
        public void GetStepDisplayName_DOPath_ReturnsDOPath()
        {
            Assert.AreEqual("DOPath", DOTweenEditorStyle.GetStepDisplayName(TweenStepType.DOPath));
        }

        [Test]
        public void GetStepDisplayName_Delay_ReturnsDelay()
        {
            Assert.AreEqual("Delay", DOTweenEditorStyle.GetStepDisplayName(TweenStepType.Delay));
        }

        [Test]
        public void GetStepDisplayName_Callback_ReturnsCallback()
        {
            Assert.AreEqual("Callback", DOTweenEditorStyle.GetStepDisplayName(TweenStepType.Callback));
        }

        #endregion

        #region GetExecutionModeCssClass

        [Test]
        public void GetExecutionModeCssClass_Append_ReturnsModeAppend()
        {
            Assert.AreEqual("mode-append", DOTweenEditorStyle.GetExecutionModeCssClass(ExecutionMode.Append));
        }

        [Test]
        public void GetExecutionModeCssClass_Join_ReturnsModeJoin()
        {
            Assert.AreEqual("mode-join", DOTweenEditorStyle.GetExecutionModeCssClass(ExecutionMode.Join));
        }

        [Test]
        public void GetExecutionModeCssClass_Insert_ReturnsModeInsert()
        {
            Assert.AreEqual("mode-insert", DOTweenEditorStyle.GetExecutionModeCssClass(ExecutionMode.Insert));
        }

        #endregion

        #region GetExecutionModeColor

        [Test]
        public void GetExecutionModeColor_Append_ReturnsBlueColor()
        {
            var color = DOTweenEditorStyle.GetExecutionModeColor(ExecutionMode.Append);
            var expected = new Color(0.29f, 0.56f, 0.85f);
            Assert.AreEqual(expected.r, color.r, 0.01f);
            Assert.AreEqual(expected.g, color.g, 0.01f);
            Assert.AreEqual(expected.b, color.b, 0.01f);
        }

        [Test]
        public void GetExecutionModeColor_Join_ReturnsGreenColor()
        {
            var color = DOTweenEditorStyle.GetExecutionModeColor(ExecutionMode.Join);
            var expected = new Color(0.29f, 0.85f, 0.29f);
            Assert.AreEqual(expected.r, color.r, 0.01f);
            Assert.AreEqual(expected.g, color.g, 0.01f);
            Assert.AreEqual(expected.b, color.b, 0.01f);
        }

        [Test]
        public void GetExecutionModeColor_Insert_ReturnsOrangeColor()
        {
            var color = DOTweenEditorStyle.GetExecutionModeColor(ExecutionMode.Insert);
            var expected = new Color(0.85f, 0.60f, 0.29f);
            Assert.AreEqual(expected.r, color.r, 0.01f);
            Assert.AreEqual(expected.g, color.g, 0.01f);
            Assert.AreEqual(expected.b, color.b, 0.01f);
        }

        #endregion
    }
}
