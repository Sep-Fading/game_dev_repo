// Designed by KINEMATION, 2024.

using KINEMATION.KAnimationCore.Runtime.Core;
using KINEMATION.KAnimationCore.Runtime.Rig;
using KINEMATION.RetargetPro.Runtime.Features.BasicRetargeting;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KINEMATION.RetargetPro.Runtime.Features.IKRetargeting
{
    public class IKRetargetingState : BasicRetargetFeatureState
    {
        private IKRetargetFeature _asset;

        private int _sourceLength = 0;
        private int _targetLength = 0;

        private ChainIKData _chainIKData;
        private KTransform _ikGoal;
        private KTransform _ikPole;

        protected Vector3 GetEffector()
        {
            Transform target = sourceChain.transformChain[^1];
            
            KTransform sourceTransform = new KTransform(GetSourceRoot());
            KTransform targetTransform = new KTransform(GetTargetRoot());
            
            Vector3 sourcePose = sourceChain.cachedTransforms[^1].position;
            Vector3 targetPose = targetChain.cachedTransforms[^1].position;
            
            Vector3 additive = target.position - sourceTransform.TransformPoint(sourcePose, true);
            
            float scale = Mathf.Lerp(1f, chainScale, _asset.scaleWeight);
            additive = additive * scale + targetTransform.TransformPoint(targetPose, true);
            
            KTransform ikTarget = new KTransform()
            {
                position = additive,
                rotation = Quaternion.identity,
                scale = Vector3.one
            };

            return KAnimationMath.MoveInSpace(targetTransform, ikTarget, _asset.effectorOffset, 1f);
        }
        
        protected override void Initialize(RetargetFeature newAsset)
        {
            base.Initialize(newAsset);
            
            _asset = newAsset as IKRetargetFeature;
            if (_asset == null) return;
            
            _sourceLength = sourceChain.transformChain.Count;
            _targetLength = targetChain.transformChain.Count;

            _chainIKData.positions = new Vector3[_targetLength];
            _chainIKData.lengths = new float[_targetLength];

            _chainIKData.tolerance = 0.001f;
            _chainIKData.maxIterations = 25;
        }

        protected virtual void ApplyTwoBoneIK()
        {
            Transform tip = targetChain.transformChain[^1];
            Transform mid = targetChain.transformChain[^2];
            Transform root = targetChain.transformChain[^3];
            
            _ikGoal = new KTransform()
            {
                position = GetEffector(),
                rotation = Quaternion.identity,
                scale = Vector3.one
            };

            _ikPole = new KTransform(mid);
            _ikPole.position = KAnimationMath.MoveInSpace(new KTransform(GetTargetRoot()), _ikPole, 
                _asset.poleOffset, 1f);
            
            KTwoBoneIkData twoBoneIkData = new KTwoBoneIkData()
            {
                root = new KTransform(root),
                mid = new KTransform(mid),
                tip = new KTransform(tip),
                hint = _ikPole,
                target = _ikGoal,
                hasValidHint = true,
                rotWeight = -1f,
                posWeight = _asset.ikWeight * _asset.featureWeight,
                hintWeight = 1f,
            };
            
            KTwoBoneIK.Solve(ref twoBoneIkData);

            root.rotation = twoBoneIkData.root.rotation;
            mid.rotation = twoBoneIkData.mid.rotation;
            tip.rotation = twoBoneIkData.tip.rotation;
        }

        protected virtual void ApplyChainIK()
        {
            _chainIKData.maxReach = 0f;
            
            // 1. Gather position and length chain data.
            for (int i = 0; i < _targetLength; i++)
            {
                Vector3 position = targetChain.transformChain[i].position;

                float distance = 0f;
                if (i != _targetLength - 1)
                {
                    distance = Vector3.Distance(position, targetChain.transformChain[i + 1].position);
                }
                
                _chainIKData.positions[i] = position;
                _chainIKData.lengths[i] = distance;
                _chainIKData.maxReach += distance;
            }
            
            _chainIKData.target = GetEffector();
            _ikGoal = new KTransform()
            {
                position = GetEffector(),
                rotation = Quaternion.identity,
                scale = Vector3.one
            };
            
            // 2. Solve Chain IK.
            if (!KChainIK.SolveFABRIK(ref _chainIKData)) return;
            
            int tipIndex = _targetLength - 1;
            
            // 3. Apply rotations.
            for (int i = 0; i < tipIndex; ++i)
            {
                KTransform thisTransform = targetChain.cachedTransforms[i];
                KTransform nextTransform = targetChain.cachedTransforms[i + 1];
                
                var prevDir = nextTransform.position - thisTransform.position;
                var newDir = _chainIKData.positions[i + 1] - _chainIKData.positions[i];

                Quaternion baseRot = targetChain.transformChain[i].rotation;
                Quaternion targetRot = KMath.FromToRotation(prevDir, newDir) * thisTransform.rotation;
                
                targetRot = Quaternion.Slerp(baseRot, targetRot, _asset.ikWeight);
                baseRot = GetTargetRoot().rotation * thisTransform.rotation;
                targetRot = Quaternion.Slerp(baseRot, targetRot, _asset.featureWeight);
                
                targetChain.transformChain[i].rotation = targetRot;
            }
            
            RetargetBone(_sourceLength - 1, _targetLength - 1);
        }

        public override void Retarget(float normalizedTime = 0f)
        {
            base.Retarget(normalizedTime);

            if (_targetLength < 3)
            {
                return;
            }

            if (_targetLength == 3)
            {
                ApplyTwoBoneIK();
                return;
            }
            
            ApplyChainIK();
        }
        
#if UNITY_EDITOR
        private Vector3 RenderHandle(Transform space, KTransform bone)
        {
            Vector3 handlePos = Handles.PositionHandle(bone.position, space.rotation);
            return space.InverseTransformPoint(handlePos) - space.InverseTransformPoint(bone.position);
        }
        
        public override void OnSceneGUI()
        {
            RenderBoneChain(targetChain, Color.cyan);
            
            if (Mathf.Approximately(_asset.ikWeight, 0f))
            {
                return;
            }
            
            _asset.effectorOffset += RenderHandle(GetTargetRoot(), _ikGoal);
            Handles.Label(_ikGoal.position, "IK Goal");

            if (targetChain.transformChain.Count > 3) return;
            
            _asset.poleOffset += RenderHandle(GetTargetRoot(), _ikPole);
            Handles.Label(_ikPole.position, "IK Pole");
        }
#endif
    }
}