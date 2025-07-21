// Designed by KINEMATION, 2024.

using KINEMATION.KAnimationCore.Runtime.Attributes;
using KINEMATION.KAnimationCore.Runtime.Rig;
using UnityEngine;

namespace KINEMATION.RetargetPro.Runtime.Features.FPSRetargeting
{
    public class FPSRetargetFeature : RetargetFeature
    {
        [Header("Source Chains")]
        
        [RigAssetSelector("sourceRig"), CustomElementChainDrawer(true, false)]
        public KRigElementChain sourceRightArm;
        
        [RigAssetSelector("sourceRig"), CustomElementChainDrawer(true, false)]
        public KRigElementChain sourceLeftArm;
        
        [RigAssetSelector("sourceRig"), CustomElementChainDrawer(true, false)]
        public KRigElementChain sourceWeapon;
        
        [Header("Target Chains")]
        [RigAssetSelector("targetRig"), CustomElementChainDrawer(true, false)]
        public KRigElementChain targetRightArm;
        
        [RigAssetSelector("targetRig"), CustomElementChainDrawer(true, false)]
        public KRigElementChain targetLeftArm;
        
        [RigAssetSelector("targetRig"), CustomElementChainDrawer(true, false)]
        public KRigElementChain targetWeapon;
        
        [HideInInspector] public bool enableWeaponHandle;
        [HideInInspector] public bool enableRightHandHandle;
        [HideInInspector] public bool enableLeftHandle;
        
        [HideInInspector] public bool enableRightPole;
        [HideInInspector] public bool enableLeftPole;

        [Header("IK Offsets")]
        [Tooltip("Right hand translation offset.")]
        public Vector3 rightHandOffset = Vector3.zero;
        [Tooltip("Left hand translation offset.")]
        public Vector3 leftHandOffset = Vector3.zero;
        [Tooltip("Weapon translation offset.")]
        public Vector3 weaponOffset = Vector3.zero;

        [Header("Pole IK Offsets")]
        [Tooltip("Right pole (elbow) translation offset.")]
        public Vector3 rightPoleOffset = Vector3.zero;
        [Tooltip("Left pole (elbow) translation offset.")]
        public Vector3 leftPoleOffset = Vector3.zero;
        
        public override RetargetFeatureState CreateFeatureState()
        {
            return new FPSRetargetFeatureState();
        }
        
#if UNITY_EDITOR
        private void MapWeaponBoneChain(KRig rig, KRigElementChain chain)
        {
            foreach (var element in rig.rigHierarchy)
            {
                if (element.name.Contains("gun") || element.name.Contains("weapon"))
                {
                    chain.elementChain.Clear();
                    chain.elementChain.Add(element);
                    chain.chainName = element.name;
                    return;
                }
            }
        }

        private void MapArmChain(KRig rig, KRigElementChain armChain, string[] queries)
        {
            foreach (var chain in rig.rigElementChains)
            {
                string chainName = chain.chainName.ToLower();

                if (!(chainName.Contains("clavicle") || chainName.Contains("shoulder") || chainName.Contains("arm")))
                {
                    continue;
                }

                foreach (var query in queries)
                {
                    if (chainName.Contains(query))
                    {
                        armChain.elementChain.Clear();
                        armChain.chainName = chain.chainName;
                        foreach (var element in chain.elementChain) armChain.elementChain.Add(element);
                        return;
                    }
                }
            }
        }
        
        public override void MapChains()
        {
            MapWeaponBoneChain(sourceRig, sourceWeapon);
            MapWeaponBoneChain(targetRig, targetWeapon);

            MapArmChain(sourceRig, sourceLeftArm, new []{"left_", "left", "_l"});
            MapArmChain(sourceRig, sourceRightArm, new []{"right_", "right" , "_r"});
            
            MapArmChain(targetRig, targetLeftArm, new []{"left_", "left", "_l"});
            MapArmChain(targetRig, targetRightArm, new []{"right_", "right" , "_r"});
        }

        public override string GetDisplayName()
        {
            return "FPS Animation Retarget";
        }
#endif
    }
}