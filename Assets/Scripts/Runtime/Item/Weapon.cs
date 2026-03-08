// Designed by KINEMATION, 2025.

using KINEMATION.FPSAnimationFramework.Runtime.Camera;
using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.FPSAnimationFramework.Runtime.Playables;
using KINEMATION.FPSAnimationFramework.Runtime.Recoil;
using KINEMATION.ProceduralRecoilAnimationSystem.Runtime;
using KINEMATION.KAnimationCore.Runtime.Input;

using Demo.Scripts.Runtime.AttachmentSystem;

using System.Collections.Generic;
using Demo.Scripts.Runtime.Character;
using KINEMATION.FPSAnimationPack.Scripts.Sounds;
using KINEMATION.FPSAnimationPack.Scripts.Weapon;
using UnityEngine;

namespace Demo.Scripts.Runtime.Item
{
    public class Weapon : FPSItem
    {
        [Header("General")]
        [SerializeField] [Range(0f, 120f)] private float fieldOfView = 90f;
        
        [SerializeField] private FPSAnimationAsset reloadClip;
        [SerializeField] private FPSAnimationAsset reloadClipEmpty;
        [SerializeField] private FPSCameraAnimation cameraReloadAnimation;
        
        [SerializeField] private FPSAnimationAsset grenadeClip;
        [SerializeField] private FPSCameraAnimation cameraGrenadeAnimation;

        [Header("Recoil")]
        [SerializeField] private FPSAnimationAsset fireClip;
        [SerializeField] private RecoilAnimData recoilData;
        [SerializeField] private RecoilPatternSettings recoilPatternSettings;
        [SerializeField] private FPSCameraShake cameraShake;
        
        [Header("Firing")]
        [Min(0f)] [SerializeField] private float fireRate;
        [Min(1f)] [SerializeField] private int magCount; //amount of mags the gun comes with 
        [Min(1f)] [SerializeField] private int magSize; //(MAY NEED TO CHANGE THIS LATER TO BE DEPENDENT ON THE PLAYER AND NOT THE WEAPON)
        private int currentAmmo = 30;
        [SerializeField] private bool supportsAuto;
        [SerializeField] private bool supportsBurst;
        [SerializeField] private int burstLength;

        [Header("Attachments")] 
        
        [SerializeField]
        private AttachmentGroup<BaseAttachment> barrelAttachments = new AttachmentGroup<BaseAttachment>();
        
        [SerializeField]
        private AttachmentGroup<BaseAttachment> gripAttachments = new AttachmentGroup<BaseAttachment>();
        
        [SerializeField]
        private List<AttachmentGroup<ScopeAttachment>> scopeGroups = new List<AttachmentGroup<ScopeAttachment>>();
        
        //~ Controller references

        private FPSController _fpsController;
        private Animator _controllerAnimator;
        private UserInputController _userInputController;
        private IPlayablesController _playablesController;
        private FPSCameraController _fpsCameraController;
        
        private FPSAnimator _fpsAnimator;
        private FPSAnimatorEntity _fpsAnimatorEntity;

        private RecoilAnimation _recoilAnimation;
        private RecoilPattern _recoilPattern;
        
        //~ Controller references
        
        private Animator _weaponAnimator;
        private int _scopeIndex;
        
        private float _lastRecoilTime;
        private int _bursts;
        private FireMode _fireMode = FireMode.Semi;
        
        private static readonly int CurveEquip = Animator.StringToHash("CurveEquip");
        private static readonly int CurveUnequip = Animator.StringToHash("CurveUnequip");

        private void OnActionEnded()
        {
            if (_fpsController == null) return;
            _fpsController.ResetActionState();
        }

        protected void UpdateTargetFOV(bool isAiming)
        {
            float fov = fieldOfView;
            float sensitivityMultiplier = 1f;
            
            if (isAiming && scopeGroups.Count != 0)
            {
                var scope = scopeGroups[_scopeIndex].GetActiveAttachment();
                fov *= scope.aimFovZoom;

                sensitivityMultiplier = scopeGroups[_scopeIndex].GetActiveAttachment().sensitivityMultiplier;
            }

            _userInputController.SetValue("SensitivityMultiplier", sensitivityMultiplier);
            _fpsCameraController.UpdateTargetFOV(fov);
        }

