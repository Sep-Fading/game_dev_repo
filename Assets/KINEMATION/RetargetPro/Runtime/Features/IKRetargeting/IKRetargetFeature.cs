// Designed by KINEMATION, 2024.

using KINEMATION.RetargetPro.Runtime.Features.BasicRetargeting;

using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KINEMATION.RetargetPro.Runtime.Features.IKRetargeting
{
    public class IKRetargetFeature : BasicRetargetFeature
    {
        [Tooltip("Controls the influence of IK. If 0 - disabled, 1 - fully enabled.")]
        [Range(0f, 1f)] public float ikWeight = 1f;
        [Tooltip("IK target position offset.")]
        public Vector3 effectorOffset;
        public Vector3 poleOffset;
        
        public override RetargetFeatureState CreateFeatureState()
        {
            return new IKRetargetingState();
        }

        public override IRetargetJob SetupRetargetJob(PlayableGraph graph, out AnimationScriptPlayable playable)
        {
            IKRetargetJob job = new IKRetargetJob();
            playable = AnimationScriptPlayable.Create(graph, job);
            return job;
        }
    }
}