// Designed by KINEMATION, 2024.

using KINEMATION.KAnimationCore.Runtime.Rig;
using KINEMATION.RetargetPro.Runtime;
using KINEMATION.RetargetPro.Runtime.Features;
using KINEMATION.RetargetPro.Runtime.Features.IKRetargeting;

using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

using FuzzySharp;

namespace KINEMATION.RetargetPro.Editor
{
    public class ProfileContextMenu
    {
        private static KRig[] GetRigAssets()
        {
            if (Selection.objects.Length < 2)
            {
                return null;
            }

            List<KRig> result = null;

            foreach (var selection in Selection.objects)
            {
                if(selection is not KRig) continue;

                result ??= new List<KRig>();
                result.Add(selection as KRig);

                if (result.Count == 2) break;
            }
            
            return result?.ToArray();
        }
        
        [MenuItem("Assets/Create Retarget Profile", true)]
        private static bool ValidateCreateRetargetProfile()
        {
            return GetRigAssets() != null;
        }

        [MenuItem("Assets/Create Retarget Profile")]
        private static void CreateRetargetProfile()
        {
            KRig[] rigAssets = GetRigAssets();
            if (rigAssets == null) return;

            string path = AssetDatabase.GetAssetPath(rigAssets[0]);
            path = path.Substring(0, path.LastIndexOf('/'));

            path = $"{path}/Retarget_{rigAssets[0].name}_{rigAssets[1].name}.asset";

            RetargetProfile newProfile = (RetargetProfile) ScriptableObject.CreateInstance(typeof(RetargetProfile));

            newProfile.sourceRig = rigAssets[0];
            newProfile.targetRig = rigAssets[1];
            newProfile.retargetFeatures = new List<RetargetFeature>();
            
            Undo.RegisterCreatedObjectUndo(newProfile, "Create Retarget Profile");
            AssetDatabase.CreateAsset(newProfile, AssetDatabase.GenerateUniqueAssetPath(path));

            List<KRigElementChain> sourceChains = new List<KRigElementChain>();
            foreach (var chain in rigAssets[0].rigElementChains)
            {
                sourceChains.Add(chain);
            }

            foreach (var targetChain in rigAssets[1].rigElementChains)
            {
                int bestScore = -1;
                KRigElementChain sourceChain = null;
                
                foreach (var chain in sourceChains)
                {
                    string a = targetChain.chainName.Replace("mixamo", "");
                    string b = chain.chainName.Replace("mixamo", "");

                    a = a.Replace("CC_", "");
                    b = b.Replace("CC_", "");

                    a = a.Replace("Proximal", "");
                    b = b.Replace("Proximal", "");
                    
                    a = Regex.Replace(a, @"\d", "");
                    b = Regex.Replace(b, @"\d", "");
                    
                    int score = Fuzz.WeightedRatio(a, b) + Fuzz.PartialRatio(a, b);
                    if (score > bestScore)
                    {
                        sourceChain = chain;
                        bestScore = score;
                    }
                }
                
                if (bestScore < 120) sourceChain = null;
                
                IKRetargetFeature feature = (IKRetargetFeature) 
                    ScriptableObject.CreateInstance(typeof(IKRetargetFeature));
                    
                feature.ikWeight = 0f;

                feature.name = feature.GetType().Name;
                feature.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;

                feature.sourceRig = rigAssets[0];
                feature.targetRig = rigAssets[1];

                feature.sourceChain = new KRigElementChain
                {
                    chainName = "None"
                };

                if (sourceChain != null)
                {
                    feature.sourceChain.chainName = sourceChain.chainName;
                    foreach (var element in sourceChain.elementChain) feature.sourceChain.elementChain.Add(element);
                    sourceChains.Remove(sourceChain);
                }
                
                feature.targetChain = new KRigElementChain
                {
                    chainName = targetChain.chainName
                };
                foreach (var element in targetChain.elementChain) feature.targetChain.elementChain.Add(element);
                
                Undo.RegisterCreatedObjectUndo(feature, "Create Retarget Feature");
                newProfile.retargetFeatures.Add(feature);
                    
                AssetDatabase.AddObjectToAsset(feature, newProfile);
            }
            
            EditorUtility.SetDirty(newProfile);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Assets/Open with Retargeting Window", true)]
        private static bool ValidateOpenRetargetProfile()
        {
            return Selection.activeObject is RetargetProfile;
        }

        [MenuItem("Assets/Open with Retargeting Window")]
        private static void OpenRetargetProfile()
        {
            RetargetProWindow window = EditorWindow.GetWindow<RetargetProWindow>(false, "Retarget Pro");

            window.retargetAnimBaker.retargetProfile = Selection.activeObject as RetargetProfile;
            
            window.Show();
            window.Focus();
        }
    }
}