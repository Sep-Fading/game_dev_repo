using KINEMATION.KAnimationCore.Runtime.Attributes;
using KINEMATION.KAnimationCore.Runtime.Rig;

namespace KINEMATION.RetargetPro.Runtime.Features.CopyBone
{
    public class CopyBoneFeatureSettings : RetargetFeature
    {
        [RigAssetSelector("sourceRig"), CustomElementChainDrawer(true, false)]
        public KRigElementChain copyFrom;
        
        [RigAssetSelector("targetRig"), CustomElementChainDrawer(true, false)]
        public KRigElementChain copyTo;

        public override RetargetFeatureState CreateFeatureState()
        {
            return new CopyBoneFeatureState();
        }
        
#if UNITY_EDITOR
        public override string GetDisplayName()
        {
            string fromName = "None";
            if (copyFrom is {elementChain: {Count: > 0}}) fromName = copyFrom.elementChain[0].name;

            string toName = "None";
            if (copyTo is {elementChain: {Count: > 0}}) toName = copyTo.elementChain[0].name;
            
            return $"Copy from: {fromName}, to: {toName}";
        }
#endif
    }
}
