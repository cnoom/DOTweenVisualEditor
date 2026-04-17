namespace CNoom.DOTweenVisual.Data
{
    /// <summary>
    /// Transform 目标类型
    /// </summary>
    public enum TransformTarget
    {
        /// <summary>世界坐标位置</summary>
        Position,
        /// <summary>本地坐标位置</summary>
        LocalPosition,
        /// <summary>世界旋转（四元数插值，编辑器以欧拉角显示）</summary>
        Rotation,
        /// <summary>本地旋转（四元数插值，编辑器以欧拉角显示）</summary>
        LocalRotation,
        /// <summary>缩放</summary>
        Scale
    }
}
