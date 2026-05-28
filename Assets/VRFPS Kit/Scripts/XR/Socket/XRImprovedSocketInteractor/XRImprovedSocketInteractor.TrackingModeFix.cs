using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRFPSKit
{
    /// <summary>
    /// Extended XRSocketInteractor with various fixes for smooth interactions.
    /// </summary>
    public partial class XRImprovedSocketInteractor
    {
        private XRBaseInteractable.MovementType _previousMovementType;

        private void TrackingModeFix_SubscribeEvents()
        {
            //Tracking mode fix events
            selectEntered.AddListener(RecordedGrabInteractableMovementType);
            selectExited.AddListener(RevertGrabInteractableMovementType);
        }
        
        private void RecordedGrabInteractableMovementType(SelectEnterEventArgs args)
        {
            if(args.interactableObject is not XRGrabInteractable grabbable) return;
            
            _previousMovementType = grabbable.movementType;
        }
        
        private void RevertGrabInteractableMovementType(SelectExitEventArgs args)
        {
            if (!FixInstantAttachTrackingMode) return;
            if (args.interactableObject is not XRGrabInteractable grabbable) return;
            
            //Revert grabbable movement type to previous value
            grabbable.movementType = _previousMovementType;
            
            //Reset kinematic state in case it was changed by socket movementType (usually to kinematic which makes item kinematic)
            grabbable.GetComponent<Rigidbody>().isKinematic = false;
            //Also fix the internal wasKinematic value through reflection
            SetGrabbableInternalValue_WasKinematic(grabbable, false);
        }
        
        private static void SetGrabbableInternalValue_WasKinematic(XRGrabInteractable grabbable, bool wasKinematic)
        {
            FieldInfo wasKinematicField = typeof(XRGrabInteractable).GetField("m_WasKinematic", 
                BindingFlags.NonPublic | BindingFlags.Instance);
                
            //Ensure the wasKinematic field was found
            if (wasKinematicField == null)
            { 
                Debug.LogError("Field m_WasKinematic could not be found through reflection on " +
                               "XRGrabInteractable. Are you using a newer version of the XR Interaction Toolkit?" +
                               "This will likely result in grabables staying kinematic after releasing them.");
                return;
            }
            
            wasKinematicField.SetValue(grabbable, wasKinematic);
        }
    }
}