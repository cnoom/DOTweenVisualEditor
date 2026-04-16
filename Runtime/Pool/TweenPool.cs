using System;
using System.Collections.Generic;
using UnityEngine;

namespace CNoom.DOTweenVisual.Pool
{
    /// <summary>
    /// Tween 对象池（全局单例）
    /// 管理 Tweener 和 Sequence 的创建与回收
    /// </summary>
    public class TweenPool : IDisposable
    {
        private static TweenPool _instance;
        public static TweenPool Instance => _instance ??= new TweenPool();

        private readonly Dictionary<Type, object> _pools = new();
        private readonly PoolConfig _config;

        private TweenPool()
        {
            _config = PoolConfig.Default;
        }

        public TweenPool(PoolConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// 从池中获取对象（懒加载）
        /// </summary>
        public T Get<T>(Func<T> factory) where T : class
        {
            var pool = GetOrCreatePool<T>();
            return pool.Get(factory);
        }

        /// <summary>
        /// 回收对象到池中
        /// </summary>
        public void Return<T>(T obj) where T : class
        {
            var pool = GetOrCreatePool<T>();
            pool.Return(obj);
        }

        /// <summary>
        /// 清空所有池
        /// </summary>
        public void Clear()
        {
            foreach (var pool in _pools.Values)
            {
                (pool as IDisposable)?.Dispose();
            }
            _pools.Clear();
        }

        public void Dispose()
        {
            Clear();
            _instance = null;
        }

        private ObjectPool<T> GetOrCreatePool<T>() where T : class
        {
            var type = typeof(T);
            if (!_pools.TryGetValue(type, out var pool))
            {
                pool = new ObjectPool<T>(_config.InitialCapacity, _config.MaxCapacity);
                _pools[type] = pool;
            }
            return (ObjectPool<T>)pool;
        }
    }

    /// <summary>
    /// 泛型对象池
    /// </summary>
    public class ObjectPool<T> : IDisposable where T : class
    {
        private readonly Stack<T> _pool;
        private readonly int _maxCapacity;
        private int _count;

        public ObjectPool(int initialCapacity, int maxCapacity)
        {
            _maxCapacity = maxCapacity;
            _pool = new Stack<T>(initialCapacity);
        }

        public T Get(Func<T> factory)
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }

            _count++;
            return factory();
        }

        public void Return(T obj)
        {
            if (_pool.Count < _maxCapacity)
            {
                _pool.Push(obj);
            }
            else
            {
                _count--;
            }
        }

        public void Clear()
        {
            _pool.Clear();
            _count = 0;
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
