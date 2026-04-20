using System.Text;
using NUnit.Framework;
using CNoom.DOTweenVisual.Editor;
using UnityEngine;

namespace CNoom.DOTweenVisual.Editor.Tests
{
    /// <summary>
    /// DOTweenVisualEditorWindow 工具方法测试
    /// 测试时间格式化、Vector3/Color 序列化与反序列化
    /// </summary>
    public class DOTweenVisualEditorWindowUtilityTests
    {
        #region FormatTime

        [Test]
        public void FormatTime_Zero_Returns00_00_0()
        {
            Assert.AreEqual("00:00.0", DOTweenVisualEditorWindow.FormatTime(0f));
        }

        [Test]
        public void FormatTime_65_5_Returns01_05_5()
        {
            Assert.AreEqual("01:05.5", DOTweenVisualEditorWindow.FormatTime(65.5f));
        }

        [Test]
        public void FormatTime_30_0_Returns00_30_0()
        {
            Assert.AreEqual("00:30.0", DOTweenVisualEditorWindow.FormatTime(30f));
        }

        [Test]
        public void FormatTime_120_0_Returns02_00_0()
        {
            Assert.AreEqual("02:00.0", DOTweenVisualEditorWindow.FormatTime(120f));
        }

        [Test]
        public void FormatTime_SmallValue_ReturnsCorrectMs()
        {
            Assert.AreEqual("00:01.5", DOTweenVisualEditorWindow.FormatTime(1.5f));
        }

        [Test]
        public void FormatTime_59_9_Returns00_59_9()
        {
            Assert.AreEqual("00:59.9", DOTweenVisualEditorWindow.FormatTime(59.9f));
        }

        [Test]
        public void FormatTime_60_0_Returns01_00_0()
        {
            Assert.AreEqual("01:00.0", DOTweenVisualEditorWindow.FormatTime(60f));
        }

        #endregion

        #region ParseVector3

        [Test]
        public void ParseVector3_ValidString_ReturnsCorrectVector()
        {
            var result = StepClipboard.ParseVector3("1,2,3");
            Assert.AreEqual(new Vector3(1f, 2f, 3f), result);
        }

        [Test]
        public void ParseVector3_NegativeValues_ReturnsCorrectVector()
        {
            var result = StepClipboard.ParseVector3("-1,-2,-3");
            Assert.AreEqual(new Vector3(-1f, -2f, -3f), result);
        }

        [Test]
        public void ParseVector3_ZeroValues_ReturnsZeroVector()
        {
            var result = StepClipboard.ParseVector3("0,0,0");
            Assert.AreEqual(Vector3.zero, result);
        }

        [Test]
        public void ParseVector3_DecimalValues_ReturnsCorrectVector()
        {
            var result = StepClipboard.ParseVector3("1.5,2.5,3.5");
            Assert.AreEqual(new Vector3(1.5f, 2.5f, 3.5f), result);
        }

        #endregion

        #region AppendVector3

        [Test]
        public void AppendVector3_WritesCorrectFormat()
        {
            var sb = new StringBuilder();
            StepClipboard.AppendVector3(sb, new Vector3(1.5f, 2.5f, 3.5f));
            Assert.AreEqual("1.5,2.5,3.5", sb.ToString());
        }

        [Test]
        public void AppendVector3_ZeroVector_WritesZeros()
        {
            var sb = new StringBuilder();
            StepClipboard.AppendVector3(sb, Vector3.zero);
            Assert.AreEqual("0,0,0", sb.ToString());
        }

        [Test]
        public void AppendVector3_NegativeValues_WritesCorrectFormat()
        {
            var sb = new StringBuilder();
            StepClipboard.AppendVector3(sb, new Vector3(-1f, -2f, -3f));
            Assert.AreEqual("-1,-2,-3", sb.ToString());
        }

        #endregion

        #region AppendAndParseVector3 Roundtrip

        [Test]
        public void AppendAndParseVector3_Roundtrip_PreservesValues()
        {
            var original = new Vector3(1.23f, 4.56f, 7.89f);
            var sb = new StringBuilder();
            StepClipboard.AppendVector3(sb, original);
            var parsed = StepClipboard.ParseVector3(sb.ToString());

            Assert.AreEqual(original.x, parsed.x, 0.0001f);
            Assert.AreEqual(original.y, parsed.y, 0.0001f);
            Assert.AreEqual(original.z, parsed.z, 0.0001f);
        }

        [Test]
        public void AppendAndParseVector3_Roundtrip_NegativeValues()
        {
            var original = new Vector3(-10f, -20f, -30f);
            var sb = new StringBuilder();
            StepClipboard.AppendVector3(sb, original);
            var parsed = StepClipboard.ParseVector3(sb.ToString());

            Assert.AreEqual(original.x, parsed.x, 0.0001f);
            Assert.AreEqual(original.y, parsed.y, 0.0001f);
            Assert.AreEqual(original.z, parsed.z, 0.0001f);
        }

        #endregion

        #region ParseColor

        [Test]
        public void ParseColor_Red_ReturnsRedColor()
        {
            var result = StepClipboard.ParseColor("1,0,0,1");
            Assert.AreEqual(new Color(1f, 0f, 0f, 1f), result);
        }

        [Test]
        public void ParseColor_ZeroAlpha_ReturnsTransparentColor()
        {
            var result = StepClipboard.ParseColor("1,1,1,0");
            Assert.AreEqual(new Color(1f, 1f, 1f, 0f), result);
        }

        [Test]
        public void ParseColor_AllZero_ReturnsBlackTransparent()
        {
            var result = StepClipboard.ParseColor("0,0,0,0");
            Assert.AreEqual(new Color(0f, 0f, 0f, 0f), result);
        }

        #endregion

        #region AppendColor

        [Test]
        public void AppendColor_WritesCorrectFormat()
        {
            var sb = new StringBuilder();
            StepClipboard.AppendColor(sb, new Color(1f, 0.5f, 0f, 1f));
            Assert.AreEqual("1,0.5,0,1", sb.ToString());
        }

        [Test]
        public void AppendColor_White_WritesOnes()
        {
            var sb = new StringBuilder();
            StepClipboard.AppendColor(sb, Color.white);
            Assert.AreEqual("1,1,1,1", sb.ToString());
        }

        #endregion

        #region AppendAndParseColor Roundtrip

        [Test]
        public void AppendAndParseColor_Roundtrip_PreservesValues()
        {
            var original = new Color(0.1f, 0.2f, 0.3f, 0.4f);
            var sb = new StringBuilder();
            StepClipboard.AppendColor(sb, original);
            var parsed = StepClipboard.ParseColor(sb.ToString());

            Assert.AreEqual(original.r, parsed.r, 0.0001f);
            Assert.AreEqual(original.g, parsed.g, 0.0001f);
            Assert.AreEqual(original.b, parsed.b, 0.0001f);
            Assert.AreEqual(original.a, parsed.a, 0.0001f);
        }

        [Test]
        public void AppendAndParseColor_Roundtrip_FullAlpha()
        {
            var original = new Color(0.75f, 0.25f, 0.5f, 1f);
            var sb = new StringBuilder();
            StepClipboard.AppendColor(sb, original);
            var parsed = StepClipboard.ParseColor(sb.ToString());

            Assert.AreEqual(original.r, parsed.r, 0.0001f);
            Assert.AreEqual(original.g, parsed.g, 0.0001f);
            Assert.AreEqual(original.b, parsed.b, 0.0001f);
            Assert.AreEqual(original.a, parsed.a, 0.0001f);
        }

        #endregion
    }
}