        protected void UpdateAimPoint()
        {
            if (scopeGroups.Count == 0) return;

            var scope = scopeGroups[_scopeIndex].GetActiveAttachment().aimPoint;
            _fpsAnimatorEntity.defaultAimPoint = scope;
        }
        
        protected void InitializeAttachments()
        {
            foreach (var attachmentGroup in scopeGroups)
            {
                attachmentGroup.Initialize(_fpsAnimator);
            }
            
            _scopeIndex = 0;
            if (scopeGroups.Count == 0) return;

            UpdateAimPoint();
            UpdateTargetFOV(false);
        }
        
        public override void OnEquip(GameObject parent)
        {
            if (parent == null) return;
            
            _fpsAnimator = parent.GetComponent<FPSAnimator>();
            _fpsAnimatorEntity = GetComponent<FPSAnimatorEntity>();
            
            _fpsController = parent.GetComponent<FPSController>();
            _weaponAnimator = GetComponentInChildren<Animator>();
            
            _controllerAnimator = parent.GetComponent<Animator>();
            _userInputController = parent.GetComponent<UserInputController>();
            _playablesController = parent.GetComponent<IPlayablesController>();
            _fpsCameraController = parent.GetComponentInChildren<FPSCameraController>();

            if (overrideController != _controllerAnimator.runtimeAnimatorController)
            {
                _playablesController.UpdateAnimatorController(overrideController);
            }
            
            InitializeAttachments();
            
            _recoilAnimation = parent.GetComponent<RecoilAnimation>();
            _recoilPattern = parent.GetComponent<RecoilPattern>();
            
            _fpsAnimator.LinkAnimatorProfile(gameObject);
            
            barrelAttachments.Initialize(_fpsAnimator);
            gripAttachments.Initialize(_fpsAnimator);
            
            _recoilAnimation.Init(recoilData, fireRate, _fireMode);

            if (_recoilPattern != null)
            {
                _recoilPattern.Init(recoilPatternSettings);
            }
            
            _fpsAnimator.LinkAnimatorLayer(equipMotion);
        }

        public override void OnUnEquip()
        {
            _fpsAnimator.LinkAnimatorLayer(unEquipMotion);
        }

        public override bool OnAimPressed()
        {
            _userInputController.SetValue(FPSANames.IsAiming, true);
            UpdateTargetFOV(true);
            _recoilAnimation.isAiming = true;
            
            return true;
        }

        public override bool OnAimReleased()
        {
            _userInputController.SetValue(FPSANames.IsAiming, false);
            UpdateTargetFOV(false);
            _recoilAnimation.isAiming = false;
            
            return true;
        }

        public override bool OnFirePressed()
        {
            if (currentAmmo > 0)
            {
                OnFire();
                // Do not allow firing faster than the allowed fire rate.
                if (Time.unscaledTime - _lastRecoilTime < 60f / fireRate)
                {
                    return false;
                }
            
                _lastRecoilTime = Time.unscaledTime;
                _bursts = burstLength;
            
                return true;
            }
            return false;
        }

        public override bool OnFireReleased()
        {
            if (_recoilAnimation != null)
            {
                _recoilAnimation.Stop();
            }
            
            if (_recoilPattern != null)
            {
                _recoilPattern.OnFireEnd();
            }
            
            CancelInvoke(nameof(OnFire));
            return true;
        }
        
