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

        private const string USS_PATH = "Assets/Plugins/DOTweenVisualEditor/Editor/USS/DOTweenVisualEditor.uss";
        private const bool DEBUG_MODE = true;

        #endregion

        #region UI 元素

        private Toolbar toolbar;
        private ObjectField targetField;
        private Button previewButton;
        private Button stopButton;
        private Button resetButton;
        private ToolbarMenu addStepMenu;
        private ListView stepListView;
        private VisualElement timelineContainer;
        private Label helpLabel;

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
            Log("OnEnable");
        }

        private void OnDisable()
        {
            Log("OnDisable");
        }

        private void OnTargetChanged(ChangeEvent<Object> evt)
        {
            Log($"OnTargetChanged: {evt.newValue}");
            
            var player = evt.newValue as DOTweenVisualPlayer;
            SetTarget(player);
        }

        private void SetTarget(DOTweenVisualPlayer player)
        {
            Log($"SetTarget: {player}");
            
            targetPlayer = player;
            
            if (player != null)
            {
                serializedObject = new SerializedObject(player);
                stepsProperty = serializedObject.FindProperty("_steps");  // 使用私有字段名
                Log($"stepsProperty: {stepsProperty}, arraySize: {stepsProperty?.arraySize}");
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
            Log("CreateGUI");
            
            BuildUIManually();
            
            // 加载 USS
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(USS_PATH);
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }
            
            // 如果已有目标，刷新列表
            if (targetPlayer != null)
            {
                SetTarget(targetPlayer);
            }
        }

        private void BuildUIManually()
        {
            rootVisualElement.Clear();
            
            // 工具栏
            toolbar = new Toolbar();
            toolbar.AddToClassList("main-toolbar");
            
            // 目标物体选择器
            var targetLabel = new Label("目标物体:");
            targetLabel.style.marginRight = 4;
            toolbar.Add(targetLabel);
            
            targetField = new ObjectField
            {
                objectType = typeof(DOTweenVisualPlayer),
                allowSceneObjects = true,
                value = targetPlayer
            };
            targetField.style.width = 200;
            targetField.RegisterValueChangedCallback(OnTargetChanged);
            toolbar.Add(targetField);
            
            var spacer1 = new VisualElement();
            spacer1.style.flexGrow = 1;
            toolbar.Add(spacer1);
            
            previewButton = new Button(OnPreviewClicked) { text = "预览" };
            previewButton.AddToClassList("toolbar-button");
            toolbar.Add(previewButton);
            
            stopButton = new Button(OnStopClicked) { text = "停止" };
            stopButton.AddToClassList("toolbar-button");
            toolbar.Add(stopButton);
            
            resetButton = new Button(OnResetClicked) { text = "重置" };
            resetButton.AddToClassList("toolbar-button");
            toolbar.Add(resetButton);
            
            var spacer2 = new VisualElement();
            spacer2.style.flexGrow = 1;
            toolbar.Add(spacer2);
            
            addStepMenu = new ToolbarMenu { text = "添加步骤" };
            addStepMenu.AddToClassList("toolbar-menu");
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
                showAddRemoveFooter = false,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight
            };
            stepListView.AddToClassList("step-list");
            stepListView.style.flexGrow = 1;
            stepListView.style.minHeight = 100;
            
            rootVisualElement.Add(stepListView);
            
            // 时间轴容器
            timelineContainer = new VisualElement();
            timelineContainer.AddToClassList("timeline-container");
            timelineContainer.style.height = 60;
            rootVisualElement.Add(timelineContainer);
            
            // 初始提示
            helpLabel = new Label("请在上方指定目标物体（包含 DOTweenVisualPlayer 组件）");
            helpLabel.AddToClassList("help-label");
            rootVisualElement.Add(helpLabel);
            
            // 构建添加步骤菜单
            BuildAddStepMenu();
        }

        #endregion

        #region 步骤列表

        private void RefreshStepList()
        {
            Log($"RefreshStepList - stepListView null: {stepListView == null}, stepsProperty null: {stepsProperty == null}");
            
            if (stepListView == null) return;
            
            // 隐藏帮助提示
            if (helpLabel != null)
            {
                helpLabel.style.display = stepsProperty != null ? DisplayStyle.None : DisplayStyle.Flex;
            }
            
            if (stepsProperty != null && serializedObject != null)
            {
                // 确保 serializedObject 是最新的
                serializedObject.Update();
                
                Log($"Binding stepsProperty, arraySize: {stepsProperty.arraySize}");
                
                // 绑定属性
                stepListView.BindProperty(stepsProperty);
            }
            else
            {
                Log("Unbinding and clearing list");
                stepListView.Unbind();
                stepListView.itemsSource = System.Array.Empty<object>();
            }
        }

        private VisualElement MakeStepItem()
        {
            Log("MakeStepItem");
            
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
                if (property != null && stepsProperty != null)
                {
                    int index = FindPropertyIndex(stepsProperty, property);
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
            Log($"BindStepItem index: {index}");
            
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

        private int FindPropertyIndex(SerializedProperty arrayProperty, SerializedProperty elementProperty)
        {
            if (arrayProperty == null || elementProperty == null) return -1;
            
            for (int i = 0; i < arrayProperty.arraySize; i++)
            {
                var item = arrayProperty.GetArrayElementAtIndex(i);
                if (item.propertyPath == elementProperty.propertyPath)
                {
                    return i;
                }
            }
            return -1;
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
            Debug.Log("[DOTween Visual Editor] 重置功能将在第三阶段实现");
        }

        #endregion

        #region 调试

        private void Log(string message)
        {
            if (DEBUG_MODE)
            {
                Debug.Log($"[DOTweenVisualEditor] {message}");
            }
        }

        #endregion
    }
}
#endif
