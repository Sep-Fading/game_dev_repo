// Designed by KINEMATION, 2024.

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using KINEMATION.RetargetPro.Runtime;
using System.IO;
using System.Linq;
using KINEMATION.Shared.KAnimationCore.Editor.Widgets;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;

namespace KINEMATION.RetargetPro.Editor
{
    public class RetargetHumanoidAnimWindow : EditorWindow
    {
        private AnimationClip _sourceClip;
        private List<AnimationClip> _batchClips = new List<AnimationClip>();
        private Vector2 _scrollPosition;
        private Vector2 _batchScrollPosition;
        private bool _isProcessing;
        private Texture2D _playIcon;
        private Texture2D _pauseIcon;
        private Texture2D _loopIcon;
        private Texture2D _stopIcon;
        private bool _showBatchMode;
        private string _errorMessage;
        private MessageType _messageType;
        private bool _showMessage;
        private bool _showSettings = true;  // Controls the foldout state of the settings panel

        // Settings fields (automatically serialized by EditorWindow)
        private GameObject _sourceCharacter;
        private GameObject _targetCharacter;
        private RetargetProfile _retargetProfile;
        private bool _copyClipSettings = true;
        private bool _exportRootMotionPosition = true;
        private bool _exportRootMotionRotation = true;
        private bool _keyframeAll = false;
        private bool _copyRootMotion = false;
        private float _frameRate = 60f;
        private string _savePath = "Assets/RetargetedHumanoidAnimations";

        // Preview related fields
        private bool _isPlaying;
        private bool _isLooping;
        private float _previewTime;
        private float _lastFrameTime;
        private AnimationClip _previewClip;
        private bool _isPaused;

        // Retargeting related fields
        private bool _isInitialized;
        private RetargetProComponent _retargetComponent;
        private GameObject _sourceCharacterInstance;
        private GameObject _targetCharacterInstance;
        private KRigComponent _sourceRigComponent;
        private KRigComponent _targetRigComponent;
        private Animator _sourceAnimator;
        private Animator _targetAnimator;

        private class BatchAnimationItem
        {
            public AnimationClip Clip;
            public bool IsPlaying;
            public bool HasRootMotion;
            public float PreviewTime;
            public bool IsLooping;
        }

        private List<BatchAnimationItem> _batchItems = new List<BatchAnimationItem>();
        private BatchAnimationItem _currentPreviewItem;

        private KToolbarWidget _toolbarWidget;
        private RetargetProfileWidget _profileEditor;

        private System.Action<RetargetProfile> _onProfileChanged;

        [MenuItem("Window/KINEMATION/Retarget Humanoid Animation")]
        public static void ShowWindow()
        {
            var window = GetWindow<RetargetHumanoidAnimWindow>("Humanoid Retargeting");
            window.minSize = new Vector2(400, 550);
            window.Show();
        }

        private void OnEnable()
        {
            try
            {
                LoadIcons();
                
                _toolbarWidget = new KToolbarWidget(new KToolbarTab[]
                {
                    new KToolbarTab()
                    {
                        name = "Baker",
                        onTabRendered = RenderBaker
                    },
                    new KToolbarTab()
                    {
                        name = "Retarget Profile",
                        onTabRendered = RenderProfile
                    }
                });

                _onProfileChanged = profile =>
                {
                    if (profile != null)
                    {
                        _profileEditor = new RetargetProfileWidget(profile);
                        _profileEditor.Init(new SerializedObject(profile));
                    }
                    else
                    {
                        _profileEditor = null;
                    }
                };
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error initializing RetargetHumanoidAnimWindow: {e.Message}");
            }
        }

        private void LoadIcons()
        {
            _playIcon = EditorGUIUtility.IconContent("PlayButton").image as Texture2D;
            _pauseIcon = EditorGUIUtility.IconContent("PauseButton").image as Texture2D;
            _loopIcon = EditorGUIUtility.IconContent("RotateTool").image as Texture2D;
            _stopIcon = EditorGUIUtility.IconContent("d_PreMatQuad").image as Texture2D;
        }

