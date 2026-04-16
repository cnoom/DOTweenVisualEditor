using UnityEngine;

namespace CNoom.DOTweenVisual.Adapter
{
    /// <summary>
    /// DOTween 适配器接口
    /// 抽象 DOTween API，支持 Free/Pro 版本切换
    /// </summary>
    public interface IDOTweenAdapter
    {
        /// <summary>
        /// DOTween 是否已初始化
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 是否为 Pro 版本
        /// </summary>
        bool IsProVersion { get; }

        /// <summary>
        /// 初始化 DOTween
        /// </summary>
        void Initialize();

        /// <summary>
        /// 创建移动 Tween
        /// </summary>
        TweenerAdapter CreateMoveTween(Transform target, Vector3 endValue, float duration);

        /// <summary>
        /// 创建相对移动 Tween
        /// </summary>
        TweenerAdapter CreateMoveTweenRelative(Transform target, Vector3 delta, float duration);

        /// <summary>
        /// 创建旋转 Tween（欧拉角）
        /// </summary>
        TweenerAdapter CreateRotateTween(Transform target, Vector3 endValue, float duration);

        /// <summary>
        /// 创建缩放 Tween
        /// </summary>
        TweenerAdapter CreateScaleTween(Transform target, Vector3 endValue, float duration);

        /// <summary>
        /// 创建序列
        /// </summary>
        SequenceAdapter CreateSequence();

        /// <summary>
        /// 清理所有 Tween
        /// </summary>
        void ClearAll();
    }

    /// <summary>
    /// Tweener 适配器接口
    /// </summary>
    public interface TweenerAdapter
    {
        void SetEase(Ease ease);
        void SetEase(AnimationCurve curve);
        void SetLoops(int loops, LoopType loopType);
        void SetDelay(float delay);
        void SetRecyclable(bool recyclable);
        void OnComplete(System.Action callback);
        void Play();
        void Pause();
        void Kill(bool complete = false);
        bool IsPlaying();
        bool IsComplete();
    }

    /// <summary>
    /// Sequence 适配器接口
    /// </summary>
    public interface SequenceAdapter
    {
        void Append(TweenerAdapter tweener);
        void AppendInterval(float interval);
        void Insert(float time, TweenerAdapter tweener);
        void Join(TweenerAdapter tweener);
        void SetLoops(int loops, LoopType loopType);
        void SetRecyclable(bool recyclable);
        void OnComplete(System.Action callback);
        void Play();
        void Pause();
        void Kill(bool complete = false);
        bool IsPlaying();
        bool IsComplete();
    }
}
