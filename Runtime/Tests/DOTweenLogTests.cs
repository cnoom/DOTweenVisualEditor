using NUnit.Framework;

namespace CNoom.DOTweenVisual.Tests
{
    /// <summary>
    /// DOTweenLog 日志系统测试
    /// 测试日志级别设置和级别顺序
    /// </summary>
    public class DOTweenLogTests
    {
        [SetUp]
        public void SetUp()
        {
            // 每次测试前重置为默认级别
            DOTweenLog.SetLogLevel(DOTweenLog.LogLevel.Debug);
        }

        #region 默认值

        [Test]
        public void DefaultLevel_IsDebug()
        {
            Assert.AreEqual(DOTweenLog.LogLevel.Debug, DOTweenLog.CurrentLevel);
        }

        #endregion

        #region SetLogLevel

        [Test]
        public void SetLogLevel_Info_LevelIsInfo()
        {
            DOTweenLog.SetLogLevel(DOTweenLog.LogLevel.Info);
            Assert.AreEqual(DOTweenLog.LogLevel.Info, DOTweenLog.CurrentLevel);
        }

        [Test]
        public void SetLogLevel_None_LevelIsNone()
        {
            DOTweenLog.SetLogLevel(DOTweenLog.LogLevel.None);
            Assert.AreEqual(DOTweenLog.LogLevel.None, DOTweenLog.CurrentLevel);
        }

        [Test]
        public void SetLogLevel_CanBeResetToDebug()
        {
            DOTweenLog.SetLogLevel(DOTweenLog.LogLevel.None);
            DOTweenLog.SetLogLevel(DOTweenLog.LogLevel.Debug);
            Assert.AreEqual(DOTweenLog.LogLevel.Debug, DOTweenLog.CurrentLevel);
        }

        [Test]
        public void SetLogLevel_Warning_LevelIsWarning()
        {
            DOTweenLog.SetLogLevel(DOTweenLog.LogLevel.Warning);
            Assert.AreEqual(DOTweenLog.LogLevel.Warning, DOTweenLog.CurrentLevel);
        }

        [Test]
        public void SetLogLevel_Error_LevelIsError()
        {
            DOTweenLog.SetLogLevel(DOTweenLog.LogLevel.Error);
            Assert.AreEqual(DOTweenLog.LogLevel.Error, DOTweenLog.CurrentLevel);
        }

        #endregion

        #region 日志级别顺序

        [Test]
        public void LogLevel_Order_DebugLessThanInfo()
        {
            Assert.Less(DOTweenLog.LogLevel.Debug, DOTweenLog.LogLevel.Info);
        }

        [Test]
        public void LogLevel_Order_InfoLessThanWarning()
        {
            Assert.Less(DOTweenLog.LogLevel.Info, DOTweenLog.LogLevel.Warning);
        }

        [Test]
        public void LogLevel_Order_WarningLessThanError()
        {
            Assert.Less(DOTweenLog.LogLevel.Warning, DOTweenLog.LogLevel.Error);
        }

        [Test]
        public void LogLevel_Order_ErrorLessThanNone()
        {
            Assert.Less(DOTweenLog.LogLevel.Error, DOTweenLog.LogLevel.None);
        }

        #endregion
    }
}
