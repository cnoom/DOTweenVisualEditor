#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CNoom.DOTweenVisual.Editor
{
    internal class HelpPopupWindow : EditorWindow
    {
        private void CreateGUI()
        {
            var styleSheet = DOTweenEditorStyle.FindStyleSheet();

            var root = rootVisualElement;
            root.style.paddingLeft = 12;
            root.style.paddingRight = 12;
            root.style.paddingTop = 8;
            root.style.paddingBottom = 8;

            // --- 颜色约定 ---
            AddSection(L10n.Tr("Help/Section_Colors"));
            AddColorItem("#4FC3F7", L10n.Tr("Help/Color_Sync"));
            AddColorItem("#EF5350", L10n.Tr("Help/Color_Delete"));
            AddColorItem("#66BB6A", L10n.Tr("Help/Color_Add"));

            AddSeparator();

            // --- 快捷键 ---
            AddSection(L10n.Tr("Help/Section_Shortcuts"));
            AddTextItem(L10n.Tr("Help/Shortcut_Copy"));
            AddTextItem(L10n.Tr("Help/Shortcut_Paste"));
            AddTextItem(L10n.Tr("Help/Shortcut_Delete"));

            AddSeparator();

            // --- 路径点规则 ---
            AddSection(L10n.Tr("Help/Section_Path"));
            AddTextItem(L10n.Tr("Help/Path_Linear"));
            AddTextItem(L10n.Tr("Help/Path_Bezier"));

            if (styleSheet != null)
                root.styleSheets.Add(styleSheet);
        }

        private void AddSection(string title)
        {
            var label = new Label(title);
            label.style.fontSize = 13;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 6;
            label.style.marginBottom = 4;
            label.style.color = new Color(0.9f, 0.9f, 0.9f);
            rootVisualElement.Add(label);
        }

        private void AddColorItem(string hexColor, string description)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 3;
            row.style.paddingLeft = 8;

            var swatch = new VisualElement();
            swatch.style.width = 14;
            swatch.style.height = 14;
            swatch.style.marginRight = 8;
            swatch.style.flexShrink = 0;
            swatch.style.backgroundColor = ColorFromString(hexColor);
            swatch.style.borderTopLeftRadius = 3;
            swatch.style.borderTopRightRadius = 3;
            swatch.style.borderBottomLeftRadius = 3;
            swatch.style.borderBottomRightRadius = 3;
            swatch.style.borderTopWidth = 1;
            swatch.style.borderBottomWidth = 1;
            swatch.style.borderLeftWidth = 1;
            swatch.style.borderRightWidth = 1;
            swatch.style.borderTopColor = new Color(1, 1, 1, 0.2f);
            swatch.style.borderBottomColor = new Color(1, 1, 1, 0.2f);
            swatch.style.borderLeftColor = new Color(1, 1, 1, 0.2f);
            swatch.style.borderRightColor = new Color(1, 1, 1, 0.2f);
            row.Add(swatch);

            var desc = new Label(description);
            desc.style.fontSize = 11;
            desc.style.color = new Color(0.8f, 0.8f, 0.8f);
            desc.style.flexGrow = 1;
            desc.style.flexWrap = Wrap.Wrap;
            row.Add(desc);

            rootVisualElement.Add(row);
        }

        private void AddTextItem(string text)
        {
            var label = new Label(text);
            label.style.fontSize = 11;
            label.style.color = new Color(0.75f, 0.75f, 0.75f);
            label.style.paddingLeft = 8;
            label.style.marginBottom = 2;
            rootVisualElement.Add(label);
        }

        private void AddSeparator()
        {
            var sep = new VisualElement();
            sep.style.height = 1;
            sep.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
            sep.style.marginTop = 8;
            sep.style.marginBottom = 4;
            rootVisualElement.Add(sep);
        }

        private static Color ColorFromString(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
#endif
