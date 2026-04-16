using UnityEngine;

namespace CNoom.DOTweenVisual.Adapter
{
    /// <summary>
    /// DOTween 兼容性检测
    /// </summary>
    public static class DOTweenCompatibility
    {
        private static bool? _isDOTweenAvailable;
        private static bool? _isProVersion;

        /// <summary>
        /// DOTween 是否可用
        /// </summary>
        public static bool IsDOTweenAvailable
        {
            get
            {
                if (_isDOTweenAvailable == null)
                {
                    _isDOTweenAvailable = CheckDOTweenAvailable();
                }
                return _isDOTweenAvailable.Value;
            }
        }

        /// <summary>
        /// 是否为 Pro 版本
        /// </summary>
        public static bool IsProVersion
        {
            get
            {
                if (_isProVersion == null)
                {
                    _isProVersion = CheckProVersion();
                }
                return _isProVersion.Value;
            }
        }

        private static bool CheckDOTweenAvailable()
        {
#if DOTWEEN
            return true;
#else
            // 运行时反射检测
            var dotweenType = System.Type.GetType("DG.Tweening.DOTween, DOTween");
            return dotweenType != null;
#endif
        }

        private static bool CheckProVersion()
        {
#if DOTWEEN_PRO
            return true;
#else
            if (!IsDOTweenAvailable) return false;

            // 反射检测 Pro 版本特性
            var visualModule = System.Type.GetType("DG.Tweening.DOTweenVisualManager, DOTween");
            return visualModule != null;
#endif
        }

        /// <summary>
        /// 获取 DOTween 版本信息
        /// </summary>
        public static string GetVersionInfo()
        {
            if (!IsDOTweenAvailable)
            {
                return "DOTween 未安装";
            }

            return IsProVersion ? "DOTween Pro" : "DOTween Free";
        }
    }
}
