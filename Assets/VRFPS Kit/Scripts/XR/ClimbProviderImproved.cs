using System.Reflection;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Climbing;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

namespace VRFPSKit
{
    /// <summary>
    /// Improved ClimbProvider so that gravity starts working after releasing the climb interactable.
    /// </summary>
    [RequireComponent(typeof(ContinuousMoveProvider))]
    public class ClimbProviderImproved : ClimbProvider
    {
        private ContinuousMoveProvider _moveProvider;      // your normal move provider
        // cache the FieldInfo once
        FieldInfo _verticalVelField;
        
        private bool hasResetGravity = false; // to prevent multiple calls to reset gravity

        protected override void Awake()
        {
            base.Awake();
            
            _moveProvider = GetComponent<ContinuousMoveProvider>();
            
            // find the private Vector3 m_VerticalVelocity field on ContinuousMoveProviderBase
            var type = _moveProvider.GetType();
            _verticalVelField = type.GetField(
                "m_GravityDrivenVelocity",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            
            Debug.Assert(_verticalVelField != null, "ContinuousMoveProviderBase.m_GravityDrivenVelocity could not be found! Are you using a newer version of UNITY XR INTERACTION TOOLKIT?");
        }

        protected override void Update()
        {
            base.Update();

            if (climbAnchorInteractable != null)
                hasResetGravity = false;
            
            if (climbAnchorInteractable == null && !hasResetGravity)
                ResetGravity();
        }

        private void ResetGravity()
        {
            //Set vertical velocity of the default move provider to force it to start applying gravity
            _verticalVelField.SetValue(
                _moveProvider,
                new Vector3(0, -.001f, 0)
            );
            
            hasResetGravity = true;
        }
    }
}
