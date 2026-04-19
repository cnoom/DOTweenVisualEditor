namespace CNoom.DOTweenVisual.Data
{
    /// <summary>
    /// 移动动画的坐标空间
    /// </summary>
    public enum MoveSpace
    {
        /// <summary>世界坐标</summary>
        World,
        /// <summary>本地坐标</summary>
        Local
    }

    /// <summary>
    /// 旋转动画的坐标空间
    /// </summary>
    public enum RotateSpace
    {
        /// <summary>世界旋转</summary>
        World,
        /// <summary>本地旋转</summary>
        Local
    }

    /// <summary>
    /// 冲击动画的属性目标
    /// </summary>
    public enum PunchTarget
    {
        /// <summary>冲击位置</summary>
        Position,
        /// <summary>冲击旋转</summary>
        Rotation,
        /// <summary>冲击缩放</summary>
        Scale
    }

    /// <summary>
    /// 震动动画的属性目标
    /// </summary>
    public enum ShakeTarget
    {
        /// <summary>震动位置</summary>
        Position,
        /// <summary>震动旋转</summary>
        Rotation,
        /// <summary>震动缩放</summary>
        Scale
    }
}
