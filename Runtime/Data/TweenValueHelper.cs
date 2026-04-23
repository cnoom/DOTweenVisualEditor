using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace CNoom.DOTweenVisual.Data
{
    /// <summary>
    /// Tween 值访问工具
    /// 提供颜色、透明度、RectTransform 等属性的安全读写和动画创建
    /// </summary>
    public static class TweenValueHelper
    {
        #region RectTransform

        /// <summary>
        /// 尝试获取 RectTransform
        /// </summary>
        public static bool TryGetRectTransform(Transform target, out RectTransform rectTransform)
        {
            rectTransform = target as RectTransform;
            if (rectTransform == null) rectTransform = target.GetComponent<RectTransform>();
            return rectTransform != null;
        }

        #endregion

        #region 颜色操作

        /// <summary>
        /// 尝试获取物体当前颜色
        /// </summary>
        public static bool TryGetColor(Transform target, out Color color)
        {
            color = Color.white;

            var graphic = target.GetComponent<Graphic>();
            if (graphic != null)
            {
                color = graphic.color;
                return true;
            }

            var spriteRenderer = target.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                color = spriteRenderer.color;
                return true;
            }

            var renderer = target.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                color = renderer.material.color;
                return true;
            }

#if DOTWEEN_TMP || TMP_PRESENT
            var tmpText = target.GetComponent<TMPro.TMP_Text>();
            if (tmpText != null)
            {
                color = tmpText.color;
                return true;
            }
#endif

            return false;
        }

        /// <summary>
        /// 尝试设置物体颜色
        /// </summary>
        public static bool TrySetColor(Transform target, Color color)
        {
            var graphic = target.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.color = color;
                return true;
            }

            var spriteRenderer = target.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
                return true;
            }

            var renderer = target.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = color;
                return true;
            }

#if DOTWEEN_TMP || TMP_PRESENT
            var tmpText = target.GetComponent<TMPro.TMP_Text>();
            if (tmpText != null)
            {
                tmpText.color = color;
                return true;
            }
#endif

            return false;
        }

        /// <summary>
        /// 创建颜色动画 Tweener
        /// 自动根据物体上的组件类型选择合适的 DOTween 方法
        /// </summary>
        public static Tweener CreateColorTween(Transform target, Color endColor, float duration)
        {
            var graphic = target.GetComponent<Graphic>();
            if (graphic != null)
            {
                return graphic.DOColor(endColor, duration);
            }

            var spriteRenderer = target.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                return spriteRenderer.DOColor(endColor, duration);
            }

            // Renderer（Material）- 使用 material 实例，避免污染共享材质
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                return renderer.material.DOColor(endColor, duration);
            }

#if DOTWEEN_TMP || TMP_PRESENT
            var tmpText = target.GetComponent<TMPro.TMP_Text>();
            if (tmpText != null)
            {
                return tmpText.DOColor(endColor, duration);
            }
#endif

            return null;
        }

        #endregion

        #region 透明度操作

        /// <summary>
        /// 尝试获取物体当前透明度
        /// </summary>
        public static bool TryGetAlpha(Transform target, out float alpha)
        {
            alpha = 1f;

            var canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                alpha = canvasGroup.alpha;
                return true;
            }

            var graphic = target.GetComponent<Graphic>();
            if (graphic != null)
            {
                alpha = graphic.color.a;
                return true;
            }

            var spriteRenderer = target.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                alpha = spriteRenderer.color.a;
                return true;
            }

            var renderer = target.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                alpha = renderer.material.color.a;
                return true;
            }

#if DOTWEEN_TMP || TMP_PRESENT
            var tmpText = target.GetComponent<TMPro.TMP_Text>();
            if (tmpText != null)
            {
                alpha = tmpText.color.a;
                return true;
            }
#endif

            return false;
        }

        /// <summary>
        /// 尝试设置物体透明度
        /// </summary>
        public static bool TrySetAlpha(Transform target, float alpha)
        {
            var canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
                return true;
            }

            var graphic = target.GetComponent<Graphic>();
            if (graphic != null)
            {
                var c = graphic.color;
                c.a = alpha;
                graphic.color = c;
                return true;
            }

            var spriteRenderer = target.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                var c = spriteRenderer.color;
                c.a = alpha;
                spriteRenderer.color = c;
                return true;
            }

            var renderer = target.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                var c = renderer.material.color;
                c.a = alpha;
                renderer.material.color = c;
                return true;
            }

#if DOTWEEN_TMP || TMP_PRESENT
            var tmpText = target.GetComponent<TMPro.TMP_Text>();
            if (tmpText != null)
            {
                var c = tmpText.color;
                c.a = alpha;
                tmpText.color = c;
                return true;
            }
#endif

            return false;
        }

        /// <summary>
        /// 创建透明度动画 Tweener
        /// 自动根据物体上的组件类型选择合适的 DOTween 方法
        /// </summary>
        public static Tweener CreateFadeTween(Transform target, float endAlpha, float duration)
        {
            var canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                return canvasGroup.DOFade(endAlpha, duration);
            }

            var graphic = target.GetComponent<Graphic>();
            if (graphic != null)
            {
                return graphic.DOFade(endAlpha, duration);
            }

            var spriteRenderer = target.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                return spriteRenderer.DOFade(endAlpha, duration);
            }

            // Renderer（Material）- 使用 material 实例，避免污染共享材质
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                return renderer.material.DOFade(endAlpha, duration);
            }

#if DOTWEEN_TMP || TMP_PRESENT
            var tmpText = target.GetComponent<TMPro.TMP_Text>();
            if (tmpText != null)
            {
                return tmpText.DOFade(endAlpha, duration);
            }
#endif

            return null;
        }

        #endregion
    }
}