        private void OnDisable()
        {
            StopAllPreviews();
            UnInitializeBaker();
        }

        private void StopAllPreviews()
        {
            if (_currentPreviewItem != null)
            {
                _currentPreviewItem.IsPlaying = false;
                _currentPreviewItem = null;
            }
            StopPreview();
        }

        private bool HasRootMotion(AnimationClip clip)
        {
            if (clip == null) return false;
            
            var curves = AnimationUtility.GetCurveBindings(clip);
            foreach (var curve in curves)
            {
                if (curve.type == typeof(Animator) && 
                    (curve.propertyName.StartsWith("RootT.") || curve.propertyName.StartsWith("RootQ.")))
                {
                    return true;
                }
            }
            return false;
        }

        private void ShowError(string message)
        {
            _errorMessage = message;
            _messageType = MessageType.Error;
            _showMessage = true;
        }

        private void ShowWarning(string message)
        {
            _errorMessage = message;
            _messageType = MessageType.Warning;
            _showMessage = true;
        }

        private void ShowInfo(string message)
        {
            _errorMessage = message;
            _messageType = MessageType.Info;
            _showMessage = true;
        }

        private void ClearMessage()
        {
            _showMessage = false;
            _errorMessage = string.Empty;
        }

        private bool ValidatePlayback(AnimationClip clip)
        {
            // First validate if basic components are set
            if (_sourceCharacter == null)
            {
                ShowError("Please set the Source Character");
                return false;
            }

            if (_targetCharacter == null)
            {
                ShowError("Please set the Target Character");
                return false;
            }

            if (_retargetProfile == null)
            {
                ShowError("Please set the Retarget Profile");
                return false;
            }

            if (clip == null)
            {
                ShowError("No animation clip selected");
                return false;
            }

            // Initialize and validate
            if (!_isInitialized)
            {
                InitializeBaker();
            }

            if (!_isInitialized)
            {
                ShowError("Failed to initialize. Please check source and target characters");
                return false;
            }

            if (_sourceAnimator == null )
            {
                ShowError("Source character must exists");
                return false;
            }

            if (_targetAnimator == null || !_targetAnimator.isHuman)
            {
                ShowError("Target character must be a humanoid");
                return false;
            }

            ClearMessage();
            return true;
        }

        private void InitializeBaker()
        {
            _sourceCharacterInstance = _sourceCharacter;
            _targetCharacterInstance = _targetCharacter;
            
            if (EditorUtility.IsPersistent(_sourceCharacter))
            {
                _sourceCharacterInstance = Instantiate(_sourceCharacter);
            }
            
            if (EditorUtility.IsPersistent(_targetCharacter))
            {
                _targetCharacterInstance = Instantiate(_targetCharacter);
            }

            _sourceAnimator = _sourceCharacterInstance.GetComponent<Animator>();
            _targetAnimator = _targetCharacterInstance.GetComponent<Animator>();

            if (_sourceAnimator == null )
            {
                Debug.LogError("Source character must have a Animator!");
                return;
            }

            if (_targetAnimator == null || !_targetAnimator.isHuman)
            {
                Debug.LogError("Target character must have a Humanoid Animator!");
                return;
            }
            
            _retargetComponent = new RetargetProComponent();
            _retargetComponent.Initialize(_sourceCharacterInstance, _targetCharacterInstance, _retargetProfile);

            _sourceRigComponent = _sourceCharacterInstance.GetComponentInChildren<KRigComponent>();
            _targetRigComponent = _targetCharacterInstance.GetComponentInChildren<KRigComponent>();
            
            if (_sourceRigComponent == null || _targetRigComponent == null)
            {
                Debug.LogError($"Rig Component not found!");
                return;
            }

            if (!_sourceRigComponent.CompareRig(_retargetProfile.sourceRig))
            {
                Debug.LogWarning($"Rig mismatch: {_retargetProfile.sourceRig.name} is not up to date.");
            }
            
            if (!_targetRigComponent.CompareRig(_retargetProfile.targetRig))
            {
                Debug.LogWarning($"Rig mismatch: {_retargetProfile.targetRig.name} is not up to date.");
            }
            
            _sourceRigComponent.CacheHierarchyPose();
            _targetRigComponent.CacheHierarchyPose();

            _isInitialized = true;
            
            // Create profile editor
            _onProfileChanged?.Invoke(_retargetProfile);
        }

