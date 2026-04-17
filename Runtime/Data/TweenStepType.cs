namespace CNoom.DOTweenVisual.Data
{
    /// <summary>
    /// 动画步骤类型
    /// </summary>
    public enum TweenStepType
    {
        /// <summary>移动动画</summary>
        Move,
        /// <summary>旋转动画（四元数插值）</summary>
        Rotate,
        /// <summary>缩放动画</summary>
        Scale,
        /// <summary>颜色动画</summary>
        Color,
        /// <summary>透明度动画</summary>
        Fade,
        /// <summary>延迟等待</summary>
        Delay,
        /// <summary>回调调用</summary>
        Callback
    }
}
