using UnityEngine;
using UnityEngine.UI;

namespace CNoom.DOTweenVisual.Data
{
    /// <summary>
    /// 动画步骤组件需求校验
    /// 定义每种 TweenStepType 所需的组件能力，并提供统一的校验方法
    /// 值访问操作（颜色/透明度读写、动画创建）请使用 TweenValueHelper
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

        #region 能力检测

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
        /// 检测物体是否具有 RectTransform
        /// </summary>
        public static bool HasRectTransform(Transform target)
        {
            return target != null && TweenValueHelper.TryGetRectTransform(target, out _);
        }

        #endregion
    }
}
