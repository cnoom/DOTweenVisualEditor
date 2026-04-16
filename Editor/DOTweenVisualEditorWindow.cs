#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;
using DG.DOTweenEditor;
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
        private const bool DEBUG_MODE = false;

        #endregion

        #region UI 元素

        private Toolbar toolbar;
        private ObjectField targetField;
        private Button previewButton;
        private Button stopButton;
        private Button replayButton;
        private Button resetButton;
        private ToolbarMenu addStepMenu;
        private ListView stepListView;
        private VisualElement timelineContainer;
        private Label helpLabel;
        private IMGUIContainer timelineGUI;

        // 状态栏元素
        private VisualElement statusBar;
        private Label stateLabel;
        private Label timeLabel;

        #endregion

        #region 数据

        private DOTweenVisualPlayer targetPlayer;
        private SerializedObject serializedObject;
        private SerializedProperty stepsProperty;

        #endregion

        #region 预览状态

        // 预览状态枚举
        private enum PreviewState
        {
            None,       // 未播放
            Playing,    // 播放中
            Paused,     // 已暂停
            Completed   // 播放完成
        }

        private PreviewState previewState = PreviewState.None;
        private Sequence previewSequence;
        private Dictionary<Transform, TransformState> initialStates = new();

        // 兼容旧代码的属性
        private bool isPreviewing => previewState == PreviewState.Playing;
        private bool isPaused => previewState == PreviewState.Paused;
        private bool hasPreviewed => previewState != PreviewState.None;

        private struct TransformState
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
        }

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
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            Log("OnDisable");
            EditorApplication.update -= OnEditorUpdate;
            StopPreview();
        }

        private void OnEditorUpdate()
        {
            // 实时更新时间显示
            UpdateTimeDisplay();
        }

        private void UpdateTimeDisplay()
        {
            if (previewSequence == null)
            {
                timeLabel.text = "--:-- / --:--";
                return;
            }

            float currentTime = previewSequence.Elapsed(false);
            float totalTime = previewSequence.Duration(false);

            string currentStr = FormatTime(currentTime);
            string totalStr = FormatTime(totalTime);

            timeLabel.text = $"{currentStr} / {totalStr}";

            // 更新时间轴显示
            if (timelineGUI != null)
            {
                timelineGUI.MarkDirtyRepaint();
            }
        }

        private string FormatTime(float seconds)
        {
            int minutes = (int)(seconds / 60);
            int secs = (int)(seconds % 60);
            int ms = (int)((seconds * 10) % 10);
            return $"{minutes:D2}:{secs:D2}.{ms}";
        }

        private void OnTargetChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            Log($"OnTargetChanged: {evt.newValue}");
            
            // 切换目标时先停止预览
            if (isPreviewing || isPaused)
            {
                StopPreview();
            }
            
            var player = evt.newValue as DOTweenVisualPlayer;
            SetTarget(player);
            UpdateButtonStates();
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
            
            replayButton = new Button(OnReplayClicked) { text = "重播" };
            replayButton.AddToClassList("toolbar-button");
            toolbar.Add(replayButton);
            
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
            
            // 状态栏（显示预览状态和时间）
            statusBar = new VisualElement();
            statusBar.AddToClassList("status-bar");
            statusBar.style.flexDirection = FlexDirection.Row;
            statusBar.style.paddingLeft = 8;
            statusBar.style.paddingRight = 8;
            statusBar.style.paddingTop = 4;
            statusBar.style.paddingBottom = 4;
            statusBar.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            stateLabel = new Label("● 未播放");
            stateLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            stateLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            stateLabel.style.flexGrow = 1;
            statusBar.Add(stateLabel);
            
            timeLabel = new Label("--:-- / --:--");
            timeLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
            timeLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            timeLabel.style.width = 140;
            statusBar.Add(timeLabel);
            
            rootVisualElement.Add(statusBar);
            
            // 主内容区域（包含列表和帮助提示）
            var contentArea = new VisualElement();
            contentArea.style.flexGrow = 1;
            contentArea.style.position = Position.Relative;
            
            // 步骤列表
            stepListView = new ListView
            {
                reorderable = false,  // 禁用拖拽，改用上移/下移按钮
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
            
            contentArea.Add(stepListView);
            
            // 初始提示（覆盖在列表上方）
            helpLabel = new Label("请在上方指定目标物体（包含 DOTweenVisualPlayer 组件）");
            helpLabel.AddToClassList("help-label");
            helpLabel.style.position = Position.Absolute;
            helpLabel.style.left = 0;
            helpLabel.style.right = 0;
            helpLabel.style.top = 0;
            helpLabel.style.bottom = 0;
            contentArea.Add(helpLabel);
            
            rootVisualElement.Add(contentArea);
            
            // 时间轴容器（固定在底部）
            timelineContainer = new VisualElement();
            timelineContainer.AddToClassList("timeline-container");

            // 时间轴标题
            var timelineTitle = new Label("时间轴预览");
            timelineTitle.AddToClassList("timeline-label");
            timelineContainer.Add(timelineTitle);

            // 时间轴绘制区域
            timelineGUI = new IMGUIContainer(() =>
            {
                DrawTimeline();
            });
            timelineGUI.style.flexGrow = 1;
            timelineContainer.Add(timelineGUI);

            rootVisualElement.Add(timelineContainer);
            
            // 构建添加步骤菜单
            BuildAddStepMenu();
            
            // 初始化按钮状态
            UpdateButtonStates();
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
            
            // 更新按钮状态（步骤数量变化可能影响预览按钮可用性）
            UpdateButtonStates();

            // 更新时间轴显示
            if (timelineGUI != null)
            {
                timelineGUI.MarkDirtyRepaint();
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
            
            var foldout = new Foldout { value = false, name = "step-foldout" };
            foldout.AddToClassList("step-foldout");
            
            var titleLabel = new Label { name = "step-title" };
            titleLabel.AddToClassList("step-title");
            
            var summaryLabel = new Label { name = "step-summary" };
            summaryLabel.AddToClassList("step-summary");
            
            var buttonRow = new VisualElement();
            buttonRow.AddToClassList("step-button-row");
            
            // 上移按钮
            var upButton = new Button { text = "↑", name = "up-button" };
            upButton.AddToClassList("move-button");
            upButton.clickable = new Clickable(() =>
            {
                var property = item.userData as SerializedProperty;
                if (property != null && stepsProperty != null)
                {
                    int index = FindPropertyIndex(stepsProperty, property);
                    if (index > 0)
                    {
                        int newIndex = index - 1;
                        stepsProperty.MoveArrayElement(index, newIndex);
                        stepsProperty.serializedObject.ApplyModifiedProperties();
                        RefreshStepList();
                        
                        // 设置选中状态跟随移动的项
                        if (stepListView != null)
                        {
                            stepListView.selectedIndex = newIndex;
                        }
                    }
                }
            });
            
            var downButton = new Button { text = "↓", name = "down-button" };
            downButton.AddToClassList("move-button");
            downButton.clickable = new Clickable(() =>
            {
                var property = item.userData as SerializedProperty;
                if (property != null && stepsProperty != null)
                {
                    int index = FindPropertyIndex(stepsProperty, property);
                    if (index >= 0 && index < stepsProperty.arraySize - 1)
                    {
                        int newIndex = index + 1;
                        stepsProperty.MoveArrayElement(index, newIndex);
                        stepsProperty.serializedObject.ApplyModifiedProperties();
                        RefreshStepList();
                        
                        // 设置选中状态跟随移动的项
                        if (stepListView != null)
                        {
                            stepListView.selectedIndex = newIndex;
                        }
                    }
                }
            });
            
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
            
            buttonRow.Add(upButton);
            buttonRow.Add(downButton);
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
            
            // 默认隐藏详情
            detailsContainer.style.display = DisplayStyle.None;
            
            // Foldout 折叠/展开控制
            foldout.RegisterValueChangedCallback(evt => 
            {
                detailsContainer.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            });
            
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
            var targetTransformProp = stepProperty.FindPropertyRelative("TargetTransform");
            
            var type = (TweenStepType)typeProp.enumValueIndex;
            
            // 更新标题 - 显示目标物体名称和类型
            var titleLabel = element.Q<Label>("step-title");
            if (titleLabel != null)
            {
                string targetName = "未指定";
                if (targetTransformProp.objectReferenceValue != null)
                {
                    targetName = targetTransformProp.objectReferenceValue.name;
                }
                titleLabel.text = $"{index + 1}. [{targetName}] {GetStepDisplayName(type)}";
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
            
            // 监听属性变化，刷新标题和摘要
            element.TrackPropertyValue(typeProp, _ => UpdateStepTitle(element, index));
            element.TrackPropertyValue(targetTransformProp, _ => UpdateStepTitle(element, index));
            element.TrackPropertyValue(durationProp, _ => UpdateStepTitle(element, index));
            element.TrackPropertyValue(easeProp, _ => UpdateStepTitle(element, index));
        }
        
        /// <summary>
        /// 更新步骤标题显示
        /// </summary>
        private void UpdateStepTitle(VisualElement element, int index)
        {
            if (stepsProperty == null || index < 0 || index >= stepsProperty.arraySize) return;
            
            // 刷新数据
            serializedObject?.Update();
            
            var stepProperty = stepsProperty.GetArrayElementAtIndex(index);
            var typeProp = stepProperty.FindPropertyRelative("Type");
            var targetTransformProp = stepProperty.FindPropertyRelative("TargetTransform");
            
            var type = (TweenStepType)typeProp.enumValueIndex;
            var titleLabel = element.Q<Label>("step-title");
            
            if (titleLabel != null)
            {
                string targetName = "未指定";
                if (targetTransformProp.objectReferenceValue != null)
                {
                    targetName = targetTransformProp.objectReferenceValue.name;
                }
                titleLabel.text = $"{index + 1}. [{targetName}] {GetStepDisplayName(type)}";
            }
            
            // 同时更新摘要
            var durationProp = stepProperty.FindPropertyRelative("Duration");
            var easeProp = stepProperty.FindPropertyRelative("Ease");
            var summaryLabel = element.Q<Label>("step-summary");
            if (summaryLabel != null)
            {
                var ease = (Ease)easeProp.enumValueIndex;
                summaryLabel.text = $"{durationProp.floatValue:F1}s | {ease}";
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

            if (previewState == PreviewState.Playing)
            {
                // 正在播放 → 暂停
                PausePreview();
            }
            else if (previewState == PreviewState.Paused)
            {
                // 已暂停 → 继续
                ResumePreview();
            }
            else if (previewState == PreviewState.None)
            {
                // 未播放 → 开始预览
                StartPreview();
            }
            // Completed 状态不响应，由重播按钮处理
        }

        private void OnReplayClicked()
        {
            if (targetPlayer == null) return;

            // 恢复初始状态
            RestoreInitialStates();

            // 重新开始预览
            StartPreview();
        }

        private void OnStopClicked()
        {
            StopPreview();
        }

        private void OnResetClicked()
        {
            if (targetPlayer == null) return;
            
            RestoreInitialStates();
            
            // 重置后清除状态，下次预览会重新保存初始状态
            previewState = PreviewState.None;
            initialStates.Clear();
            UpdateButtonStates();
        }

        #region 预览逻辑

        private void StartPreview()
        {
            if (targetPlayer == null || targetPlayer.StepCount == 0) return;

            Log("StartPreview");

            // 清理旧序列
            if (previewSequence != null)
            {
                previewSequence.Kill();
                previewSequence = null;
            }
            DOTweenEditorPreview.Stop();

            // 如果已有初始状态（播放完成后重新预览），先恢复初始状态
            if (initialStates.Count > 0)
            {
                RestoreInitialStates();
            }
            else
            {
                // 首次预览，保存初始状态
                SaveInitialStates();
            }

            // 启动编辑器预览模式
            DOTweenEditorPreview.Start();

            // 创建预览序列
            previewSequence = DOTween.Sequence();

            // 构建预览序列
            BuildPreviewSequence();

            // 调试：检查序列状态
            Log($"Preview sequence created, duration: {previewSequence.Duration()}");

            // 为编辑器预览准备 Tween（必须在设置 OnComplete 之前调用）
            DOTweenEditorPreview.PrepareTweenForPreview(previewSequence);

            // 播放完成后设置状态（必须在 PrepareTweenForPreview 之后调用）
            previewSequence.OnComplete(() =>
            {
                Log("Preview completed");
                previewState = PreviewState.Completed;
                UpdateButtonStates();
            });

            previewSequence.Play();
            previewState = PreviewState.Playing;
            UpdateButtonStates();

            Log($"Preview started, isPlaying: {previewSequence.IsPlaying()}");
        }

        private void PausePreview()
        {
            Log("PausePreview");

            if (previewSequence != null && previewSequence.IsPlaying())
            {
                previewSequence.Pause();
                previewState = PreviewState.Paused;
                UpdateButtonStates();
            }
        }

        private void ResumePreview()
        {
            Log("ResumePreview");

            if (previewSequence != null && !previewSequence.IsPlaying())
            {
                previewSequence.Play();
                previewState = PreviewState.Playing;
                UpdateButtonStates();
            }
        }

        private void StopPreview()
        {
            Log("StopPreview");

            if (previewSequence != null)
            {
                previewSequence.Kill();
                previewSequence = null;
            }

            // 停止编辑器预览模式
            DOTweenEditorPreview.Stop();

            // 停止后设置为 Completed 状态，保留初始状态供用户重置
            previewState = PreviewState.Completed;
            UpdateButtonStates();
        }

        /// <summary>
        /// 更新按钮状态，防止误触
        /// </summary>
        private void UpdateButtonStates()
        {
            // 目标物体是否存在
            bool hasTarget = targetPlayer != null;

            // 步骤是否存在
            bool hasSteps = hasTarget && targetPlayer.StepCount > 0;

            // 是否在预览中（播放或暂停）
            bool inPreview = isPreviewing || isPaused;

            // 是否已播放完成
            bool isCompleted = previewState == PreviewState.Completed;

            // --- 预览按钮 ---
            // 仅在 None/Playing/Paused 状态启用
            previewButton.SetEnabled(hasSteps && !isCompleted);
            previewButton.text = isPreviewing ? "暂停" : (isPaused ? "继续" : "预览");

            // --- 停止按钮 ---
            // 仅在预览过程中（播放或暂停）启用
            stopButton.SetEnabled(inPreview);

            // --- 重播按钮 ---
            // 仅在播放完成后启用
            replayButton.SetEnabled(hasSteps && isCompleted);

            // --- 重置按钮 ---
            // 仅在播放完成后启用
            resetButton.SetEnabled(isCompleted);

            // --- 添加步骤菜单 ---
            // 预览过程中禁用，避免数据不一致
            addStepMenu.SetEnabled(hasTarget && !inPreview);

            // --- 更新状态栏 ---
            UpdateStatusBar();
        }

        private void UpdateStatusBar()
        {
            switch (previewState)
            {
                case PreviewState.None:
                    stateLabel.text = "● 未播放";
                    stateLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                    break;
                case PreviewState.Playing:
                    stateLabel.text = "● 播放中";
                    stateLabel.style.color = new Color(0.3f, 0.8f, 0.3f);
                    break;
                case PreviewState.Paused:
                    stateLabel.text = "● 已暂停";
                    stateLabel.style.color = new Color(1f, 0.7f, 0f);
                    break;
                case PreviewState.Completed:
                    stateLabel.text = "● 播放完成";
                    stateLabel.style.color = new Color(0.3f, 0.6f, 1f);
                    break;
            }
        }

        private void DrawTimeline()
        {
            float totalDuration = 0f;
            float currentTime = 0f;

            // 计算总时长
            if (previewSequence != null)
            {
                totalDuration = (float)previewSequence.Duration();
                currentTime = previewSequence.Elapsed();
            }
            else if (targetPlayer != null && targetPlayer.Steps != null)
            {
                // 未预览时，计算步骤总时长
                foreach (var step in targetPlayer.Steps)
                {
                    if (step.IsEnabled)
                    {
                        totalDuration += step.Duration;
                    }
                }
            }

            // 获取绘制区域
            Rect rect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth - 20, 40);
            float barHeight = 20f;
            float barY = rect.y + 10;

            // 绘制背景
            EditorGUI.DrawRect(new Rect(rect.x, barY, rect.width, barHeight), new Color(0.2f, 0.2f, 0.2f));

            // 绘制进度条（仅在预览时）
            if (previewSequence != null && totalDuration > 0)
            {
                float progress = currentTime / totalDuration;
                float progressWidth = rect.width * progress;
                EditorGUI.DrawRect(new Rect(rect.x, barY, progressWidth, barHeight), new Color(0.3f, 0.6f, 1f));
            }

            // 绘制时间刻度
            int totalSeconds = Mathf.CeilToInt(totalDuration);
            if (totalSeconds > 0)
            {
                int maxMarks = Mathf.Min(totalSeconds, 10);
                for (int i = 0; i <= maxMarks; i++)
                {
                    float x = rect.x + (rect.width * i / maxMarks);
                    EditorGUI.DrawRect(new Rect(x, barY + barHeight, 1, 5), Color.gray);

                    // 绘制刻度标签
                    int second = (totalSeconds * i) / maxMarks;
                    var labelRect = new Rect(x - 10, barY + barHeight + 5, 20, 15);
                    EditorGUI.LabelField(labelRect, $"{second}s", EditorStyles.centeredGreyMiniLabel);
                }
            }

            // 绘制当前位置标记（仅在预览时）
            if (previewSequence != null && totalDuration > 0 && currentTime >= 0)
            {
                float markerX = rect.x + (rect.width * currentTime / totalDuration);
                EditorGUI.DrawRect(new Rect(markerX - 1, barY - 2, 2, barHeight + 4), Color.white);
            }
        }

        private void SaveInitialStates()
        {
            initialStates.Clear();

            // 保存主物体状态
            SaveTransformState(targetPlayer.transform);

            // 保存所有步骤中涉及的物体状态
            foreach (var step in targetPlayer.Steps)
            {
                if (step.TargetTransform != null)
                {
                    SaveTransformState(step.TargetTransform);
                }
            }
        }

        private void SaveTransformState(Transform t)
        {
            if (!initialStates.ContainsKey(t))
            {
                initialStates[t] = new TransformState
                {
                    position = t.position,
                    rotation = t.rotation,
                    localPosition = t.localPosition,
                    localRotation = t.localRotation,
                    localScale = t.localScale
                };
            }
        }

        private void RestoreInitialStates()
        {
            foreach (var kvp in initialStates)
            {
                var t = kvp.Key;
                var state = kvp.Value;

                if (t != null)
                {
                    Undo.RecordObject(t, "Reset Preview State");
                    t.position = state.position;
                    t.rotation = state.rotation;
                    t.localPosition = state.localPosition;
                    t.localRotation = state.localRotation;
                    t.localScale = state.localScale;
                }
            }

            initialStates.Clear();
            Log("Initial states restored");
        }

        private void BuildPreviewSequence()
        {
            Log($"BuildPreviewSequence: step count = {targetPlayer.StepCount}");
            
            int addedCount = 0;
            foreach (var step in targetPlayer.Steps)
            {
                if (!step.IsEnabled)
                {
                    Log($"Step skipped (disabled)");
                    continue;
                }
                
                AppendStepToPreview(step);
                addedCount++;
            }
            
            Log($"Added {addedCount} steps to preview sequence");
        }

        private void AppendStepToPreview(TweenStepData step)
        {
            var target = step.TargetTransform != null ? step.TargetTransform : targetPlayer.transform;
            Log($"AppendStep: {step.Type} | Target: {target.name} | Value: {step.TargetValue} | Duration: {step.Duration}");
            
            Tweener tweener = null;

            switch (step.Type)
            {
                case TweenStepType.Move:
                    tweener = CreateMoveTween(step);
                    break;
                case TweenStepType.Rotate:
                    tweener = CreateRotateTween(step);
                    break;
                case TweenStepType.Scale:
                    tweener = CreateScaleTween(step);
                    break;
                case TweenStepType.Delay:
                    previewSequence.AppendInterval(step.Duration);
                    return;
                case TweenStepType.Callback:
                    previewSequence.AppendCallback(() => step.OnComplete?.Invoke());
                    return;
            }

            if (tweener == null)
            {
                Log($"Warning: tweener is null for {step.Type}");
                return;
            }

            // 设置缓动
            if (step.UseCustomCurve && step.CustomCurve != null)
            {
                tweener.SetEase(step.CustomCurve);
            }
            else
            {
                tweener.SetEase(step.Ease);
            }

            // 添加到序列
            switch (step.ExecutionMode)
            {
                case ExecutionMode.Append:
                    previewSequence.Append(tweener);
                    break;
                case ExecutionMode.Join:
                    previewSequence.Join(tweener);
                    break;
                case ExecutionMode.Insert:
                    previewSequence.Insert(step.InsertTime, tweener);
                    break;
            }
            
            Log($"Tweener added to sequence");
        }

        private Tweener CreateMoveTween(TweenStepData step)
        {
            var target = step.TargetTransform != null ? step.TargetTransform : targetPlayer.transform;
            Tweener tweener = null;

            switch (step.TransformTarget)
            {
                case TransformTarget.Position:
                    tweener = target.DOMove(step.TargetValue, step.Duration);
                    break;
                case TransformTarget.LocalPosition:
                    tweener = target.DOLocalMove(step.TargetValue, step.Duration);
                    break;
                default:
                    tweener = target.DOMove(step.TargetValue, step.Duration);
                    break;
            }

            if (step.IsRelative)
            {
                tweener.SetRelative(true);
            }

            return tweener;
        }

        private Tweener CreateRotateTween(TweenStepData step)
        {
            var target = step.TargetTransform != null ? step.TargetTransform : targetPlayer.transform;
            Tweener tweener = null;

            switch (step.TransformTarget)
            {
                case TransformTarget.Rotation:
                    tweener = target.DORotate(step.TargetValue, step.Duration);
                    break;
                case TransformTarget.LocalRotation:
                    tweener = target.DOLocalRotate(step.TargetValue, step.Duration);
                    break;
                default:
                    tweener = target.DORotate(step.TargetValue, step.Duration);
                    break;
            }

            if (step.IsRelative)
            {
                tweener.SetRelative(true);
            }

            return tweener;
        }

        private Tweener CreateScaleTween(TweenStepData step)
        {
            var target = step.TargetTransform != null ? step.TargetTransform : targetPlayer.transform;
            var tweener = target.DOScale(step.TargetValue, step.Duration);

            if (step.IsRelative)
            {
                tweener.SetRelative(true);
            }

            return tweener;
        }

        #endregion

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
