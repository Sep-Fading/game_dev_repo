// Designed by KINEMATION, 2024.

using KINEMATION.KAnimationCore.Runtime.Rig;
using UnityEngine;

namespace KINEMATION.RetargetPro.Runtime.Features
{
    public class RetargetUtility
    {
        public static KTransformChain GetTransformChain(KRigComponent rigComponent, KRig rigAsset, 
            KRigElementChain chain)
        {
            if (rigComponent == null || rigAsset == null)
            {
                return null;
            }
            
            if (chain == null)
            {
                return null;
            }

            KTransformChain output = new KTransformChain();

            foreach (var element in chain.elementChain)
            {
                Transform bone = rigComponent.GetRigTransform(element.name);

                if (bone == null)
                {
                    Debug.LogWarning($"Failed to find {element.name} of {rigAsset.name} {chain.chainName}");
                    continue;
                }
                
                output.transformChain.Add(bone);
            }
            
            return output;
        }

        public static bool IsNameMatching(string from, string to)
        {
            from = from.ToLower();
            to = to.ToLower();
            
            bool isToRight = to.Contains("_r") || to.Contains(".r") || to.Contains("right") || to.Contains(" r");
            bool isToLeft = to.Contains("_l") || to.Contains(".l") || to.Contains("left") || to.Contains(" l");
            
            bool isFromRight = from.Contains("_r") || from.Contains(".r") || from.Contains("right");
            bool isFromLeft = from.Contains("_l") || from.Contains(".l") || from.Contains("left");

            bool sameSide = isToRight && isFromRight || isToLeft && isFromLeft;
            
            if (to.Contains("root"))
            {
                return from.EndsWith("root");
            }

            if (to.Contains("pelvis") || to.Contains("hip"))
            {
                return from.Contains("pelvis") || from.Contains("hip");
            }
            
            if (to.Contains("lowerleg"))
            {
                return sameSide && from.Contains("lowerleg");
            }

            if (to.Contains("upperleg") || to.Contains("shin") || to.Contains("thigh") || to.Contains("leg"))
            {
                return sameSide && (from.Contains("upperleg") || from.Contains("shin") || from.Contains("thigh") 
                       || from.Contains("leg"));
            }

            if (to.Contains("foot") || to.Contains("ball"))
            {
                return sameSide && (from.Contains("foot") || from.Contains("ball"));
            }

            if (to.Contains("spine")) return from.Contains("spine");

            if (to.Contains("neck")) return from.Contains("neck");
            if (to.Contains("head")) return from.Contains("head");

            if (to.Contains("clavicle") || to.Contains("shoulder"))
            {
                return sameSide && (from.Contains("clavicle") || from.Contains("shoulder"));
            }

            if (to.Contains("upperarm")) 
            {
                return sameSide && from.Contains("upperarm");
            }

            if (to.Contains("lowerarm") || to.Contains("forearm"))
            {
                return sameSide && (from.Contains("lowerarm") || from.Contains("forearm"));
            }

            if (to.Contains("index")) return sameSide && from.Contains("index");
            if (to.Contains("middle")) return sameSide && from.Contains("middle");
            if (to.Contains("ring"))
            {
                Debug.Log($"{from}, {to}: {sameSide && from.Contains("ring")}");
                return sameSide && from.Contains("ring");
            }
            if (to.Contains("pinky")) return sameSide && from.Contains("pinky");
            if (to.Contains("thumb")) return sameSide && from.Contains("thumb");
            if (to.Contains("finger")) return sameSide && from.Contains("finger");

            if (to.Contains("hand") || to.Contains("palm"))
            {
                return sameSide && (from.Contains("hand") || from.Contains("palm"));
            }

            if (to.Contains("tail")) return from.Contains("tail");
            return false;
        }
    }
}