        private void UnInitializeBaker()
        {
            if (!_isInitialized) return;
            
            _isInitialized = false;
            
            if (_sourceRigComponent != null)
            {
                _sourceRigComponent.ApplyHierarchyCachedPose();
            }

            if (_targetRigComponent != null)
            {
                _targetRigComponent.ApplyHierarchyCachedPose();
            }
            
            _retargetComponent.DestroyRetargetFeatures();
            
            if(EditorUtility.IsPersistent(_sourceCharacter)) DestroyImmediate(_sourceCharacterInstance);
            if(EditorUtility.IsPersistent(_targetCharacter)) DestroyImmediate(_targetCharacterInstance);
        }

        private void RetargetAtTime(AnimationClip clip, float time)
        {
            if (!_isInitialized) return;
            
            clip.SampleAnimation(_sourceCharacterInstance, time);
            _retargetComponent.RetargetTransforms(time);
        }

        private AnimationClip BakeAnimation(AnimationClip animationToRetarget)
        {
            if (_sourceRigComponent == null || _targetRigComponent == null)
            {
                Debug.LogError("RetargetHumanoidAnimBaker: Rig Component is NULL!");
                return null;
            }

            if (!_targetAnimator.isHuman)
            {
                Debug.LogError("Target must be humanoid!");
                return null;
            }
            
            AnimationClip clip = new AnimationClip
            {
                name = $"{_targetCharacter.name}_{animationToRetarget.name}",
                frameRate = _frameRate
            };
            
            var toExclude = _retargetProfile.excludeChain.elementChain
                .Select(item => _targetRigComponent.GetRigTransform(item))
                .ToArray();
            var bones = _targetRigComponent.GetHierarchy()
                .Where(item => !toExclude.Contains(item))
                .ToArray();

            HumanoidAnimationBaker baker = new HumanoidAnimationBaker();
            baker.Initialize(_targetRigComponent.transform.root.gameObject, bones, _keyframeAll,_copyRootMotion,_exportRootMotionPosition, _exportRootMotionRotation);

            float playBack = 0f;
            float delta = 1f / _frameRate;
            
            while (playBack <= animationToRetarget.length)
            {
                RetargetAtTime(animationToRetarget, playBack);
                baker.BakeAnimationFrame(playBack);
                playBack += delta;
            }
            
            baker.WriteToClip(clip);
            if (_exportRootMotionPosition||_exportRootMotionRotation) baker.WriteRootMotion(animationToRetarget, clip);
            
            clip.EnsureQuaternionContinuity();
            
            if (_copyClipSettings)
            {
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(animationToRetarget);
                var events = AnimationUtility.GetAnimationEvents(animationToRetarget);
                
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                AnimationUtility.SetAnimationEvents(clip, events);
            }
            
            if (!Directory.Exists(_savePath))
            {
                Directory.CreateDirectory(_savePath);
            }

            string path = AssetDatabase.GenerateUniqueAssetPath($"{_savePath}/{clip.name}.anim");
            
            AssetDatabase.CreateAsset(clip, path);
            
            _sourceRigComponent.ApplyHierarchyCachedPose();
            _targetRigComponent.ApplyHierarchyCachedPose();

            return clip;
        }

        private void TogglePreview()
        {
            if (!ValidatePlayback(_sourceClip))
            {
                return;
            }

            if (IsPlaying())
            {
                // 暂停当前预览
                PausePreview();
            }
            else
            {
                // 如果之前是暂停状态，则继续播放
                if (_isPaused)
                {
                    ResumePreview();
                }
                else
                {
                    // 从头开始播放
                    StartPreview(_sourceClip);
                }
            }
        }

