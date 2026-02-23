// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using UnityEngine;

namespace KINEMATION.RetargetPro.Runtime.Features.BonePoser
{
    public class BonePoserFeatureState : RetargetFeatureState
    {
        protected KTransformChain _boneChain;
        protected BonePoserFeature _asset;

        public override bool IsValid()
        {
            return _boneChain == null || _boneChain.IsValid();
        }
        
        protected override void Initialize(RetargetFeature newAsset)
        {
            _asset = newAsset as BonePoserFeature;
            if (_asset == null) return;
            
            _boneChain = RetargetUtility.GetTransformChain(targetRigComponent, _asset.targetRig, 
                _asset.targetBoneChain);
            
            _boneChain.CacheTransforms(ESpaceType.ParentBoneSpace);
        }

        public override void Retarget(float time = 0)
        {
            int count = _boneChain.transformChain.Count;

            for (int i = 0; i < count; i++)
            {
                var bone = _boneChain.transformChain[i];
                Quaternion defaultRotation = _boneChain.cachedTransforms[i].rotation;

                if (_asset.isAdditive)
                {
                    bone.localRotation *= Quaternion.Slerp(Quaternion.identity, _asset.rotationPose, 
                        _asset.featureWeight);
                }
                else
                {
                    bone.localRotation = Quaternion.Slerp(defaultRotation, defaultRotation * _asset.rotationPose,
                        _asset.featureWeight);
                }
            }
        }
    }
}