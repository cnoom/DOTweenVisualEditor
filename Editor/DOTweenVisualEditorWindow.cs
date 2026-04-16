#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;
using CNoom.DOTweenVisual.Components;
using CNoom.DOTweenVisual.Data;

namespace CNoom.DOTweenVisual.Editor
{
    /// <summary>
    /// DOTween 可视化编辑器主窗口
    /// </summary>
    public class DOTweenVisualEditorWindow : EditorWindow
    {
        #region 常量

        private const string UXML_PATH = "Assets/Plugins/DOTweenVisualEditor/Editor/UXML/DOTweenVisualEditorWindow.uxml";
        private const string USS_PATH = "Assets/Plugins/DOTweenVisualEditor/Editor/USS/DOTweenVisualEditor.uss";

        #endregion

        #region UI 元素

        private Toolbar toolbar;
        private Button previewButton;
        private Button stopButton;
        private Button resetButton;
        private ToolbarMenu addStepMenu;
        private ListView stepListView;
        private VisualElement timelineContainer;

        #endregion

        #region 数据

        private DOTweenVisualPlayer targetPlayer;
        private SerializedObject serializedObject;
        private SerializedProperty stepsProperty;

        #endregion

        #region 生命周期

        [MenuItem("Tools/DOTween Visual Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<DOTweenVisualEditorWindow>("DOTween Visual Editor");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnEnable()
        {
            // 监听选择变化
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            var selected = Selection.activeGameObject;
            if (selected != null)
            {
                var player = selected.GetComponent<DOTweenVisualPlayer>();
                if (player != null && player != targetPlayer)
                {
                    SetTarget(player);
                }
            }
        }

        private void SetTarget(DOTweenVisualPlayer player)
        {
            targetPlayer = player;
            
            if (player != null)
            {
                serializedObject = new SerializedObject(player);
                stepsProperty = serializedObject.FindProperty("Steps");
            }
            else
            {
                serializedObject = null;
                stepsProperty = null;
            }
            
            RefreshStepList();
        }

        #endregion

        #region UI 构建

        private void CreateGUI()
        {
            // 加载 UXML
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML_PATH);
            if (visualTree != null)
            {
                visualTree.CloneTree(rootVisualElement);
            }
            else
            {
                // 如果 UXML 不存在，手动构建
                BuildUIManually();
            }
            
            // 加载 USS
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(USS_PATH);
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }
            
            // 绑定回调
            BindCallbacks();
            
            // 检查当前选择
            if (Selection.activeGameObject != null)
            {
                var player = Selection.activeGameObject.GetComponent<DOTweenVisualPlayer>();
                if (player != null)
                {
                    SetTarget(player);
                }
            }
        }

        private void BuildUIManually()
        {
            rootVisualElement.Clear();
            
            // 工具栏
            toolbar = new Toolbar();
            toolbar.AddToClassList("main-toolbar");
            
            previewButton = new Button(OnPreviewClicked) { text = "预览" };
            previewButton.AddToClassList("toolbar-button");
            toolbar.Add(previewButton);
            
            stopButton = new Button(OnStopClicked) { text = "停止" };
            stopButton.AddToClassList("toolbar-button");
            toolbar.Add(stopButton);
            
            resetButton = new Button(OnResetClicked) { text = "重置" };
            resetButton.AddToClassList("toolbar-button");
            toolbar.Add(resetButton);
            
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            toolbar.Add(spacer);
            
            addStepMenu = new ToolbarMenu { text = "添加步骤" };
            addStepMenu.AddToClassList("toolbar-menu");
            BuildAddStepMenu();
            toolbar.Add(addStepMenu);
            
            rootVisualElement.Add(toolbar);
            
            // 步骤列表
            stepListView = new ListView
            {
                reorderable = true,
                makeItem = MakeStepItem,
                bindItem = BindStepItem,
                selectionType = SelectionType.Single,
                showBorder = true,
                showFoldoutHeader = false,
                showAddRemoveFooter = false
            };
            stepListView.AddToClassList("step-list");
            stepListView.style.flexGrow = 1;
            rootVisualElement.Add(stepListView);
            
            // 时间轴容器
            timelineContainer = new VisualElement();
            timelineContainer.AddToClassList("timeline-container");
            timelineContainer.style.height = 60;
            rootVisualElement.Add(timelineContainer);
            
            // 初始提示
            var helpLabel = new Label("请选择一个包含 DOTweenVisualPlayer 组件的物体");
            helpLabel.AddToClassList("help-label");
            rootVisualElement.Add(helpLabel);
        }

