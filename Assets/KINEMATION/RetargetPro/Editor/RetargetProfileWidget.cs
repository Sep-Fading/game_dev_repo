using System;
using System.Collections.Generic;
using System.Linq;

using KINEMATION.RetargetPro.Runtime;
using KINEMATION.RetargetPro.Runtime.Features;
using KINEMATION.RetargetPro.Runtime.Features.BasicRetargeting;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace KINEMATION.RetargetPro.Editor
{
    public class RetargetProfileWidget
    {
        public Action onComponentAdded;
        public Action onComponentPasted;
        public Action onComponentRemoved;
        
        private SerializedObject _serializedObject;
        private SerializedProperty _componentsProperty;
        private ScriptableObject _asset;
        
        private Type _collectionType;
        
        private List<UnityEditor.Editor> _editors;

        private bool _isInitialized;
        private ReorderableList _componentsList;
        
        private GUIStyle _elementButtonStyle;

        private Type[] _componentTypes;
        
        private int _editorIndex = -1;

        private RetargetProfile _retargetProfile;
        private Vector2 _scrollPosition;

        public RetargetProfileWidget(RetargetProfile profile)
        {
            _retargetProfile = profile;
        }

        public void AddComponent(Type type)
        {
            _serializedObject.Update();
            
            ScriptableObject newComponent = CreateNewComponent(type);
            
            Undo.RegisterCreatedObjectUndo(newComponent, "Add Component");
            AssetDatabase.AddObjectToAsset(newComponent, _asset);
            
            _componentsProperty.arraySize++;
            var componentProp = _componentsProperty.GetArrayElementAtIndex(_componentsProperty.arraySize - 1);
            componentProp.objectReferenceValue = newComponent;

            _editors.Add(UnityEditor.Editor.CreateEditor(newComponent));
            _serializedObject.ApplyModifiedProperties();
            
            EditorUtility.SetDirty(_asset);
            AssetDatabase.SaveAssetIfDirty(_asset);

            var newFeature = _retargetProfile.retargetFeatures[^1];
            newFeature.sourceRig = _retargetProfile.sourceRig;
            newFeature.targetRig = _retargetProfile.targetRig;
            
            onComponentAdded?.Invoke();
            _editorIndex = -1;
        }
        
        private ScriptableObject CreateNewComponent(Type type)
        {
            var instance = ScriptableObject.CreateInstance(type);
            instance.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            instance.name = type.Name;
            return instance;
        }

        private void OnTypeSelected(object userData, string[] options, int selected)
        {
            AddComponent(_componentTypes[selected]);
            _scrollPosition.y = 1;
        }

        public void RemoveComponent(int index)
        {
            _editors.RemoveAt(index);
            _serializedObject.Update();
            
            _editorIndex = -1;
            
            var property = _componentsProperty.GetArrayElementAtIndex(index);
            var component = property.objectReferenceValue;
            
            property.objectReferenceValue = null;
            _componentsProperty.DeleteArrayElementAtIndex(index);
            
            _serializedObject.ApplyModifiedProperties();
            Undo.DestroyObjectImmediate(component);
            
            EditorUtility.SetDirty(_asset);
            AssetDatabase.SaveAssets();
            
            onComponentRemoved?.Invoke();
        }
        
        private void CopyComponent(Object component)
        {
            string typeName = component.GetType().AssemblyQualifiedName;
            string typeData = JsonUtility.ToJson(component);
            EditorGUIUtility.systemCopyBuffer = $"{typeName}|{typeData}";
        }

        private bool CanPaste(Object component)
        {
            if (string.IsNullOrWhiteSpace(EditorGUIUtility.systemCopyBuffer)) return false;

            string clipboard = EditorGUIUtility.systemCopyBuffer;
            int separator = clipboard.IndexOf('|');

            if (separator < 0) return false;

            return component.GetType().AssemblyQualifiedName == clipboard.Substring(0, separator);
        }

        private void PasteComponent(Object component)
        {
            string clipboard = EditorGUIUtility.systemCopyBuffer;
            string typeData = clipboard.Substring(clipboard.IndexOf('|') + 1);
            Undo.RecordObject(component, "Paste Settings");
            JsonUtility.FromJsonOverwrite(typeData, component);
        }

        private void OnContextMenuSelection(object userData, string[] options, int selected)
        {
            int index = (int) userData;
            Object component = _componentsProperty.GetArrayElementAtIndex(index).objectReferenceValue;
            
            if (selected == 0)
            {
                CopyComponent(component);
                return;
            }

            if (selected == 1)
            {
                if (!CanPaste(component)) return;

                PasteComponent(component);
                onComponentPasted?.Invoke();
                return;
            }

            RemoveComponent(index);
        }

        private void SetupReorderableList()
        {
            _componentsList = new ReorderableList(_serializedObject, _componentsProperty, true, 
                false, false, true);
            
            _componentsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                RetargetFeature feature = _retargetProfile.retargetFeatures[index];
                BasicRetargetFeature basicFeature = feature as BasicRetargetFeature;
                
                Rect toggleRect = rect;
                toggleRect.width = 14f;
                
                bool enabled = EditorGUI.Toggle(toggleRect, feature.featureWeight > 0f);
                
                if (feature.featureWeight == 0f && enabled)
                {
                    feature.featureWeight = 1f;
                    EditorUtility.SetDirty(_retargetProfile);
                }

                if (feature.featureWeight > 0f && !enabled)
                {
                    feature.featureWeight = 0f;
                    EditorUtility.SetDirty(_retargetProfile);
                }

                if (isFocused && _editorIndex != index)
                {
                    _editorIndex = index;
                    feature.drawGizmos = true;
                }

                if (feature.drawGizmos && !isActive)
                {
                    feature.drawGizmos = false;
                }
                
                float iconWidth = 25f;
                Rect iconRect = new Rect(rect.x + rect.width - iconWidth, rect.y, iconWidth, rect.height);
                iconRect.y += 2f;
                iconRect.height -= 3f;
                
                var iconContent = EditorGUIUtility.IconContent("Refresh");
                iconContent.tooltip = "Auto map bone chains.";
                
                if (GUI.Button(iconRect, iconContent))
                {
                    feature.MapChains();
                }

                iconRect.x -= iconRect.width + 3f;
                
                Rect labelsRect = rect;
                float leftPadding = 16f;
                labelsRect.x += toggleRect.width + leftPadding;

                if (basicFeature != null)
                {
                    labelsRect.width = (iconRect.x - labelsRect.x) / 2f;

                    EditorGUI.LabelField(labelsRect, basicFeature.sourceChain.chainName);

                    labelsRect.x += labelsRect.width + leftPadding;
                    labelsRect.width = iconRect.x - labelsRect.x;
                    EditorGUI.LabelField(labelsRect, basicFeature.targetChain.chainName);
                }
                else
                {
                    labelsRect.width = iconRect.x - labelsRect.x;
                    EditorGUI.LabelField(labelsRect, feature.GetDisplayName());
                }

                if (!feature.GetStatus())
                {
                    iconContent = EditorGUIUtility.IconContent("console.warnicon");

                    float padding = 3;
                    iconRect.x += padding;
                    iconRect.width -= padding;
                    iconRect.y += padding;
                    iconRect.height -= padding;

                    iconContent.tooltip = feature.GetErrorMessage();

                    GUI.Label(iconRect, iconContent);
                }
                
                if (Event.current.type == EventType.MouseUp && Event.current.button == 1 
                    && rect.Contains(Event.current.mousePosition))
                {
                    GUIContent[] menuOptions = new GUIContent[]
                    {
                        new GUIContent("Copy"),
                        new GUIContent("Paste"),
                        new GUIContent("Delete")
                    };
                
                    EditorUtility.DisplayCustomMenu(new Rect(Event.current.mousePosition, Vector2.zero), 
                        menuOptions, -1, OnContextMenuSelection, index);
                }
            };

            _componentsList.onReorderCallbackWithDetails = (ReorderableList list, int oldIndex, int newIndex) =>
            {
                UnityEditor.Editor editorToMove = _editors[oldIndex];
                
                _editors.RemoveAt(oldIndex);
                if (newIndex > oldIndex)
                {
                    if (newIndex > _editors.Count - 1)
                    {
                        _editors.Add(editorToMove);
                        return;
                    }
                }
                
                _editors.Insert(newIndex, editorToMove);
            };

            _componentsList.onRemoveCallback = list =>
            {
                RemoveComponent(list.index);
            };
        }
        
        public void Init(SerializedObject serializedObject)
        {
            _serializedObject = serializedObject;
            _componentsProperty = _serializedObject.FindProperty("retargetFeatures");
            _collectionType = typeof(RetargetFeature);
            
            Assert.IsNotNull(_componentsProperty);
            Assert.IsNotNull(_collectionType);
            
            _asset = _serializedObject.targetObject as ScriptableObject;
            if (_asset == null)
            {
                Debug.LogError($"{_serializedObject.targetObject.name} is not a Scriptable Object!");
                return;
            }

            if (!_componentsProperty.isArray)
            {
                Debug.LogError($"{_componentsProperty.displayName} is not an array!");
                return;
            }

            List<Type> collectionTypes = new List<Type>();
            var allCollectionTypes = TypeCache.GetTypesDerivedFrom(_collectionType).ToArray();

            foreach (var type in allCollectionTypes)
            {
                if(type.IsAbstract) continue;
                collectionTypes.Add(type);
            }
            
            _componentTypes = collectionTypes.ToArray();
            _editors = new List<UnityEditor.Editor>();

            // Create editors for the current components.
            int arraySize = _componentsProperty.arraySize;
            for (int i = 0; i < arraySize; i++)
            {
                SerializedProperty element = _componentsProperty.GetArrayElementAtIndex(i);
                _editors.Add(UnityEditor.Editor.CreateEditor(element.objectReferenceValue));
            }
            
            SetupReorderableList();
            _isInitialized = true;
        }

        private void DrawWidgetGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MinHeight(250f));
            
            _componentsList.DoLayoutList();
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Add Retarget Feature", EditorStyles.miniButton))
            {
                int count = _componentTypes.Length;
                
                GUIContent[] menuOptions = new GUIContent[count];
                for (int i = 0; i < count; i++)
                {
                    menuOptions[i] = new GUIContent(_componentTypes[i].Name);
                }
                
                EditorUtility.DisplayCustomMenu(new Rect(Event.current.mousePosition, Vector2.zero), 
                    menuOptions, -1, OnTypeSelected, null);
            }
            
            if (_editorIndex < 0 || _editors.Count == 0)
            {
                return;
            }
            
            EditorGUILayout.Space();

            var style = GUI.skin.box;
            style.padding = new RectOffset(12, 12, 12, 12);
            EditorGUILayout.BeginVertical(style);
            _editors[_editorIndex].OnInspectorGUI();
            EditorGUILayout.EndVertical();
        }

        public void OnGUI()
        {
            if (!_isInitialized || _retargetProfile == null) return;

            bool wideMode = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;

            _serializedObject.Update();
            DrawWidgetGUI();
            _serializedObject.ApplyModifiedProperties();

            EditorGUIUtility.wideMode = wideMode;
        }
    }
}