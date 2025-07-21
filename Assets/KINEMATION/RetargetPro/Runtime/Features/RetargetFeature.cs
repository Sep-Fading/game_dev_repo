// Designed by KINEMATION, 2024.

using KINEMATION.KAnimationCore.Runtime.Rig;

using System;
using UnityEngine;

namespace KINEMATION.RetargetPro.Runtime.Features
{
    [Serializable]
    public abstract class RetargetFeature : ScriptableObject, IRigUser
    {
        [HideInInspector] public KRig sourceRig;
        [HideInInspector] public KRig targetRig;
        
        [Tooltip("Influence of this feature. If 0 - disabled, 1 - enabled.")]
        [Range(0f, 1f)] public float featureWeight = 1f;
        [NonSerialized] public bool drawGizmos = false;
        
        public virtual RetargetFeatureState CreateFeatureState()
        {
            return null;
        }

        public KRig GetRigAsset()
        {
            return targetRig;
        }
        
#if UNITY_EDITOR
        public virtual bool GetStatus()
        {
            return true;
        }

        public virtual string GetErrorMessage()
        {
            return "";
        }
        
        public virtual void OnRigUpdated()
        {
        }

        public virtual void MapChains()
        {
            
        }

        public virtual string GetDisplayName()
        {
            return GetType().Name;
        }
#endif
    }
}