        private void ToggleBatchItemPreview(BatchAnimationItem item)
        {
            if (!ValidatePlayback(item.Clip))
            {
                return;
            }

            // 如果当前项正在播放
            if (item.IsPlaying)
            {
                // 暂停当前项
                item.IsPlaying = false;
                if (item == _currentPreviewItem)
                {
                    PausePreview();
                }
            }
            else
            {
                // 停止其他正在播放的预览
                if (_currentPreviewItem != null && _currentPreviewItem != item)
                {
                    _currentPreviewItem.IsPlaying = false;
                    StopPreview();
                }

                // 设置当前预览项
                item.IsPlaying = true;
                _currentPreviewItem = item;

                // 如果之前是暂停状态，则继续播放
                if (_isPaused && _currentPreviewItem == item)
                {
                    ResumePreview();
                }
                else
                {
                    // 从头开始播放
                    StartPreview(item.Clip);
                    SetPreviewTime(item.PreviewTime / item.Clip.length);
                }

                // 同步循环状态
                if (item.IsLooping)
                {
                    ToggleLoop();
                }
            }
        }

        private void StartPreview(AnimationClip clip)
        {
            if (clip == null) return;
            
            // 确保停止之前的预览
            StopPreview();
            
            _previewClip = clip;
            _previewTime = 0f;
            _lastFrameTime = (float)EditorApplication.timeSinceStartup;
            _isPlaying = true;
            _isPaused = false;
            EditorApplication.update += OnPreviewUpdate;
        }

        private void StopPreview()
        {
            if (_isPlaying || _isPaused)
            {
                _isPlaying = false;
                _isPaused = false;
                _previewTime = 0f;
                _previewClip = null;
                EditorApplication.update -= OnPreviewUpdate;
            }
        }

        private void PausePreview()
        {
            if (_isPlaying)
            {
                _isPlaying = false;
                _isPaused = true;
                EditorApplication.update -= OnPreviewUpdate;
            }
        }

        private void ResumePreview()
        {
            if (_isPaused)
            {
                _isPlaying = true;
                _isPaused = false;
                _lastFrameTime = (float)EditorApplication.timeSinceStartup;
                EditorApplication.update += OnPreviewUpdate;
            }
        }

        private void SetPreviewTime(float normalizedTime)
        {
            if (_previewClip == null) return;
            
            _previewTime = normalizedTime * _previewClip.length;
            PreviewFrame();
        }

        private void OnPreviewUpdate()
        {
            if (!_isPlaying || _previewClip == null) return;

            try
            {
                float currentTime = (float)EditorApplication.timeSinceStartup;
                float deltaTime = currentTime - _lastFrameTime;
                _lastFrameTime = currentTime;

                // Limit maximum deltaTime to prevent large jumps when editor is lagging
                deltaTime = Mathf.Min(deltaTime, 0.1f);
                
                _previewTime += deltaTime;
                
                if (_previewTime >= _previewClip.length)
                {
                    if (_isLooping)
                    {
                        _previewTime %= _previewClip.length;
                    }
                    else
                    {
                        _previewTime = _previewClip.length;
                        StopPreview();
                        return;
                    }
                }

                PreviewFrame();
                
                // Force UI repaint to ensure smooth progress bar updates
                Repaint();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during preview update: {e.Message}");
                StopPreview();
            }
        }

        private void PreviewFrame()
        {
            if (_previewClip == null) return;
            
            try
            {
                RetargetAtTime(_previewClip, _previewTime);
                SceneView.RepaintAll();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during preview: {e.Message}");
                StopPreview();
            }
        }

        public void ToggleLoop()
        {
            _isLooping = !_isLooping;
        }

        public bool IsPlaying()
        {
            return _isPlaying;
        }

        public bool IsLooping()
        {
            return _isLooping;
        }

