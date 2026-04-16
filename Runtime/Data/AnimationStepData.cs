using System;
using UnityEngine;

namespace CNoom.DOTweenVisual.Data
{
    /// <summary>
    /// 动画步骤类型标识
    /// </summary>
    public enum AnimationStepType
    {
        None = 0,
        Move = 1,
        Rotation = 2,
        Scale = 3
    }

    /// <summary>
    /// 动画步骤数据基类（可序列化）
    /// 使用类型标记 + 字段方式实现多态序列化
    /// </summary>
    [Serializable]
    public class AnimationStepData
    {
        public AnimationStepType StepType = AnimationStepType.None;

        // ========== Move 参数 ==========
        public MoveMode MoveMode = MoveMode.Absolute;
        public Vector3 TargetPosition;
        public Transform TargetTransform;

        // ========== Rotation 参数 ==========
        public RotationMode RotationMode = RotationMode.Absolute;
        public Vector3 TargetEulerAngles;
        public Quaternion TargetRotation;

        // ========== Scale 参数 ==========
        public ScaleMode ScaleMode = ScaleMode.Absolute;
        public Vector3 TargetScale = Vector3.one;

        // ========== 通用参数 ==========
        public float Duration = 1f;
        public EasingData Easing = new();
        public int Loops = 1;
        public LoopType LoopType = LoopType.Restart;
    }

    /// <summary>
    /// 移动模式
    /// </summary>
    public enum MoveMode
    {
        /// <summary>绝对位置</summary>
        Absolute,
        /// <summary>相对偏移</summary>
        Relative,
        /// <summary>跟随目标 Transform</summary>
        Follow
    }

    /// <summary>
    /// 旋转模式
    /// </summary>
    public enum RotationMode
    {
        /// <summary>绝对旋转（欧拉角）</summary>
        Absolute,
        /// <summary>相对旋转</summary>
        Relative,
        /// <summary>目标旋转（四元数）</summary>
        TargetRotation
    }

    /// <summary>
    /// 缩放模式
    /// </summary>
    public enum ScaleMode
    {
        /// <summary>绝对缩放</summary>
        Absolute,
        /// <summary>相对缩放</summary>
        Relative
    }
}
