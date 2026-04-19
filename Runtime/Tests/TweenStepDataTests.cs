using NUnit.Framework;
using CNoom.DOTweenVisual.Data;
using DG.Tweening;
using UnityEngine;

namespace CNoom.DOTweenVisual.Tests
{
    /// <summary>
    /// TweenStepData 数据类测试
    /// 验证所有字段的默认值符合预期
    /// </summary>
    public class TweenStepDataTests
    {
        #region 基本信息

        [Test]
        public void DefaultValues_IsEnabled_IsTrue()
        {
            var data = new TweenStepData();
            Assert.IsTrue(data.IsEnabled);
        }

        [Test]
        public void DefaultValues_Type_IsMove()
        {
            var data = new TweenStepData();
            Assert.AreEqual(TweenStepType.Move, data.Type);
        }

        #endregion

        #region 时长控制

        [Test]
        public void DefaultValues_Duration_IsOne()
        {
            var data = new TweenStepData();
            Assert.AreEqual(1f, data.Duration);
        }

        [Test]
        public void DefaultValues_Delay_IsZero()
        {
            var data = new TweenStepData();
            Assert.AreEqual(0f, data.Delay);
        }

        #endregion

        #region 缓动曲线

        [Test]
        public void DefaultValues_Ease_IsOutQuad()
        {
            var data = new TweenStepData();
            Assert.AreEqual(Ease.OutQuad, data.Ease);
        }

        [Test]
        public void DefaultValues_UseCustomCurve_IsFalse()
        {
            var data = new TweenStepData();
            Assert.IsFalse(data.UseCustomCurve);
        }

        #endregion

        #region Transform 值组

        [Test]
        public void DefaultValues_TargetTransform_IsNull()
        {
            var data = new TweenStepData();
            Assert.IsNull(data.TargetTransform);
        }

        [Test]
        public void DefaultValues_MoveSpace_IsWorld()
        {
            var data = new TweenStepData();
            Assert.AreEqual(MoveSpace.World, data.MoveSpace);
        }

        [Test]
        public void DefaultValues_RotateSpace_IsWorld()
        {
            var data = new TweenStepData();
            Assert.AreEqual(RotateSpace.World, data.RotateSpace);
        }

        [Test]
        public void DefaultValues_PunchTarget_IsPosition()
        {
            var data = new TweenStepData();
            Assert.AreEqual(PunchTarget.Position, data.PunchTarget);
        }

        [Test]
        public void DefaultValues_ShakeTarget_IsPosition()
        {
            var data = new TweenStepData();
            Assert.AreEqual(ShakeTarget.Position, data.ShakeTarget);
        }

        [Test]
        public void DefaultValues_UseStartValue_IsFalse()
        {
            var data = new TweenStepData();
            Assert.IsFalse(data.UseStartValue);
        }

        [Test]
        public void DefaultValues_StartVector_IsZero()
        {
            var data = new TweenStepData();
            Assert.AreEqual(Vector3.zero, data.StartVector);
        }

        [Test]
        public void DefaultValues_TargetVector_IsZero()
        {
            var data = new TweenStepData();
            Assert.AreEqual(Vector3.zero, data.TargetVector);
        }

        [Test]
        public void DefaultValues_IsRelative_IsFalse()
        {
            var data = new TweenStepData();
            Assert.IsFalse(data.IsRelative);
        }

        #endregion

        #region Color 值组

        [Test]
        public void DefaultValues_UseStartColor_IsFalse()
        {
            var data = new TweenStepData();
            Assert.IsFalse(data.UseStartColor);
        }

        [Test]
        public void DefaultValues_StartColor_IsWhite()
        {
            var data = new TweenStepData();
            Assert.AreEqual(Color.white, data.StartColor);
        }

        [Test]
        public void DefaultValues_TargetColor_IsWhite()
        {
            var data = new TweenStepData();
            Assert.AreEqual(Color.white, data.TargetColor);
        }

        #endregion

        #region Float 值组

        [Test]
        public void DefaultValues_UseStartFloat_IsFalse()
        {
            var data = new TweenStepData();
            Assert.IsFalse(data.UseStartFloat);
        }

        [Test]
        public void DefaultValues_StartFloat_IsOne()
        {
            var data = new TweenStepData();
            Assert.AreEqual(1f, data.StartFloat);
        }

        [Test]
        public void DefaultValues_TargetFloat_IsZero()
        {
            var data = new TweenStepData();
            Assert.AreEqual(0f, data.TargetFloat);
        }

        #endregion

        #region 特效参数值组

        [Test]
        public void DefaultValues_JumpHeight_IsOne()
        {
            var data = new TweenStepData();
            Assert.AreEqual(1f, data.JumpHeight);
        }

        [Test]
        public void DefaultValues_JumpNum_IsOne()
        {
            var data = new TweenStepData();
            Assert.AreEqual(1, data.JumpNum);
        }

        [Test]
        public void DefaultValues_Intensity_IsOne()
        {
            var data = new TweenStepData();
            Assert.AreEqual(Vector3.one, data.Intensity);
        }

        [Test]
        public void DefaultValues_ShakeRandomness_IsNinety()
        {
            var data = new TweenStepData();
            Assert.AreEqual(90f, data.ShakeRandomness);
        }

        [Test]
        public void DefaultValues_Vibrato_IsTen()
        {
            var data = new TweenStepData();
            Assert.AreEqual(10, data.Vibrato);
        }

        [Test]
        public void DefaultValues_Elasticity_IsOne()
        {
            var data = new TweenStepData();
            Assert.AreEqual(1f, data.Elasticity);
        }

        #endregion

        #region 路径动画值组

        [Test]
        public void DefaultValues_PathWaypoints_NotNull()
        {
            var data = new TweenStepData();
            Assert.IsNotNull(data.PathWaypoints);
        }

        [Test]
        public void DefaultValues_PathWaypoints_HasTwoElements()
        {
            var data = new TweenStepData();
            Assert.AreEqual(2, data.PathWaypoints.Length);
        }

        [Test]
        public void DefaultValues_PathType_IsZero()
        {
            var data = new TweenStepData();
            Assert.AreEqual(0, data.PathType);
        }

        [Test]
        public void DefaultValues_PathMode_IsZero()
        {
            var data = new TweenStepData();
            Assert.AreEqual(0, data.PathMode);
        }

        [Test]
        public void DefaultValues_PathResolution_IsTen()
        {
            var data = new TweenStepData();
            Assert.AreEqual(10, data.PathResolution);
        }

        #endregion

        #region 执行模式

        [Test]
        public void DefaultValues_ExecutionMode_IsAppend()
        {
            var data = new TweenStepData();
            Assert.AreEqual(ExecutionMode.Append, data.ExecutionMode);
        }

        [Test]
        public void DefaultValues_InsertTime_IsZero()
        {
            var data = new TweenStepData();
            Assert.AreEqual(0f, data.InsertTime);
        }

        #endregion

        #region 回调

        [Test]
        public void DefaultValues_OnComplete_NotNull()
        {
            var data = new TweenStepData();
            Assert.IsNotNull(data.OnComplete);
        }

        #endregion
    }
}
