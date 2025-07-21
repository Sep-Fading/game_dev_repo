// Designed by KINEMATION, 2024.

using KINEMATION.KAnimationCore.Runtime.Attributes;
using KINEMATION.KAnimationCore.Runtime.Rig;
using UnityEngine;

namespace KINEMATION.RetargetPro.Runtime.Features.BonePoser
{
    public class BonePoserFeature : RetargetFeature
    {
        [RigAssetSelector("targetRig"), CustomElementChainDrawer(true, false)]
        public KRigElementChain targetBoneChain;
        public Quaternion rotationPose = Quaternion.identity;

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