        public override bool OnReload()
        {
            if (magCount > 0)
            {
                if (currentAmmo == 0)
                {
                    if (!FPSAnimationAsset.IsValid(reloadClipEmpty))
                    {
                        return false;
                    }
                    _playablesController.PlayAnimation(reloadClipEmpty, 0f);
            
                    if (_weaponAnimator != null)
                    {
                        _weaponAnimator.Rebind();
                        _weaponAnimator.Play("Reload_Empty", 0);
                        _playablesController.PlayAnimation(reloadClipEmpty);
                    }

                    if (_fpsCameraController != null)
                    {
                        _fpsCameraController.PlayCameraAnimation(cameraReloadAnimation);
                    }
            
                    Invoke(nameof(OnActionEnded), reloadClipEmpty.clip.length * 0.85f);
                }
                else
                {
                    if (!FPSAnimationAsset.IsValid(reloadClip))
                    {
                        return false;
                    }
                    _playablesController.PlayAnimation(reloadClip, 0f);
            
                    if (_weaponAnimator != null)
                    {
                        _weaponAnimator.Rebind();
                        _weaponAnimator.Play("Reload_Tac", 0);
                        _playablesController.PlayAnimation(reloadClip);
                    }

                    if (_fpsCameraController != null)
                    {
                        _fpsCameraController.PlayCameraAnimation(cameraReloadAnimation);
                    }
            
                    Invoke(nameof(OnActionEnded), reloadClip.clip.length * 0.85f);
                }
                OnFireReleased();
                currentAmmo = magSize;
                magCount --;
                return true;
            }

            return false;
        }

        public override bool OnGrenadeThrow()
        {
            if (!FPSAnimationAsset.IsValid(grenadeClip))
            {
                return false;
            }

            _playablesController.PlayAnimation(grenadeClip, 0f);
            
            if (_fpsCameraController != null)
            {
                _fpsCameraController.PlayCameraAnimation(cameraGrenadeAnimation);
            }
            
            Invoke(nameof(OnActionEnded), grenadeClip.clip.length * 0.8f);
            return true;
        }
        
        private void OnFire()
        {
            Debug.Log(currentAmmo);
            if(currentAmmo > 0)
            {
                currentAmmo--;
                if (_weaponAnimator != null)
                {
                    _weaponAnimator.Play("Fire", 0, 0f);
                }
            
                _fpsCameraController.PlayCameraShake(cameraShake);
                GetComponentInChildren<FPSWeaponSound>().PlayFireSound();
            
            
                if(fireClip != null) _playablesController.PlayAnimation(fireClip);

                if (_recoilAnimation != null && recoilData != null)
                {
                    _recoilAnimation.Play();
                }

                if (_recoilPattern != null)
                {
                    _recoilPattern.OnFireStart();
                }

                if (_recoilAnimation.fireMode == FireMode.Semi)
                {
                    Invoke(nameof(OnFireReleased), 60f / fireRate);
                    return;
                }
            
                if (_recoilAnimation.fireMode == FireMode.Burst)
                {
                    _bursts--;
                
                    if (_bursts == 0)
                    {
                        OnFireReleased();
                        return;
                    }
                }
            
                Invoke(nameof(OnFire), 60f / fireRate);
            }
        }

        public override void OnCycleScope()
        {
            if (scopeGroups.Count == 0) return;
            
            _scopeIndex++;
            _scopeIndex = _scopeIndex > scopeGroups.Count - 1 ? 0 : _scopeIndex;
            
            UpdateAimPoint();
            UpdateTargetFOV(true);
        }

        private void CycleFireMode()
        {
            if (_fireMode == FireMode.Semi && supportsBurst)
            {
                _fireMode = FireMode.Burst;
                _bursts = burstLength;
                return;
            }

            if (_fireMode != FireMode.Auto && supportsAuto)
            {
                _fireMode = FireMode.Auto;
                return;
            }

            _fireMode = FireMode.Semi;
        }
        
        public override void OnChangeFireMode()
        {
            CycleFireMode();
            GetComponentInChildren<FPSWeaponSound>().PlayFireModeSound();
            _recoilAnimation.fireMode = _fireMode;
        }

        public override void OnAttachmentChanged(int attachmentTypeIndex)
        {
            if (attachmentTypeIndex == 1)
            {
                barrelAttachments.CycleAttachments(_fpsAnimator);
                return;
            }

            if (attachmentTypeIndex == 2)
            {
                gripAttachments.CycleAttachments(_fpsAnimator);
                return;
            }

            if (scopeGroups.Count == 0) return;
            scopeGroups[_scopeIndex].CycleAttachments(_fpsAnimator);
            UpdateAimPoint();
        }

        public void AddMagazineCount(int n)
        {
            magCount += n;
        }
        public int GetMagazineCount()
        {
            return magCount;
        }
    }
}