using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VRFPSKit.Input;

namespace VRFPSKit
{
    /// <summary>
    /// Keeps firearm ammo effectively infinite while still forcing a manual reload input every X shots.
    /// Optional VR gesture requires moving the controller down and then up while holding reload input.
    /// </summary>
    [RequireComponent(typeof(Firearm), typeof(XRGrabInteractable))]
    public class InfiniteAmmoReloadInput : MonoBehaviour
    {
        [Header("Reload Rules")]
        public int shotsPerReload = 15;
        public XRHeldInputAction reloadInput;
        [Range(0f, 1f)] public float reloadInputThreshold = 0.55f;

        [Header("VR Gesture (Optional)")]
        public bool requireDownUpGesture = true;
        public bool requireReloadInputHeldForGesture = true;
        public float minDownDistance = 0.08f;
        public float minUpDistance = 0.08f;
        public float gestureTimeout = 1.25f;

        [Header("Fire Lock While Reload Needed")]
        public bool lockFireModeToSafeWhenReloadNeeded = true;

        private Firearm _firearm;
        private XRGrabInteractable _grabbable;
        private FirearmCyclingAction _cyclingAction;

        private Cartridge _refillCartridge;
        private bool _hasRefillCartridge;

        private int _shotsSinceReload;
        private bool _reloadRequired;

        private bool _forcedSafeMode;
        private FireMode _modeBeforeForcedSafe;

        private bool _gestureTracking;
        private bool _gestureWentDown;
        private float _gestureStartTime;
        private float _gestureStartY;
        private float _gestureMinY;
        private bool _reloadInputWasPressedLastFrame;

        public bool ReloadRequired => _reloadRequired;

        private void Awake()
        {
            _firearm = GetComponent<Firearm>();
            _grabbable = GetComponent<XRGrabInteractable>();
            _cyclingAction = GetComponent<FirearmCyclingAction>();

            _firearm.ShootEvent += OnShoot;
            if (_cyclingAction != null)
                _cyclingAction.ChamberRoundEvent += OnChamberRound;
        }

        private void Start()
        {
            ResolveMagazineReference();
            EnsureRefillCartridge();
            TryTopUpMagazine();
        }

        private void OnDestroy()
        {
            if (_firearm != null)
                _firearm.ShootEvent -= OnShoot;

            if (_cyclingAction != null)
                _cyclingAction.ChamberRoundEvent -= OnChamberRound;
        }

        private void Update()
        {
            ResolveMagazineReference();

            if (!_grabbable.isSelected)
            {
                ResetGestureState();
                return;
            }

            InputAction input = reloadInput.GetActionForPrimaryHand(_grabbable);
            bool reloadPressed = IsReloadPressed(input, reloadInputThreshold);

            bool reloadInputPressedThisFrame = reloadPressed && !_reloadInputWasPressedLastFrame;
            _reloadInputWasPressedLastFrame = reloadPressed;

            if (!_reloadRequired)
                return;

            if (!requireDownUpGesture)
            {
                if (reloadInputPressedThisFrame)
                    PerformReload();
                return;
            }

            if (requireReloadInputHeldForGesture && !reloadPressed)
            {
                ResetGestureState();
                return;
            }

            if (!requireReloadInputHeldForGesture && !reloadPressed)
                return;

            Transform handTransform = GetPrimaryHandTransform();
            if (handTransform == null)
                return;

            TrackGesture(handTransform.position.y);
        }

        private void OnShoot(Cartridge firedCartridge)
        {
            if (shotsPerReload <= 0)
                return;

            CacheRefillCartridge(firedCartridge);
            _shotsSinceReload++;

            if (_shotsSinceReload >= shotsPerReload)
                SetReloadRequired();
        }

        private void OnChamberRound()
        {
            // Refill one round after each chamber event so the magazine never runs out.
            TryTopUpMagazine(1);
        }

        private void SetReloadRequired()
        {
            _reloadRequired = true;

            if (!lockFireModeToSafeWhenReloadNeeded)
                return;

            ForceSafeMode();
        }