        private void BindCallbacks()
        {
            // 获取 UI 元素引用（如果从 UXML 加载）
            previewButton = rootVisualElement.Q<Button>("preview-button") ?? previewButton;
            stopButton = rootVisualElement.Q<Button>("stop-button") ?? stopButton;
            resetButton = rootVisualElement.Q<Button>("reset-button") ?? resetButton;
            addStepMenu = rootVisualElement.Q<ToolbarMenu>("add-step-menu") ?? addStepMenu;
            stepListView = rootVisualElement.Q<ListView>("step-list") ?? stepListView;
            timelineContainer = rootVisualElement.Q<VisualElement>("timeline-container") ?? timelineContainer;
            
            // 绑定按钮事件
            if (previewButton != null) previewButton.clickable = new Clickable(OnPreviewClicked);
            if (stopButton != null) stopButton.clickable = new Clickable(OnStopClicked);
            if (resetButton != null) resetButton.clickable = new Clickable(OnResetClicked);
        }

        #endregion

        #region 步骤列表

        private void RefreshStepList()
        {
            if (stepListView == null) return;
            
            if (stepsProperty != null)
            {
                stepListView.itemsSource = stepsProperty;
                stepListView.BindProperty(stepsProperty);
            }
            else
            {
                stepListView.itemsSource = null;
                stepListView.Unbind();
            }
        }

        private VisualElement MakeStepItem()
        {
            var item = new VisualElement();
            item.AddToClassList("step-item");
            
            // 折叠箭头 + 标题行
            var headerRow = new VisualElement();
            headerRow.AddToClassList("step-header-row");
            
            var foldout = new Foldout { value = false };
            foldout.AddToClassList("step-foldout");
            
            var titleLabel = new Label { name = "step-title" };
            titleLabel.AddToClassList("step-title");
            
            var summaryLabel = new Label { name = "step-summary" };
            summaryLabel.AddToClassList("step-summary");
            
            var buttonRow = new VisualElement();
            buttonRow.AddToClassList("step-button-row");
            
            var enableToggle = new Toggle { name = "enable-toggle", value = true };
            enableToggle.AddToClassList("enable-toggle");
            enableToggle.RegisterValueChangedCallback(evt => 
            {
                var property = item.userData as SerializedProperty;
                if (property != null)
                {
                    property.FindPropertyRelative("IsEnabled").boolValue = evt.newValue;
                    property.serializedObject.ApplyModifiedProperties();
                }
            });
            
            var deleteButton = new Button { text = "删除", name = "delete-button" };
            deleteButton.AddToClassList("delete-button");
            deleteButton.clickable = new Clickable(() =>
            {
                var property = item.userData as SerializedProperty;
                if (property != null)
                {
                    var index = stepsProperty.IndexOf(property);
                    if (index >= 0)
                    {
                        stepsProperty.DeleteArrayElementAtIndex(index);
                        stepsProperty.serializedObject.ApplyModifiedProperties();
                        RefreshStepList();
                    }
                }
            });
            
            buttonRow.Add(enableToggle);
            buttonRow.Add(deleteButton);
            
            headerRow.Add(foldout);
            headerRow.Add(titleLabel);
            headerRow.Add(summaryLabel);
            headerRow.Add(buttonRow);
            
            item.Add(headerRow);
            
            // 详情容器（在 Inspector 中编辑）
            var detailsContainer = new PropertyField { name = "step-details" };
            detailsContainer.AddToClassList("step-details");
            item.Add(detailsContainer);
            
            return item;
        }

