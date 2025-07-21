// Designed by KINEMATION, 2024.

using UnityEditor;
using UnityEngine;

namespace KINEMATION.RetargetPro.Editor
{
    public class RetargetProWindow : EditorWindow
    {
        public RetargetAnimBaker retargetAnimBaker;
        private AnimationClip _animation;
        private AnimationClip _itemAnimation;
        
        private bool _looping;
        
        // Will bake the motion into an animation clip.
        private float _sliderValueCache = -1f;
        private float _sliderValue = -1f;
        
        private Vector2 _scrollPosition;

        private bool _autoPlay = false;
        private float _repaintTimer = 0f;

        private float _lastFrameTime = 0f;

        private RetargetProfileWidget _retargetWidget;
        
        [MenuItem("Window/KINEMATION/Retarget Pro")]
        public static void ShowWindow()
        {
            GetWindow(typeof(RetargetProWindow), false, "Retarget Pro");
        }
        
        private void UpdatePlaybackSlider()
        {
            float sliderCache = _sliderValue;
            _sliderValue = EditorGUILayout.Slider(_sliderValue, 0f, _animation.length);

            if (!Mathf.Approximately(sliderCache, _sliderValue))
            {
                _autoPlay = false;
            }
        }

        private void StopRetargetPreview()
        {
            EditorApplication.update -= SampleAnimation;
            SceneView.duringSceneGui -= OnSceneGUI;
            
            retargetAnimBaker.UnInitializeBaker();
            _sliderValue = _sliderValueCache = 0f;
            _autoPlay = false;
        }
        
        private void DrawWindowGUI()
        {
            if (!retargetAnimBaker.Render())
            {
                if (retargetAnimBaker.IsInitialized) StopRetargetPreview();
                return;
            }
            
            var content = new GUIContent("Animation", "The clip to retarget.");
            _animation = (AnimationClip) EditorGUILayout.ObjectField(content, _animation, 
                typeof(AnimationClip), false);
            
            content = new GUIContent("Item Animation", "Animation clip for the weapon or item.");
            _itemAnimation = (AnimationClip) EditorGUILayout.ObjectField(content, _itemAnimation, 
                typeof(AnimationClip), false);
            
            if (_animation == null)
            {
                if (retargetAnimBaker.IsInitialized)
                {
                    StopRetargetPreview();
                }
                
                EditorGUILayout.HelpBox("Select Animation", MessageType.Warning);
                return;
            }
            
            EditorGUILayout.BeginHorizontal();

            bool pressedPlay = GUILayout.Button("Play");
            bool pressedLoop = GUILayout.Button("Loop");
            
            if ((pressedPlay || pressedLoop) && !retargetAnimBaker.IsInitialized)
            {
                EditorApplication.update += SampleAnimation;
                SceneView.duringSceneGui += OnSceneGUI;
                
                retargetAnimBaker.InitializeBaker();
                if (!retargetAnimBaker.IsInitialized)
                {
                    return;
                }
                
                retargetAnimBaker.RetargetAtTime(_animation, _itemAnimation, 0f);
            }
            
            if (pressedLoop && retargetAnimBaker.IsInitialized)
            {
                _autoPlay = true;
                _lastFrameTime = (float) EditorApplication.timeSinceStartup;
                _repaintTimer = 0f;
            }
            
            if (GUILayout.Button("Stop") && retargetAnimBaker.IsInitialized)
            {
                StopRetargetPreview();
            }
            
            UpdatePlaybackSlider();
            
            EditorGUILayout.EndHorizontal();
            
            retargetAnimBaker.RetargetAtTime(_animation, _itemAnimation, _sliderValue);

            if (GUILayout.Button("Bake Animation"))
            {
                if(!retargetAnimBaker.IsInitialized) retargetAnimBaker.InitializeBaker();
               retargetAnimBaker.BakeAnimation(_animation);
               StopRetargetPreview();
               
               AssetDatabase.SaveAssets();
               AssetDatabase.Refresh();
            }
        }
        
        private void OnEnable()
        {
            retargetAnimBaker = new RetargetAnimBaker();
            
            retargetAnimBaker.onProfileChanged = profile =>
            {
                if (profile == null)
                {
                    _retargetWidget = null;
                }
                else
                {
                    _retargetWidget = new RetargetProfileWidget(profile);
                    _retargetWidget.Init(new SerializedObject(profile));
                }
            };
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            retargetAnimBaker.RetargetComponent.OnSceneGUI();
            retargetAnimBaker.RetargetAtTime(_animation, _itemAnimation, _sliderValue);
        }

        private void SampleAnimation()
        {
            if (Mathf.Approximately(_sliderValue, _sliderValueCache) && !_autoPlay)
            {
                return;
            }
            
            if (_autoPlay)
            {
                float deltaTime = (float) EditorApplication.timeSinceStartup - _lastFrameTime;
                _sliderValue = _sliderValue + deltaTime > _animation.length ? 0f : _sliderValue + deltaTime;

                if (_repaintTimer > 1f / 60f)
                {
                    _repaintTimer = 0f;
                    Repaint();
                }

                _repaintTimer += deltaTime;
            }

            _sliderValueCache = _sliderValue;
            _lastFrameTime = (float) EditorApplication.timeSinceStartup;
        }
        
        private void OnGUI()
        {
            GUIStyle windowStyle = new GUIStyle();
            windowStyle.padding = new RectOffset(15, 5, 10, 5);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, windowStyle);
            
            DrawWindowGUI();
            
            EditorGUILayout.Space();
            
            _retargetWidget?.OnGUI();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void OnDisable()
        {
            StopRetargetPreview();
        }
    }
}
