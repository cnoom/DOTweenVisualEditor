using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace CNoom.DOTweenVisual.Data
{
    /// <summary>
    /// 动画步骤组件需求配置
    /// 定义每种 TweenStepType 所需的组件能力，并提供统一的校验和工具方法
    /// </summary>
    public static class TweenStepRequirement
    {
        #region 校验方法

        /// <summary>
        /// 校验目标物体是否满足指定动画类型的组件需求
        /// </summary>
        /// <param name="target">目标 Transform</param>
        /// <param name="type">动画类型</param>
        /// <param name="errorMessage">不满足时的错误信息</param>
        /// <returns>是否满足需求</returns>
        public static bool Validate(Transform target, TweenStepType type, out string errorMessage)
        {
            errorMessage = null;

            if (target == null)
            {
                errorMessage = "目标物体为空";
                return false;
            }

            switch (type)
            {
                case TweenStepType.Move:
                case TweenStepType.Rotate:
                case TweenStepType.Scale:
                case TweenStepType.Jump:
                case TweenStepType.Punch:
                case TweenStepType.Shake:
                    // Transform 类型无额外要求
                    return true;

                case TweenStepType.Color:
                    if (!HasColorTarget(target))
                    {
                        errorMessage = "该物体不包含可着色组件（需要 Graphic / Renderer / SpriteRenderer）";
                        return false;
                    }
                    return true;

                case TweenStepType.Fade:
                    if (!HasFadeTarget(target))
                    {
                        errorMessage = "该物体不包含可透明组件（需要 CanvasGroup / Graphic / Renderer / SpriteRenderer）";
                        return false;
                    }
                    return true;

                case TweenStepType.AnchorMove:
                case TweenStepType.SizeDelta:
                    if (!HasRectTransform(target))
                    {
                        errorMessage = "该物体不是 UI 物体（需要 RectTransform）";
                        return false;
                    }
                    return true;

                case TweenStepType.FillAmount:
                    if (target.GetComponent<Image>() == null)
                    {
                        errorMessage = "该物体不包含 Image 组件";
                        return false;
                    }
                    return true;

                case TweenStepType.Delay:
                case TweenStepType.Callback:
                    // 无目标物体需求
                    return true;

                default:
                    return true;
            }
        }

        /// <summary>
        /// 获取指定动画类型的需求描述（用于 Editor 提示）
        /// </summary>
        public static string GetRequirementDescription(TweenStepType type)
        {
            return type switch
            {
                TweenStepType.Color => "需要可着色组件：Graphic / Renderer / SpriteRenderer",
                TweenStepType.Fade => "需要可透明组件：CanvasGroup / Graphic / Renderer / SpriteRenderer",
                TweenStepType.AnchorMove or TweenStepType.SizeDelta => "需要 UI 物体（RectTransform）",
                TweenStepType.FillAmount => "需要 Image 组件",
                _ => null
            };
        }

        #endregion

        #region 颜色能力检测

        /// <summary>
        /// 检测物体是否具有颜色控制能力
        /// </summary>
        public static bool HasColorTarget(Transform target)
        {
            if (target == null) return false;

            if (target.GetComponent<Graphic>() != null) return true;
            if (target.GetComponent<Renderer>() != null) return true;
            if (target.GetComponent<SpriteRenderer>() != null) return true;

#if DOTWEEN_TMP || TMP_PRESENT
            if (target.GetComponent<TMPro.TMP_Text>() != null) return true;
#endif

            return false;
        }

        /// <summary>
        /// 尝试获取物体当前颜色
        /// </summary>
        public static bool TryGetColor(Transform target, out Color color)
        {
            color = Color.white;

            // 优先级：Graphic > SpriteRenderer > Renderer (Material) > TMP
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
            if (renderer != null && renderer.sharedMaterial != null)
            {
                color = renderer.sharedMaterial.color;
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
            if (renderer != null && renderer.sharedMaterial != null)
            {
                renderer.sharedMaterial.color = color;
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
            // Graphic（Image, Text, RawImage 等）
            var graphic = target.GetComponent<Graphic>();
            if (graphic != null)
            {
                return graphic.DOColor(endColor, duration);
            }

            // SpriteRenderer
            var spriteRenderer = target.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                return spriteRenderer.DOColor(endColor, duration);
            }

            // Renderer（Material）
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                return renderer.material.DOColor(endColor, duration);
            }

#if DOTWEEN_TMP || TMP_PRESENT
            // TextMeshPro
            var tmpText = target.GetComponent<TMPro.TMP_Text>();
            if (tmpText != null)
            {
                return tmpText.DOColor(endColor, duration);
            }
#endif

            return null;
        }

        #endregion

        #region 透明度能力检测

        /// <summary>
        /// 检测物体是否具有透明度控制能力
        /// </summary>
        public static bool HasFadeTarget(Transform target)
        {
            if (target == null) return false;

            if (target.GetComponent<CanvasGroup>() != null) return true;
            if (target.GetComponent<Graphic>() != null) return true;
            if (target.GetComponent<Renderer>() != null) return true;
            if (target.GetComponent<SpriteRenderer>() != null) return true;

#if DOTWEEN_TMP || TMP_PRESENT
            if (target.GetComponent<TMPro.TMP_Text>() != null) return true;
#endif

            return false;
        }

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
            if (renderer != null && renderer.sharedMaterial != null)
            {
                alpha = renderer.sharedMaterial.color.a;
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
            if (renderer != null && renderer.sharedMaterial != null)
            {
                var c = renderer.sharedMaterial.color;
                c.a = alpha;
                renderer.sharedMaterial.color = c;
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
            // CanvasGroup 优先（UI 整体透明）
            var canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                return canvasGroup.DOFade(endAlpha, duration);
            }

            // Graphic
            var graphic = target.GetComponent<Graphic>();
            if (graphic != null)
            {
                return graphic.DOFade(endAlpha, duration);
            }

            // SpriteRenderer
            var spriteRenderer = target.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                return spriteRenderer.DOFade(endAlpha, duration);
            }

            // Renderer（Material）- 动画需要使用 material 实例
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                return renderer.material.DOFade(endAlpha, duration);
            }

#if DOTWEEN_TMP || TMP_PRESENT
            // TextMeshPro
            var tmpText = target.GetComponent<TMPro.TMP_Text>();
            if (tmpText != null)
            {
                return tmpText.DOFade(endAlpha, duration);
            }
#endif

            return null;
        }

        #endregion

        #region RectTransform 能力检测

        /// <summary>
        /// 检测物体是否具有 RectTransform
        /// </summary>
        public static bool HasRectTransform(Transform target)
        {
            if (target == null) return false;
            return target is RectTransform || target.GetComponent<RectTransform>() != null;
        }

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
    }
}
