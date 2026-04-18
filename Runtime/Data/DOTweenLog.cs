using System.Diagnostics;
using UnityEngine;

namespace CNoom.DOTweenVisual
{
    /// <summary>
    /// DOTween Visual 日志系统
    /// 统一日志输出格式，支持日志级别控制
    /// Debug 级别日志仅在编辑器环境下编译，发布版本自动移除
    /// </summary>
    public static class DOTweenLog
    {
        #region 枚举

        /// <summary>
        /// 日志级别
        /// </summary>
        public enum LogLevel
        {
            /// <summary>调试信息，仅编辑器可用</summary>
            Debug,
            /// <summary>一般信息</summary>
            Info,
            /// <summary>警告信息</summary>
            Warning,
            /// <summary>错误信息</summary>
            Error,
            /// <summary>关闭所有日志</summary>
            None
        }

        #endregion

        #region 常量

        private const string Tag = "DOTweenVisual";

        #endregion

        #region 静态字段

        private static LogLevel _currentLevel = LogLevel.Debug;

        #endregion

        #region 属性

        /// <summary>
        /// 当前日志级别
        /// </summary>
        public static LogLevel CurrentLevel => _currentLevel;

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置日志级别
        /// </summary>
        /// <param name="level">目标日志级别</param>
        public static void SetLogLevel(LogLevel level)
        {
            _currentLevel = level;
        }

        /// <summary>
        /// 输出 Debug 级别日志
        /// 仅在编辑器环境下编译，发布版本自动移除调用
        /// </summary>
        /// <param name="message">日志内容</param>
        [Conditional("UNITY_EDITOR")]
        public static void Debug(string message)
        {
            if (_currentLevel > LogLevel.Debug) return;
            UnityEngine.Debug.Log($"[{Tag}] {message}");
        }

        /// <summary>
        /// 输出 Debug 级别格式化日志
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void DebugFormat(string format, params object[] args)
        {
            if (_currentLevel > LogLevel.Debug) return;
            UnityEngine.Debug.LogFormat($"[{Tag}] " + format, args);
        }

        /// <summary>
        /// 输出 Info 级别日志
        /// </summary>
        /// <param name="message">日志内容</param>
        public static void Info(string message)
        {
            if (_currentLevel > LogLevel.Info) return;
            UnityEngine.Debug.Log($"[{Tag}] {message}");
        }

        /// <summary>
        /// 输出 Info 级别格式化日志
        /// </summary>
        public static void InfoFormat(string format, params object[] args)
        {
            if (_currentLevel > LogLevel.Info) return;
            UnityEngine.Debug.LogFormat($"[{Tag}] " + format, args);
        }

        /// <summary>
        /// 输出 Warning 级别日志
        /// </summary>
        /// <param name="message">日志内容</param>
        public static void Warning(string message)
        {
            if (_currentLevel > LogLevel.Warning) return;
            UnityEngine.Debug.LogWarning($"[{Tag}] {message}");
        }

        /// <summary>
        /// 输出 Warning 级别格式化日志
        /// </summary>
        public static void WarningFormat(string format, params object[] args)
        {
            if (_currentLevel > LogLevel.Warning) return;
            UnityEngine.Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace,
                null, $"[{Tag}] " + format, args);
        }

        /// <summary>
        /// 输出 Error 级别日志
        /// </summary>
        /// <param name="message">日志内容</param>
        public static void Error(string message)
        {
            if (_currentLevel > LogLevel.Error) return;
            UnityEngine.Debug.LogError($"[{Tag}] {message}");
        }

        /// <summary>
        /// 输出 Error 级别格式化日志
        /// </summary>
        public static void ErrorFormat(string format, params object[] args)
        {
            if (_currentLevel > LogLevel.Error) return;
            UnityEngine.Debug.LogFormat(LogType.Error, LogOption.NoStacktrace,
                null, $"[{Tag}] " + format, args);
        }

        #endregion
    }
}
