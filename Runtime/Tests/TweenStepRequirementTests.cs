using NUnit.Framework;
using CNoom.DOTweenVisual.Data;

namespace CNoom.DOTweenVisual.Tests
{
    /// <summary>
    /// TweenStepRequirement 组件需求校验测试
    /// 测试 GetRequirementDescription 的返回值
    /// </summary>
    public class TweenStepRequirementTests
    {
        #region GetRequirementDescription - 有需求的类型

        [Test]
        public void GetRequirementDescription_Color_包含Graphic()
        {
            var desc = TweenStepRequirement.GetRequirementDescription(TweenStepType.Color);
            Assert.IsNotNull(desc);
            Assert.IsTrue(desc.Contains("Graphic"),
                $"Color 类型需求描述应包含 'Graphic'，实际：{desc}");
        }

        [Test]
        public void GetRequirementDescription_Color_包含Renderer()
        {
            var desc = TweenStepRequirement.GetRequirementDescription(TweenStepType.Color);
            Assert.IsNotNull(desc);
            Assert.IsTrue(desc.Contains("Renderer"),
                $"Color 类型需求描述应包含 'Renderer'，实际：{desc}");
        }

        [Test]
        public void GetRequirementDescription_Fade_包含CanvasGroup()
        {
            var desc = TweenStepRequirement.GetRequirementDescription(TweenStepType.Fade);
            Assert.IsNotNull(desc);
            Assert.IsTrue(desc.Contains("CanvasGroup"),
                $"Fade 类型需求描述应包含 'CanvasGroup'，实际：{desc}");
        }

        [Test]
        public void GetRequirementDescription_AnchorMove_包含RectTransform()
        {
            var desc = TweenStepRequirement.GetRequirementDescription(TweenStepType.AnchorMove);
            Assert.IsNotNull(desc);
            Assert.IsTrue(desc.Contains("RectTransform"),
                $"AnchorMove 类型需求描述应包含 'RectTransform'，实际：{desc}");
        }

        [Test]
        public void GetRequirementDescription_SizeDelta_包含RectTransform()
        {
            var desc = TweenStepRequirement.GetRequirementDescription(TweenStepType.SizeDelta);
            Assert.IsNotNull(desc);
            Assert.IsTrue(desc.Contains("RectTransform"),
                $"SizeDelta 类型需求描述应包含 'RectTransform'，实际：{desc}");
        }

        [Test]
        public void GetRequirementDescription_FillAmount_包含Image()
        {
            var desc = TweenStepRequirement.GetRequirementDescription(TweenStepType.FillAmount);
            Assert.IsNotNull(desc);
            Assert.IsTrue(desc.Contains("Image"),
                $"FillAmount 类型需求描述应包含 'Image'，实际：{desc}");
        }

        #endregion

        #region GetRequirementDescription - 无需求的类型

        [Test]
        public void GetRequirementDescription_Move_ReturnsNull()
        {
            Assert.IsNull(TweenStepRequirement.GetRequirementDescription(TweenStepType.Move));
        }

        [Test]
        public void GetRequirementDescription_Rotate_ReturnsNull()
        {
            Assert.IsNull(TweenStepRequirement.GetRequirementDescription(TweenStepType.Rotate));
        }

        [Test]
        public void GetRequirementDescription_Scale_ReturnsNull()
        {
            Assert.IsNull(TweenStepRequirement.GetRequirementDescription(TweenStepType.Scale));
        }

        [Test]
        public void GetRequirementDescription_Jump_ReturnsNull()
        {
            Assert.IsNull(TweenStepRequirement.GetRequirementDescription(TweenStepType.Jump));
        }

        [Test]
        public void GetRequirementDescription_Punch_ReturnsNull()
        {
            Assert.IsNull(TweenStepRequirement.GetRequirementDescription(TweenStepType.Punch));
        }

        [Test]
        public void GetRequirementDescription_Shake_ReturnsNull()
        {
            Assert.IsNull(TweenStepRequirement.GetRequirementDescription(TweenStepType.Shake));
        }

        [Test]
        public void GetRequirementDescription_DOPath_ReturnsNull()
        {
            Assert.IsNull(TweenStepRequirement.GetRequirementDescription(TweenStepType.DOPath));
        }

        [Test]
        public void GetRequirementDescription_Delay_ReturnsNull()
        {
            Assert.IsNull(TweenStepRequirement.GetRequirementDescription(TweenStepType.Delay));
        }

        [Test]
        public void GetRequirementDescription_Callback_ReturnsNull()
        {
            Assert.IsNull(TweenStepRequirement.GetRequirementDescription(TweenStepType.Callback));
        }

        #endregion
    }
}
