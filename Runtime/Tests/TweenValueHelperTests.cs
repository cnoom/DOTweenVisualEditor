using NUnit.Framework;
using UnityEngine;
using CNoom.DOTweenVisual.Data;

namespace CNoom.DOTweenVisual.Tests
{
    /// <summary>
    /// TweenValueHelper 值访问工具测试
    /// 测试 RectTransform/Color/Alpha 的安全读写操作
    /// </summary>
    public class TweenValueHelperTests
    {
        private GameObject _gameObject;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("TestTarget");
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
            }
        }

        #region TryGetRectTransform

        [Test]
        public void TryGetRectTransform_PlainTransform_ReturnsFalse()
        {
            var result = TweenValueHelper.TryGetRectTransform(
                _gameObject.transform, out var rectTransform);
            Assert.IsFalse(result);
            Assert.IsNull(rectTransform);
        }

        [Test]
        public void TryGetRectTransform_WithRectTransform_ReturnsTrue()
        {
            // Canvas 下的子物体自动拥有 RectTransform
            var canvas = new GameObject("Canvas");
            canvas.AddComponent<Canvas>();
            var uiObj = new GameObject("UIElement");
            uiObj.transform.SetParent(canvas.transform);
            uiObj.AddComponent<UnityEngine.UI.Image>();

            var result = TweenValueHelper.TryGetRectTransform(
                uiObj.transform, out var rectTransform);
            Assert.IsTrue(result);
            Assert.IsNotNull(rectTransform);

            Object.DestroyImmediate(canvas);
        }

        #endregion

        #region TryGetColor / TrySetColor

        [Test]
        public void TryGetColor_PlainTransform_ReturnsFalse()
        {
            var result = TweenValueHelper.TryGetColor(_gameObject.transform, out _);
            Assert.IsFalse(result);
        }

        [Test]
        public void TryGetColor_WithImage_ReturnsTrue()
        {
            var image = _gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.red;

            var result = TweenValueHelper.TryGetColor(_gameObject.transform, out var color);
            Assert.IsTrue(result);
            Assert.AreEqual(Color.red, color);
        }

        [Test]
        public void TryGetColor_WithSpriteRenderer_ReturnsTrue()
        {
            var sr = _gameObject.AddComponent<SpriteRenderer>();
            sr.color = Color.blue;

            var result = TweenValueHelper.TryGetColor(_gameObject.transform, out var color);
            Assert.IsTrue(result);
            Assert.AreEqual(Color.blue, color);
        }

        [Test]
        public void TrySetColor_WithImage_SetsColor()
        {
            var image = _gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.white;

            var result = TweenValueHelper.TrySetColor(_gameObject.transform, Color.green);
            Assert.IsTrue(result);
            Assert.AreEqual(Color.green, image.color);
        }

        [Test]
        public void TrySetColor_WithSpriteRenderer_SetsColor()
        {
            var sr = _gameObject.AddComponent<SpriteRenderer>();
            sr.color = Color.white;

            var result = TweenValueHelper.TrySetColor(_gameObject.transform, Color.red);
            Assert.IsTrue(result);
            Assert.AreEqual(Color.red, sr.color);
        }

        [Test]
        public void TrySetColor_PlainTransform_ReturnsFalse()
        {
            var result = TweenValueHelper.TrySetColor(_gameObject.transform, Color.red);
            Assert.IsFalse(result);
        }

        #endregion

        #region TryGetAlpha / TrySetAlpha

        [Test]
        public void TryGetAlpha_PlainTransform_ReturnsFalse()
        {
            var result = TweenValueHelper.TryGetAlpha(_gameObject.transform, out _);
            Assert.IsFalse(result);
        }

        [Test]
        public void TryGetAlpha_WithCanvasGroup_ReturnsTrue()
        {
            var cg = _gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0.5f;

            var result = TweenValueHelper.TryGetAlpha(_gameObject.transform, out var alpha);
            Assert.IsTrue(result);
            Assert.AreEqual(0.5f, alpha, 0.001f);
        }

        [Test]
        public void TryGetAlpha_WithImage_ReturnsAlphaFromColor()
        {
            var image = _gameObject.AddComponent<UnityEngine.UI.Image>();
            var c = Color.white;
            c.a = 0.3f;
            image.color = c;

            var result = TweenValueHelper.TryGetAlpha(_gameObject.transform, out var alpha);
            Assert.IsTrue(result);
            Assert.AreEqual(0.3f, alpha, 0.001f);
        }

        [Test]
        public void TryGetAlpha_WithSpriteRenderer_ReturnsAlphaFromColor()
        {
            var sr = _gameObject.AddComponent<SpriteRenderer>();
            var c = Color.white;
            c.a = 0.7f;
            sr.color = c;

            var result = TweenValueHelper.TryGetAlpha(_gameObject.transform, out var alpha);
            Assert.IsTrue(result);
            Assert.AreEqual(0.7f, alpha, 0.001f);
        }

        [Test]
        public void TrySetAlpha_WithCanvasGroup_SetsAlpha()
        {
            var cg = _gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            var result = TweenValueHelper.TrySetAlpha(_gameObject.transform, 0.2f);
            Assert.IsTrue(result);
            Assert.AreEqual(0.2f, cg.alpha, 0.001f);
        }

        [Test]
        public void TrySetAlpha_WithImage_SetsAlphaInColor()
        {
            var image = _gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.white;

            var result = TweenValueHelper.TrySetAlpha(_gameObject.transform, 0.5f);
            Assert.IsTrue(result);
            Assert.AreEqual(0.5f, image.color.a, 0.001f);
        }

        [Test]
        public void TrySetAlpha_WithSpriteRenderer_SetsAlphaInColor()
        {
            var sr = _gameObject.AddComponent<SpriteRenderer>();
            sr.color = Color.white;

            var result = TweenValueHelper.TrySetAlpha(_gameObject.transform, 0.8f);
            Assert.IsTrue(result);
            Assert.AreEqual(0.8f, sr.color.a, 0.001f);
        }

        [Test]
        public void TrySetAlpha_PlainTransform_ReturnsFalse()
        {
            var result = TweenValueHelper.TrySetAlpha(_gameObject.transform, 0.5f);
            Assert.IsFalse(result);
        }

        #endregion

        #region 优先级顺序验证

        [Test]
        public void TryGetAlpha_CanvasGroupTakesPriorityOverImage()
        {
            // CanvasGroup 应优先于 Image
            _gameObject.AddComponent<UnityEngine.UI.Image>();
            var cg = _gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0.1f;

            TweenValueHelper.TryGetAlpha(_gameObject.transform, out var alpha);
            Assert.AreEqual(0.1f, alpha, 0.001f,
                "CanvasGroup 的 alpha 应优先于 Image 的 color.a");
        }

        [Test]
        public void TryGetAlpha_ImageTakesPriorityOverSpriteRenderer()
        {
            // Image (Graphic) 应优先于 SpriteRenderer
            var sr = _gameObject.AddComponent<SpriteRenderer>();
            sr.color = new Color(1f, 1f, 1f, 0.2f);
            var image = _gameObject.AddComponent<UnityEngine.UI.Image>();
            var c = Color.white;
            c.a = 0.9f;
            image.color = c;

            TweenValueHelper.TryGetAlpha(_gameObject.transform, out var alpha);
            Assert.AreEqual(0.9f, alpha, 0.001f,
                "Image 的 alpha 应优先于 SpriteRenderer");
        }

        #endregion
    }
}
