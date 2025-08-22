using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace KINEMATION.RetargetPro.Editor
{
    public class HumanoidAnimationBaker
    {
        private GameObject _target;
        private Animator _animator;
        private readonly List<AnimationFrame> _genericFrames = new List<AnimationFrame>();
        private HumanPoseHandler _poseHandler;
        private bool _keyframeAll;

        private void AddLinearKey(AnimationCurve curve, float time, float value)
        {
            int keysNum = curve.keys.Length;

            if (!_keyframeAll && keysNum > 1 && Mathf.Approximately(curve.keys[keysNum - 1].value, value))
            {
                curve.RemoveKey(keysNum - 1);
            }
            
            int index = curve.AddKey(time, value);
            AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
        }

        private bool IsHumanoidBone(Transform bone)
        {
            if (_animator == null || !_animator.isHuman) return false;
            
            for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                if (_animator.GetBoneTransform((HumanBodyBones)i) == bone)
                {
                    Debug.Log($" is humanoid bone: {bone.name}");
                    return true;
                }
            }
            return false;
        }

        private AnimationCurve _rootTX;
        private AnimationCurve _rootTY;
        private AnimationCurve _rootTZ;
        private AnimationCurve _rootQX;
        private AnimationCurve _rootQY;
        private AnimationCurve _rootQZ;
        private AnimationCurve _rootQW;
        private bool _isRootMotion = false;
        private bool _directCopyRootMotion = true;
        private bool _exportRootPosition = true;
        private bool _exportRootRotation = true;
		private bool _isFirstFrame = true;
        
        public void Initialize(GameObject target, Transform[] hierarchy,bool keyframeAll = true,
            bool directCopyRootMotion = true,
            bool exportRootPosition = true,
            bool exportRootRotation = true
            )
        {
            _target = target;
            _animator = target.GetComponent<Animator>();
            _keyframeAll = keyframeAll;
            _directCopyRootMotion = directCopyRootMotion;
            _exportRootPosition = exportRootPosition;
            _exportRootRotation = exportRootRotation;
            
            if (_animator == null)
            {
                Debug.LogError("Target GameObject must have an Animator component!");
                return;
            }

            if (!_animator.isHuman)
            {
                Debug.LogError("Target Animator must be Humanoid!");
                return;
            }
            _poseHandler = new HumanPoseHandler(_animator.avatar, _target.transform);
            _isRootMotion = _animator.applyRootMotion;
            _animator.applyRootMotion = false; 



            Transform root = target.transform;


            for (int i = 0; i < hierarchy.Length; i++)
            {
                var element = hierarchy[i];
                var parent = element.parent;

                string path = element.name;

                while (parent != null && parent != root)
                {
                    path = $"{parent.name}/{path}";
                    parent = parent.parent;
                }
                if (!IsHumanoidBone(element))
                {
                    _genericFrames.Add(new AnimationFrame
                    {
                        boneReference = element,
                        path = path
                    });
                }
                else
                {
                    //Debug.Log($"Skipping humanoid bone: {element.name}");
                }
            }
			
            _rootTX = new AnimationCurve();
            _rootTY = new AnimationCurve();
            _rootTZ = new AnimationCurve();
            _rootQX = new AnimationCurve();
            _rootQY = new AnimationCurve();
            _rootQZ = new AnimationCurve();
            _rootQW = new AnimationCurve();
            _muscles = new AnimationCurve[HumanTrait.MuscleCount];
            for (int i = 0; i < HumanTrait.MuscleCount; i++)
            {
                if (_muscles[i] == null)
                {
                    _muscles[i] = new AnimationCurve();
                }
            }
        }
		Vector3 positionBeforeBake;
		Quaternion rotationBeforeBake;

        public void BakeAnimationFrame(float time)
        {
			if(_isFirstFrame){
				_isFirstFrame=false;
				positionBeforeBake = _animator.avatarRoot.position;
				rotationBeforeBake = _animator.avatarRoot.rotation;
				_animator.avatarRoot.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
			}
            HumanPose currentPose = new HumanPose();
            _poseHandler.GetHumanPose(ref currentPose);
            
            for (int i = 0; i < HumanTrait.MuscleCount; i++)
            {
                AddLinearKey(_muscles[i], time, currentPose.muscles[i]);
            }

            if (!_directCopyRootMotion)
            {
                Vector3 position = currentPose.bodyPosition ;
                Quaternion rotation =  currentPose.bodyRotation;
                AddLinearKey(_rootTX, time, position.x);
                AddLinearKey(_rootTY, time, position.y);
                AddLinearKey(_rootTZ, time, position.z);
                AddLinearKey(_rootQX, time, rotation.x);
                AddLinearKey(_rootQY, time, rotation.y);
                AddLinearKey(_rootQZ, time, rotation.z);
                AddLinearKey(_rootQW, time, rotation.w);
            }

            foreach (var frame in _genericFrames)
            {
                Transform boneTransform = frame.boneReference;

                if (boneTransform != null)
                {
                    Quaternion normalizedRotation = boneTransform.localRotation.normalized;

                    AddLinearKey(frame.localPositionX, time, boneTransform.localPosition.x);
                    AddLinearKey(frame.localPositionY, time, boneTransform.localPosition.y);
                    AddLinearKey(frame.localPositionZ, time, boneTransform.localPosition.z);
                    
                    AddLinearKey(frame.localRotationW, time, normalizedRotation.w);
                    AddLinearKey(frame.localRotationX, time, normalizedRotation.x);
                    AddLinearKey(frame.localRotationY, time, normalizedRotation.y);
                    AddLinearKey(frame.localRotationZ, time, normalizedRotation.z);
                    
                    AddLinearKey(frame.localScaleX, time, boneTransform.localScale.x);
                    AddLinearKey(frame.localScaleY, time, boneTransform.localScale.y);
                    AddLinearKey(frame.localScaleZ, time, boneTransform.localScale.z);
                }
            }
        }

        private AnimationCurve[] _muscles = new AnimationCurve[HumanTrait.MuscleCount];

        public void WriteToClip(AnimationClip clip)
        {
            for (int i = 0; i < HumanTrait.MuscleCount; i++)
            {
                if (_muscles[i] != null)
                {
                    string muscleName = HumanTrait.MuscleName[i];
                    if(
                        muscleName.Contains("Thumb") ||
                        muscleName.Contains("Index") ||
                        muscleName.Contains("Little") ||
                        muscleName.Contains("Middle") ||
                        muscleName.Contains("Ring"))
                    {
                        string convertedName = muscleName;

                        convertedName = Regex.Replace(convertedName, 
                            @"^(Left|Right)\s", 
                            "$1Hand.");

                        convertedName = Regex.Replace(convertedName,
                            @"(Thumb|Index|Middle|Ring|Little)\s((\d+)\s(Stretched))", 
                            "$1.$2");
                        convertedName = Regex.Replace(convertedName,
                            @"(Thumb|Index|Middle|Ring|Little)\s((Spread))", 
                            "$1.$2");
                        // Debug.Log(convertedName);
                        clip.SetCurve("", typeof(Animator), convertedName, _muscles[i]);
                    }
                    else
                    {
                        clip.SetCurve("", typeof(Animator), muscleName, _muscles[i]);
                    }
                }
            }
             
            foreach (var frame in _genericFrames)
            {
                clip.SetCurve(frame.path, typeof(Transform), "localPosition.x", frame.localPositionX);
                clip.SetCurve(frame.path, typeof(Transform), "localPosition.y", frame.localPositionY);
                clip.SetCurve(frame.path, typeof(Transform), "localPosition.z", frame.localPositionZ);
            
                clip.SetCurve(frame.path, typeof(Transform), "localRotation.x", frame.localRotationX);
                clip.SetCurve(frame.path, typeof(Transform), "localRotation.y", frame.localRotationY);
                clip.SetCurve(frame.path, typeof(Transform), "localRotation.z", frame.localRotationZ);
                clip.SetCurve(frame.path, typeof(Transform), "localRotation.w", frame.localRotationW);
            
                clip.SetCurve(frame.path, typeof(Transform), "localScale.x", frame.localScaleX);
                clip.SetCurve(frame.path, typeof(Transform), "localScale.y", frame.localScaleY);
                clip.SetCurve(frame.path, typeof(Transform), "localScale.z", frame.localScaleZ);
            }
            _animator.applyRootMotion = _isRootMotion;
			_animator.avatarRoot.SetPositionAndRotation(positionBeforeBake,rotationBeforeBake);
        }

        public void WriteRootMotion(AnimationClip source, AnimationClip target)
        {
            if (!_directCopyRootMotion)
            {
                if (_exportRootPosition)
                {
                    target.SetCurve("", typeof(Animator), "RootT.x", _rootTX);
                    target.SetCurve("", typeof(Animator), "RootT.y", _rootTY);
                    target.SetCurve("", typeof(Animator), "RootT.z", _rootTZ);
                }

                if (_exportRootRotation)
                {
                    target.SetCurve("", typeof(Animator), "RootQ.x", _rootQX);
                    target.SetCurve("", typeof(Animator), "RootQ.y", _rootQY);
                    target.SetCurve("", typeof(Animator), "RootQ.z", _rootQZ);
                    target.SetCurve("", typeof(Animator), "RootQ.w", _rootQW);
                }
            }
            else
            {
                var bindings = AnimationUtility.GetCurveBindings(source);

                foreach (var binding in bindings)
                {
                    string propertyName = binding.propertyName.ToLower();
                
                    if(!propertyName.Contains("roott") && !propertyName.Contains("rootq")) continue;
                
                    if(!_exportRootPosition && propertyName.Contains("roott")) continue;
                    if(!_exportRootRotation && propertyName.Contains("rootq")) continue;
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(source, binding);
                    target.SetCurve("", typeof(Animator), binding.propertyName, curve);
                }
            }
        }
    }
} 