        private void BindStepItem(VisualElement element, int index)
        {
            if (stepsProperty == null || index < 0 || index >= stepsProperty.arraySize) return;
            
            var stepProperty = stepsProperty.GetArrayElementAtIndex(index);
            element.userData = stepProperty;
            
            var typeProp = stepProperty.FindPropertyRelative("Type");
            var isEnabledProp = stepProperty.FindPropertyRelative("IsEnabled");
            var durationProp = stepProperty.FindPropertyRelative("Duration");
            var easeProp = stepProperty.FindPropertyRelative("Ease");
            
            var type = (TweenStepType)typeProp.enumValueIndex;
            
            // 更新标题
            var titleLabel = element.Q<Label>("step-title");
            if (titleLabel != null)
            {
                titleLabel.text = $"{index + 1}. {GetStepDisplayName(type)}";
            }
            
            // 更新摘要
            var summaryLabel = element.Q<Label>("step-summary");
            if (summaryLabel != null)
            {
                var ease = (Ease)easeProp.enumValueIndex;
                summaryLabel.text = $"{durationProp.floatValue:F1}s | {ease}";
            }
            
            // 更新启用状态
            var enableToggle = element.Q<Toggle>("enable-toggle");
            if (enableToggle != null)
            {
                enableToggle.SetValueWithoutNotify(isEnabledProp.boolValue);
            }
            
            // 绑定详情
            var detailsField = element.Q<PropertyField>("step-details");
            if (detailsField != null)
            {
                detailsField.BindProperty(stepProperty);
            }
        }

        private string GetStepDisplayName(TweenStepType type)
        {
            return type switch
            {
                TweenStepType.Move => "Move",
                TweenStepType.Rotate => "Rotate",
                TweenStepType.Scale => "Scale",
                TweenStepType.Delay => "Delay",
                TweenStepType.Callback => "Callback",
                TweenStepType.Property => "Property",
                _ => type.ToString()
            };
        }

        #endregion

        #region 工具栏回调

        private void BuildAddStepMenu()
        {
            if (addStepMenu == null) return;
            
            addStepMenu.menuItems.Clear();
            
            addStepMenu.menu.AppendAction("Transform/Move (Position)", _ => AddStep(TweenStepType.Move, TransformTarget.Position));
            addStepMenu.menu.AppendAction("Transform/Move (LocalPosition)", _ => AddStep(TweenStepType.Move, TransformTarget.LocalPosition));
            addStepMenu.menu.AppendAction("Transform/Rotate (Rotation)", _ => AddStep(TweenStepType.Rotate, TransformTarget.Rotation));
            addStepMenu.menu.AppendAction("Transform/Rotate (LocalRotation)", _ => AddStep(TweenStepType.Rotate, TransformTarget.LocalRotation));
            addStepMenu.menu.AppendAction("Transform/Scale", _ => AddStep(TweenStepType.Scale, TransformTarget.Scale));
            addStepMenu.menu.AppendSeparator();
            addStepMenu.menu.AppendAction("Other/Delay", _ => AddStep(TweenStepType.Delay));
            addStepMenu.menu.AppendAction("Other/Callback", _ => AddStep(TweenStepType.Callback));
        }

        private void AddStep(TweenStepType type, TransformTarget transformTarget = TransformTarget.Position)
        {
            if (stepsProperty == null)
            {
                Debug.LogWarning("请先选择一个 DOTweenVisualPlayer 组件");
                return;
            }
            
            stepsProperty.InsertArrayElementAtIndex(stepsProperty.arraySize);
            var newStep = stepsProperty.GetArrayElementAtIndex(stepsProperty.arraySize - 1);
            
            newStep.FindPropertyRelative("Type").enumValueIndex = (int)type;
            newStep.FindPropertyRelative("IsEnabled").boolValue = true;
            newStep.FindPropertyRelative("Duration").floatValue = 1f;
            newStep.FindPropertyRelative("Delay").floatValue = 0f;
            newStep.FindPropertyRelative("Ease").enumValueIndex = (int)Ease.OutQuad;
            newStep.FindPropertyRelative("TransformTarget").enumValueIndex = (int)transformTarget;
            
            stepsProperty.serializedObject.ApplyModifiedProperties();
            RefreshStepList();
        }

        private void OnPreviewClicked()
        {
            if (targetPlayer == null)
            {
                Debug.LogWarning("请先选择一个 DOTweenVisualPlayer 组件");
                return;
            }
            
            // 第三阶段实现编辑器预览
            Debug.Log("[DOTween Visual Editor] 预览功能将在第三阶段实现");
        }

        private void OnStopClicked()
        {
            if (targetPlayer == null) return;
            
            targetPlayer.Stop();
        }

        private void OnResetClicked()
        {
            if (targetPlayer == null) return;
            
            // 第三阶段实现重置功能
            Debug.Log("[DOTween Visual Editor] 重置功能将在第三阶段实现");
        }

        #endregion
    }
}
#endif