        public float GetNormalizedTime()
        {
            if (_previewClip == null) return 0f;
            return _previewTime / _previewClip.length;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Humanoid Animation Retargeting", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            _toolbarWidget.Render();
            
            EditorGUILayout.Space(10);
            
            bool canBake = _sourceCharacter != null && 
                         _targetCharacter != null && 
                         _retargetProfile != null;

            EditorGUILayout.Space(10);

            if (!_showBatchMode)
            {
                // Single animation retarget button
                using (new EditorGUI.DisabledScope(!canBake || _sourceClip == null))
                {
                    if (GUILayout.Button("Retarget Animation", GUILayout.Height(30)))
                    {
                        RetargetAnimation();
                    }
                }
            }
            else
            {
                // Batch processing button
                using (new EditorGUI.DisabledScope(!canBake || _batchItems.Count == 0))
                {
                    if (GUILayout.Button($"Retarget {_batchItems.Count} Animations", GUILayout.Height(30)))
                    {
                        RetargetBatchAnimations();
                    }
                }
            }

            // Display message
            EditorGUILayout.Space(5);
            
            string message = "";
            MessageType messageType = MessageType.Info;

            if (_showMessage)
            {
                message = _errorMessage;
                messageType = _messageType;
            }
            else if (!canBake)
            {
                message = "Please assign all required fields above";
                messageType = MessageType.Warning;
            }
            else if (!_showBatchMode && _sourceClip == null)
            {
                message = "Please assign a source animation clip";
                messageType = MessageType.Warning;
            }
            else if (_showBatchMode && _batchItems.Count == 0)
            {
                message = "Please add animation clips for batch processing";
                messageType = MessageType.Warning;
            }

            if (!string.IsNullOrEmpty(message))
            {
                EditorGUILayout.HelpBox(message, messageType);
            }
        }

        private void RenderProfile()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            if (!EditorGUIUtility.wideMode)
            {
                EditorGUIUtility.wideMode = true;
            }