        private void PerformReload()
        {
            _shotsSinceReload = 0;
            _reloadRequired = false;

            if (_forcedSafeMode)
            {
                _firearm.currentFireMode = _modeBeforeForcedSafe;
                _forcedSafeMode = false;
            }

            EnsureRefillCartridge();
            TryTopUpMagazine();

            if ((_firearm.chamberCartridge.IsNull() || !_firearm.chamberCartridge.CanFire()) && _hasRefillCartridge)
                _firearm.chamberCartridge = _refillCartridge;

            if (_cyclingAction != null && _cyclingAction.IsLockedBack())
                _cyclingAction.TryUnlockAction();

            ResetGestureState();
        }

        private void ForceSafeMode()
        {
            if (_firearm.currentFireMode == FireMode.Safe)
                return;

            _modeBeforeForcedSafe = _firearm.currentFireMode;
            _firearm.currentFireMode = FireMode.Safe;
            _forcedSafeMode = true;
        }

        private void TryTopUpMagazine(int maxToAdd = int.MaxValue)
        {
            if (_firearm.magazine == null)
                return;

            if (!EnsureRefillCartridge())
                return;

            int added = 0;
            while (!_firearm.magazine.IsFull() && added < maxToAdd)
            {
                _firearm.magazine.AddCartridgeToTop(_refillCartridge);
                added++;
            }
        }

        private bool EnsureRefillCartridge()
        {
            if (_hasRefillCartridge && _refillCartridge.CanFire())
                return true;

            if (_firearm.chamberCartridge.CanFire())
            {
                CacheRefillCartridge(_firearm.chamberCartridge);
                return true;
            }

            if (_firearm.magazine != null)
            {
                Cartridge top = _firearm.magazine.GetTopCartridge();
                if (top.CanFire())
                {
                    CacheRefillCartridge(top);
                    return true;
                }

                foreach (Cartridge preset in _firearm.magazine.cartridgePreset)
                {
                    if (!preset.CanFire())
                        continue;

                    CacheRefillCartridge(preset);
                    return true;
                }
            }

            return false;
        }

        private void CacheRefillCartridge(Cartridge cartridge)
        {
            if (!cartridge.CanFire())
                return;

            _refillCartridge = cartridge;
            _hasRefillCartridge = true;
        }

        private static bool IsReloadPressed(InputAction action, float threshold)
        {
            if (action == null)
                return false;

            // Value actions (Axis) are common in XR for grip/trigger, but button actions are also supported.
            if (action.expectedControlType == "Axis")
                return action.ReadValue<float>() >= threshold;

            return action.IsPressed();
        }

        private void ResolveMagazineReference()
        {
            if (_firearm.magazine != null)
                return;

            foreach (Magazine magazine in GetComponentsInChildren<Magazine>(true))
            {
                if (magazine == null)
                    continue;

                _firearm.magazine = magazine;
                return;
            }
        }

        private Transform GetPrimaryHandTransform()
        {
            if (_grabbable.interactorsSelecting.Count == 0)
                return null;

            return _grabbable.interactorsSelecting[0].transform;
        }

        private void TrackGesture(float currentY)
        {
            if (!_gestureTracking)
            {
                _gestureTracking = true;
                _gestureWentDown = false;
                _gestureStartTime = Time.time;
                _gestureStartY = currentY;
                _gestureMinY = currentY;
                return;
            }

            if (Time.time - _gestureStartTime > gestureTimeout)
            {
                ResetGestureState();
                return;
            }

            if (currentY < _gestureMinY)
                _gestureMinY = currentY;

            float downTravel = _gestureStartY - _gestureMinY;
            if (!_gestureWentDown && downTravel >= minDownDistance)
                _gestureWentDown = true;

            if (!_gestureWentDown)
                return;

            float upTravel = currentY - _gestureMinY;
            if (upTravel >= minUpDistance)
                PerformReload();
        }

        private void ResetGestureState()
        {
            _gestureTracking = false;
            _gestureWentDown = false;
            _gestureStartTime = 0f;
            _gestureStartY = 0f;
            _gestureMinY = 0f;
        }
    }
}