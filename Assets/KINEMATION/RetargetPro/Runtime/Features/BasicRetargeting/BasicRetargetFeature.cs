// Designed by KINEMATION, 2024.

using KINEMATION.KAnimationCore.Runtime.Attributes;
using KINEMATION.KAnimationCore.Runtime.Rig;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KINEMATION.RetargetPro.Runtime.Features.BasicRetargeting
{
    public class BasicRetargetFeature : RetargetFeature, IDynamicRetarget
    {
        [RigAssetSelector("sourceRig"), CustomElementChainDrawer(true, true)]
        [Tooltip("Bones to retarget FROM.")]
        public KRigElementChain sourceChain;
        
        [RigAssetSelector("targetRig"), CustomElementChainDrawer(true, true)]
        [Tooltip("Bones to retarget TO.")]
        public KRigElementChain targetChain;
        
        [Tooltip("Scale influence. If 1 - model scale will be taken into account.")]
        [Range(0f, 1f)] public float scaleWeight = 1f;
        [Tooltip("Influence of the translation. Use it for hip, root and ik bones.")]
        [Range(0f, 1f)] public float translationWeight;
        
        [Tooltip("Translation offset. Useful for root and hip bones.")]
        public Vector3 offset = Vector3.zero;
        
        public override RetargetFeatureState CreateFeatureState()
        {
            return new BasicRetargetFeatureState();
        }
        
        public virtual IRetargetJob SetupRetargetJob(PlayableGraph graph, out AnimationScriptPlayable playable)
        {
            BasicRetargetJob job = new BasicRetargetJob();
            playable = AnimationScriptPlayable.Create(graph, job);
            return job;
        }
        
#if UNITY_EDITOR
        public override string GetDisplayName()
        {
            return "Basic Retarget";
        }

        public override void MapChains()
        {
            foreach (var fromChain in sourceRig.rigElementChains)
            {
                if (!RetargetUtility.IsNameMatching(fromChain.chainName, targetChain.chainName)) continue;
                
                sourceChain = new KRigElementChain
                {
                    chainName = fromChain.chainName
                };
                foreach (var element in fromChain.elementChain) sourceChain.elementChain.Add(element);

                break;
            }
        }

        public override bool GetStatus()
        {
            return sourceChain.elementChain.Count > 0 && targetChain.elementChain.Count > 0;
        }

        public override string GetErrorMessage()
        {
            return "Source chain is empty! Please press the `Refresh` icon to the right" +
                   " or manually adjust the chain.";
        }
#endif
    }
}