            if (_profileEditor != null)
            {
                _profileEditor.OnGUI();
                // 只要预览开始且未被停止，就每一帧都采样动画
                if (_previewClip != null && (_isPlaying || _isPaused))
                {
                    RetargetAtTime(_previewClip, _previewTime);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Select a Retarget Profile", MessageType.Info);
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void RetargetAnimation()
        {
            if (_sourceClip == null)
            {
                ShowError("No animation clip selected");
                return;
            }

            _isProcessing = true;
            
            // 停止所有预览并恢复初始姿势
            StopAllPreviews();
            if (_isInitialized)
            {
                UnInitializeBaker();
            }
            
            try
            {
                InitializeBaker();
                
                if (!_isInitialized)
                {
                    ShowError("Failed to initialize the retargeting process");
                    return;
                }

                EditorUtility.DisplayProgressBar("Retargeting Animation", 
                    $"Processing {_sourceClip.name}...", 0.5f);

                AnimationClip bakedClip = BakeAnimation(_sourceClip);

                if (bakedClip != null)
                {
                    ShowInfo($"Successfully retargeted animation to: {AssetDatabase.GetAssetPath(bakedClip)}");
                    EditorGUIUtility.PingObject(bakedClip);
                }
            }
            catch (System.Exception e)
            {
                ShowError($"Error during retargeting: {e.Message}");
            }
            finally
            {
                UnInitializeBaker();
                EditorUtility.ClearProgressBar();
                _isProcessing = false;
            }
        }

        private void RetargetBatchAnimations()
        {
            if (_batchItems.Count == 0)
            {
                ShowError("No animation clips to process");
                return;
            }

            _isProcessing = true;
            
            // 停止所有预览并恢复初始姿势
            StopAllPreviews();
            if (_isInitialized)
            {
                UnInitializeBaker();
            }
            
            try
            {
                InitializeBaker();
                
                if (!_isInitialized)
                {
                    ShowError("Failed to initialize the retargeting process");
                    return;
                }

                int total = _batchItems.Count;
                int processed = 0;
                int failed = 0;
                List<AnimationClip> bakedClips = new List<AnimationClip>();
                
                foreach (var item in _batchItems)
                {
                    if (item.Clip == null)
                    {
                        failed++;
                        continue;
                    }
                    
                    EditorUtility.DisplayProgressBar("Retargeting Animations", 
                        $"Processing {item.Clip.name} ({processed + 1}/{total})", 
                        (float)processed / total);

                    AnimationClip bakedClip = BakeAnimation(item.Clip);

                    if (bakedClip != null)
                    {
                        processed++;
                        bakedClips.Add(bakedClip);
                    }
                    else
                    {
                        failed++;
                    }
                }
                
                // Show processing results
                if (failed > 0)
                {
                    ShowWarning($"Completed with warnings: {processed} succeeded, {failed} failed");
                }
                else
                {
                    ShowInfo($"Successfully retargeted {processed} animations");
                }

                // Select the baked clips in the Project window
                if (bakedClips.Count > 0)
                {
                    Selection.objects = bakedClips.ToArray();
                }
            }
            catch (System.Exception e)
            {
                ShowError($"Error during batch retargeting: {e.Message}");
            }
            finally
            {
                UnInitializeBaker();
                EditorUtility.ClearProgressBar();
                _isProcessing = false;
            }
        }

        private void Update()
        {
            // Update current preview item time
            if (_currentPreviewItem != null && _currentPreviewItem.IsPlaying)
            {
                _currentPreviewItem.PreviewTime = GetNormalizedTime() * _currentPreviewItem.Clip.length;
                Repaint();
            }
        }

        private void RenderBaker()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Use foldout to organize settings
            _showSettings = EditorGUILayout.Foldout(_showSettings, "Settings", true);
            if (_showSettings)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Source character setting
                EditorGUI.BeginChangeCheck();
                _sourceCharacter = (GameObject)EditorGUILayout.ObjectField("Source Character", _sourceCharacter, typeof(GameObject), true);
                if (EditorGUI.EndChangeCheck() && _isInitialized)
                {
                    UnInitializeBaker();
                }
                
                // Target character setting
                EditorGUI.BeginChangeCheck();
                _targetCharacter = (GameObject)EditorGUILayout.ObjectField("Target Character", _targetCharacter, typeof(GameObject), true);
                if (EditorGUI.EndChangeCheck() && _isInitialized)
                {
                    UnInitializeBaker();
                }
                
                // Retarget profile setting
                EditorGUI.BeginChangeCheck();
                var newProfile = (RetargetProfile)EditorGUILayout.ObjectField("Retarget Profile", _retargetProfile, typeof(RetargetProfile), false);
                if (EditorGUI.EndChangeCheck())
                {
                    if (_isInitialized)
                    {
                        UnInitializeBaker();
                    }
                    _retargetProfile = newProfile;
                    _onProfileChanged?.Invoke(_retargetProfile);
                }
                
                EditorGUILayout.Space(5);
                
                // Other settings
                _copyClipSettings = EditorGUILayout.Toggle("Copy Clip Settings", _copyClipSettings);
                _exportRootMotionPosition = EditorGUILayout.Toggle("Root Motion Position", _exportRootMotionPosition);
                _exportRootMotionRotation = EditorGUILayout.Toggle("Root Motion Rotation", _exportRootMotionRotation);
                if (_exportRootMotionPosition||_exportRootMotionRotation)
                {
                    _copyRootMotion = EditorGUILayout.Toggle("Direct Copy (for 9CG)", _copyRootMotion);
                }
                _keyframeAll = EditorGUILayout.Toggle("Keyframe All", _keyframeAll);
                _frameRate = EditorGUILayout.FloatField("Frame Rate", _frameRate);
                _savePath = EditorGUILayout.TextField("Save Path", _savePath);
                
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.Space(5);

            using (new EditorGUI.DisabledScope(_isProcessing))
            {
                // Mode selection
                _showBatchMode = EditorGUILayout.ToggleLeft("Batch Mode", _showBatchMode);
                EditorGUILayout.Space(5);

                if (!_showBatchMode)
                {
                    // Single animation mode
                    DrawSingleAnimationMode();
                }
                else
                {
                    // Batch processing mode
                    DrawBatchAnimationMode();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSingleAnimationMode()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUIContent clipContent = new GUIContent("Source Animation", "The animation clip to retarget.");
            AnimationClip newClip = (AnimationClip)EditorGUILayout.ObjectField(clipContent, _sourceClip, typeof(AnimationClip), false);
            
            if (newClip != _sourceClip)
            {
                StopPreview();
                _sourceClip = newClip;
            }
            
            if (_sourceClip != null)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                
                // Check if playback is possible
                bool canPlay = _sourceCharacter != null && 
                             _targetCharacter != null && 
                             _retargetProfile != null;

                using (new EditorGUI.DisabledScope(!canPlay))
                {
                    // Play/Pause Button
                    if (GUILayout.Button(new GUIContent(IsPlaying() ? _pauseIcon : _playIcon), 
                        EditorStyles.toolbarButton, GUILayout.Width(35)))
                    {
                        if (!canPlay)
                        {
                            ShowError("Please set the Source Character, Target Character and Retarget Profile");
                        }
                        else
                        {
                            TogglePreview();
                        }
                    }

                    // Stop Button
                    if (GUILayout.Button(new GUIContent(_stopIcon), EditorStyles.toolbarButton, GUILayout.Width(35)))
                    {
                        if (!canPlay)
                        {
                            ShowError("Please set the Source Character, Target Character and Retarget Profile");
                        }
                        else
                        {
                            StopPreview();
                            if (_isInitialized)
                            {
                                _sourceRigComponent.ApplyHierarchyCachedPose();
                                _targetRigComponent.ApplyHierarchyCachedPose();
                            }
                        }
                    }
                    
                    // Loop Button
                    Color originalColor = GUI.color;
                    GUI.color = IsLooping() ? Color.cyan : originalColor;
                    if (GUILayout.Button(new GUIContent(_loopIcon), EditorStyles.toolbarButton, GUILayout.Width(35)))
                    {
                        if (!canPlay)
                        {
                            ShowError("Please set the Source Character, Target Character and Retarget Profile");
                        }
                        else
                        {
                            ToggleLoop();
                        }
                    }
                    GUI.color = originalColor;
                    
                    // Time Slider
                    float normalizedTime = GetNormalizedTime();
                    float newTime = GUILayout.HorizontalSlider(normalizedTime, 0f, 1f);
                    if (!Mathf.Approximately(newTime, normalizedTime))
                    {
                        if (!canPlay)
                        {
                            ShowError("Please set the Source Character, Target Character and Retarget Profile");
                        }
                        else
                        {
                            SetPreviewTime(newTime);
                        }
                    }
                }
                
                // Time Display
                float currentTime = GetNormalizedTime() * _sourceClip.length;
                EditorGUILayout.LabelField($"{currentTime:F2}s / {_sourceClip.length:F2}s", 
                    EditorStyles.miniLabel, GUILayout.Width(80));
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }

        private void DrawBatchAnimationMode()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Drop area
            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag and Drop Animation Clips Here", EditorStyles.helpBox);
            
            // Handle drag and drop
            if (Event.current.type == EventType.DragUpdated && dropArea.Contains(Event.current.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                Event.current.Use();
            }
            else if (Event.current.type == EventType.DragPerform && dropArea.Contains(Event.current.mousePosition))
            {
                DragAndDrop.AcceptDrag();
                
                foreach (Object draggedObject in DragAndDrop.objectReferences)
                {
                    if (draggedObject is AnimationClip clip)
                    {
                        if (!_batchItems.Exists(item => item.Clip == clip))
                        {
                            _batchItems.Add(new BatchAnimationItem 
                            { 
                                Clip = clip,
                                HasRootMotion = HasRootMotion(clip),
                                PreviewTime = 0f,
                                IsLooping = false
                            });
                        }
                    }
                }
                
                Event.current.Use();
            }

            // Display added animations list
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Animation Clips ({_batchItems.Count})", EditorStyles.boldLabel);
            
            _batchScrollPosition = EditorGUILayout.BeginScrollView(_batchScrollPosition, GUILayout.Height(250));
            
            for (int i = _batchItems.Count - 1; i >= 0; i--)
            {
                var item = _batchItems[i];
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Animation name and remove button
                EditorGUILayout.BeginHorizontal();
                
                AnimationClip newClip = (AnimationClip)EditorGUILayout.ObjectField(item.Clip, typeof(AnimationClip), false);
                if (newClip != item.Clip)
                {
                    if (item == _currentPreviewItem)
                    {
                        StopAllPreviews();
                    }
                    item.Clip = newClip;
                    item.HasRootMotion = HasRootMotion(newClip);
                    item.PreviewTime = 0f;
                }
                
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    if (item == _currentPreviewItem)
                    {
                        StopAllPreviews();
                    }
                    _batchItems.RemoveAt(i);
                    continue;
                }
                
                EditorGUILayout.EndHorizontal();

                if (item.Clip != null)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    
                    // Check if playback is possible
                    bool canPlay = _sourceCharacter != null && 
                                 _targetCharacter != null && 
                                 _retargetProfile != null;

                    using (new EditorGUI.DisabledScope(!canPlay))
                    {
                        // Play/Pause Button
                        if (GUILayout.Button(new GUIContent(item.IsPlaying ? _pauseIcon : _playIcon), 
                            EditorStyles.toolbarButton, GUILayout.Width(35)))
                        {
                            if (!canPlay)
                            {
                                ShowError("Please set the Source Character, Target Character and Retarget Profile");
                            }
                            else
                            {
                                ToggleBatchItemPreview(item);
                            }
                        }
                        
                        // Stop Button
                        if (GUILayout.Button(new GUIContent(_stopIcon), EditorStyles.toolbarButton, GUILayout.Width(35)))
                        {
                            if (!canPlay)
                            {
                                ShowError("Please set the Source Character, Target Character and Retarget Profile");
                            }
                            else
                            {
                                if (item == _currentPreviewItem)
                                {
                                    StopPreview();
                                    if (_isInitialized)
                                    {
                                        _sourceRigComponent.ApplyHierarchyCachedPose();
                                        _targetRigComponent.ApplyHierarchyCachedPose();
                                    }
                                }
                                item.IsPlaying = false;
                                item.PreviewTime = 0f;
                            }
                        }
                        
                        // Loop Button
                        Color originalColor = GUI.color;
                        GUI.color = item.IsLooping ? Color.cyan : originalColor;
                        if (GUILayout.Button(new GUIContent(_loopIcon), EditorStyles.toolbarButton, GUILayout.Width(35)))
                        {
                            if (!canPlay)
                            {
                                ShowError("Please set the Source Character, Target Character and Retarget Profile");
                            }
                            else
                            {
                                item.IsLooping = !item.IsLooping;
                                if (item == _currentPreviewItem)
                                {
                                    ToggleLoop();
                                }
                            }
                        }
                        GUI.color = originalColor;
                        
                        // Root Motion indicator
                        GUIStyle rootMotionStyle = new GUIStyle(EditorStyles.miniLabel);
                        rootMotionStyle.normal.textColor = item.HasRootMotion ? Color.green : Color.gray;
                        EditorGUILayout.LabelField(item.HasRootMotion ? "Root Motion" : "No Root Motion", 
                            rootMotionStyle, GUILayout.Width(80));
                        
                        // Time Slider
                        float normalizedTime = item.PreviewTime / item.Clip.length;
                        float newTime = GUILayout.HorizontalSlider(normalizedTime, 0f, 1f);
                        if (!Mathf.Approximately(newTime, normalizedTime))
                        {
                            item.PreviewTime = newTime * item.Clip.length;
                            if (item == _currentPreviewItem)
                            {
                                SetPreviewTime(newTime);
                            }
                        }
                        
                        // Time Display
                        EditorGUILayout.LabelField($"{item.PreviewTime:F2}s / {item.Clip.length:F2}s", 
                            EditorStyles.miniLabel, GUILayout.Width(80));
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndScrollView();
            
            // Clear button
            if (_batchItems.Count > 0)
            {
                if (GUILayout.Button("Clear All"))
                {
                    StopAllPreviews();
                    _batchItems.Clear();
                }
            }
            
            EditorGUILayout.EndVertical();
        }
    }
} 