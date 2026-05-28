using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRFPSKit
{
    /// <summary>
    /// Represents a firearm action that operates by cycling (As opposed to say breaking open)
    /// </summary>
    [RequireComponent(typeof(Firearm))]
    public class FirearmCyclingAction : MonoBehaviour
    {
        [Header("Configure action events")]
        [Range(0, 1)] public float inBattery01 = .05f;
        [Range(0, 1)] public float roundEjectAction01 = .6f;
        [Range(0, 1)] public float roundFeedAction01 = .85f;
        [Range(0, 1)] public float hammerResetAction01 = 0.25f;
        
        [Space]
        [Header("Action properties")]
        
        [Tooltip("Does the action lock back when the magazine is empty?")]
        public bool lockOnEmptyMag = true;
        
        [Tooltip("Turn this off for bolt action")] 
        public bool automaticCycling = true;
        
        [Tooltip("Decides how quick action moves back in to battery (essentially spring strength)")]
        public int roundsPerMinute;
        
        [Space]
        [Header("Action state")]
        
        [SerializeField] 
        private float actionPosition01;
        
        [SerializeField] 
        private bool isLockedBack;
        
        [Tooltip("LoadingCartridge represents cartridge that has been removed from top of magazine but isn't yet loaded in chamber. Will be transfered to Firearm.chamberCartridge one loading is complete.")] [SerializeField] 
        private Cartridge loadingCartridge;

        public event Action FeedRoundEvent;
        public event Action LoadRoundEvent;
        public event Action ChamberRoundEvent;

        public FirearmCyclingActionInteractable ActionInteractable => _actionInteractable;
        public CartridgeEjector Ejector => _ejector;
        
        private bool _readyToEject;
        private bool _roundFed;

        private Firearm _firearm;
        private FirearmCyclingActionInteractable _actionInteractable;
        private CartridgeEjector _ejector;
        
        private void Update()
        {
            UpdateAction();
        }

        public void UpdateAction()
        {
            if (isLockedBack)
                actionPosition01 = 1;

            _firearm.inBattery = (actionPosition01 <= inBattery01);
            
            //Try Recock hammer
            if(actionPosition01 > hammerResetAction01)
                _firearm.isHammerCocked = true;
            
            HandleChamber();
            TryEmptyLockAction();
            ApplySpringMovement();
        }
        
        public void HandleChamber()
        {
            //Cartridge Ejection
            if (actionPosition01 < roundEjectAction01) _readyToEject = true;
            if (actionPosition01 > roundEjectAction01 && _readyToEject)
            {
                _readyToEject = false;
                
                _ejector?.TryEjectChamber();
            }

            //Feed Cartridge (Action has been moved back far enough to feed a cartridge)
            if (actionPosition01 > roundFeedAction01 && actionPosition01 > roundEjectAction01 && //Action moved past feed & eject point
                _firearm.magazine && !_firearm.magazine.GetTopCartridge().IsNull() && //There is another round in the magazine
                _firearm.chamberCartridge.IsNull() && //Chamber is empty
                loadingCartridge.IsNull() && //loading cartridge is empty
                !_roundFed) //Round has not been fed yet
            {
                _roundFed = true;
                FeedRoundEvent?.Invoke();
            }
            
            //Feed cartridge (Begin moving round in to chamber)
            if (actionPosition01 < roundFeedAction01 && _roundFed)
            {
                LoadRoundEvent?.Invoke();
                TryLoadFromMagazine();
            }
            
            //Feed cartridge (Begin moving round in to chamber)
            if (actionPosition01 < inBattery01)
            {
                if (FinishLoadChamber())
                    ChamberRoundEvent?.Invoke();
            }
        }
        
        /// <summary>
        /// When the action is moved back all the way back on an empty magazine, lock action back, also detach hand
        /// </summary>
        private void TryEmptyLockAction()
        {
            if (!lockOnEmptyMag) return;
            //Don't lock back twice
            if (isLockedBack) return;
            //Wait until action is all the way back
            if (actionPosition01 < 0.9f) return;
            
            //Don't lock back if magazine is missing
            if (_firearm.magazine == null) return;
            //Lock back when magazine is empty
            if (!_firearm.magazine.IsEmpty()) return;
            //Also chamber cartridge must be empty
            if (!_firearm.chamberCartridge.IsNull() && _firearm.chamberCartridge.CanFire()) return;
            //Also loading cartridge must be empty
            if (!loadingCartridge.IsNull() && loadingCartridge.CanFire()) return;
            
            isLockedBack = true;

            //Detach hand from potential action interactable
            if (_actionInteractable)
                _actionInteractable.ForceDetachSelectors();
        }

        private void ShootEvent(Cartridge cartridge)
        {
            if (!automaticCycling) return;
            
            actionPosition01 = 1;
            
            if (_actionInteractable)
                _actionInteractable.ForceDetachSelectors();
            
            //Make sure all action updates are performed right after shot
            //TODO might not be needed anymore
            UpdateAction();
        }

        public void TryUnlockAction()
        {
            if (!isLockedBack) return;
            
            isLockedBack = false;
            
            //Move action forward far enough that action won't lock again
            actionPosition01 = 0.7f;
        }

        private void ApplySpringMovement()
        {
            if (isLockedBack) return;
            //If has action interactable, and it is held, don't apply spring movement
            if (_actionInteractable && _actionInteractable._interactable.isSelected) return;

            //Fire rate is determined by how quick action goes back in to battery again after firing
            float roundsPerSecond = roundsPerMinute / 60f;
            actionPosition01 = Mathf.Clamp01(actionPosition01 - (roundsPerSecond * Time.deltaTime));
        }
        
        /// <summary>
        /// If possible, load chamber from magazine
        /// </summary>
        public void TryLoadFromMagazine()
        {
            //Cant load filled chamber
            if (!_firearm.chamberCartridge.IsNull()) return;
            if (!loadingCartridge.IsNull()) return;
            if (!_roundFed) return;
            //Cant load with empty mag
            if (_firearm.magazine == null) return;
            if (_firearm.magazine.IsEmpty()) return;
            
            loadingCartridge = _firearm.magazine.GetTopCartridge();//TODO magazine load error is here
            _firearm.magazine.RemoveCartridgeFromTop();
            _roundFed = false;
        }
        
        /// <summary>
        /// If possible, load chamber from magazine
        /// </summary>
        public bool FinishLoadChamber()
        {
            //Cant load filled chamber
            if (!_firearm.chamberCartridge.IsNull()) return false;
            if (loadingCartridge.IsNull()) return false;

            _firearm.chamberCartridge = loadingCartridge;
            loadingCartridge = Cartridge.Empty;

            return true;
        }
        
        private void Awake()
        {
            _firearm = GetComponent<Firearm>();
            _actionInteractable = GetComponentInChildren<FirearmCyclingActionInteractable>();
            
            _ejector = _firearm.GetComponentInChildren<CartridgeEjector>();
            if (_ejector == null)
            {
                Debug.LogError($"FirearmCyclingAction couldn't find a CartridgeEjector component on Firearm '{_firearm.gameObject.name}'");
                return;
            }

            _firearm.ShootEvent += (cartridge) =>
            {
                ShootEvent(cartridge);
            };
        }
        
        public float GetActionPosition01() => actionPosition01;
        
        public void SetActionPosition01(float value)
        {
            actionPosition01 = value;
        }
        
        public bool IsLockedBack() =>isLockedBack;
        
        public void SetLockedBack(bool value)
        {
            isLockedBack = value;
        }
        
        public Cartridge GetLoadingCartridge() => loadingCartridge;
        
        public bool ReadyToEject() => _readyToEject;
    }
}