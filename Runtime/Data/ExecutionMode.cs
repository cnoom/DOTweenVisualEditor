namespace CNoom.DOTweenVisual.Data
{
    /// <summary>
    /// 动画执行模式
    /// </summary>
    public enum ExecutionMode
    {
        /// <summary>顺序追加到序列末尾</summary>
        Append,
        /// <summary>与上一个 Tween 同时执行</summary>
        Join,
        /// <summary>在指定时间点插入</summary>
        Insert
    }
}
