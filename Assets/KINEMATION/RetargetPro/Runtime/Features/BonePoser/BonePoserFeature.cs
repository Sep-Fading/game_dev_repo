// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using KINEMATION.Shared.KAnimationCore.Runtime.Attributes;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using UnityEngine;

namespace KINEMATION.RetargetPro.Runtime.Features.BonePoser
{
    public class BonePoserFeature : RetargetFeature
    {
        [RigAssetSelector("targetRig"), CustomElementChainDrawer(true, false)]
        public KRigElementChain targetBoneChain;
        public Quaternion rotationPose = Quaternion.identity;
        public bool isAdditive = false;

        public override RetargetFeatureState CreateFeatureState()
        {
            return new BonePoserFeatureState();
        }
        
#if UNITY_EDITOR
        public override string GetDisplayName()
        {
            if (targetBoneChain?.elementChain == null 
                || targetBoneChain.elementChain.Count == 0) return "Bone Poser";
            
            return $"Bone Poser for {targetBoneChain.elementChain[0].name}";
        }
#endif
    }
}