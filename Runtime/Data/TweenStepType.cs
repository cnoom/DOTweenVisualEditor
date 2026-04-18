namespace CNoom.DOTweenVisual.Data
{
    /// <summary>
    /// 动画步骤类型
    /// </summary>
    public enum TweenStepType
    {
        // --- Transform ---
        /// <summary>移动动画</summary>
        Move,
        /// <summary>旋转动画（四元数插值）</summary>
        Rotate,
        /// <summary>缩放动画</summary>
        Scale,

        // --- 视觉 ---
        /// <summary>颜色动画</summary>
        Color,
        /// <summary>透明度动画</summary>
        Fade,

        // --- UI ---
        /// <summary>UI 锚点移动动画</summary>
        AnchorMove,
        /// <summary>UI 尺寸动画</summary>
        SizeDelta,

        // --- 特效 ---
        /// <summary>跳跃移动动画</summary>
        Jump,
        /// <summary>冲击弹性动画</summary>
        Punch,
        /// <summary>震动动画</summary>
        Shake,
        /// <summary>Image 填充量动画</summary>
        FillAmount,

        // --- 路径 ---
        /// <summary>路径移动动画</summary>
        DOPath,

        // --- 流程控制 ---
        /// <summary>延迟等待</summary>
        Delay,
        /// <summary>回调调用</summary>
        Callback
    }
}
