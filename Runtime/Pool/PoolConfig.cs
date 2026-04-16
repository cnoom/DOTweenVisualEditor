using System;

namespace CNoom.DOTweenVisual.Pool
{
    /// <summary>
    /// 对象池配置
    /// </summary>
    [Serializable]
    public class PoolConfig
    {
        /// <summary>初始容量</summary>
        public int InitialCapacity = 16;

        /// <summary>最大容量</summary>
        public int MaxCapacity = 256;

        /// <summary>默认配置</summary>
        public static PoolConfig Default => new PoolConfig();

        /// <summary>高性能配置</summary>
        public static PoolConfig HighPerformance => new PoolConfig
        {
            InitialCapacity = 64,
            MaxCapacity = 1024
        };

        /// <summary>低内存配置</summary>
        public static PoolConfig LowMemory => new PoolConfig
        {
            InitialCapacity = 4,
            MaxCapacity = 32
        };
    }
}
