using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRFPSKit
{
    /// <summary>
    /// This script is a workaround for an issue in the XR Interaction Toolkit where item tracking
    /// lags behind by a frame when using CharacterMovement with continuous movement.
    /// </summary>
    [RequireComponent(typeof(HandPresence))]
    public class HandPresenceCharacterMovementInteractionTrackingFixer : MonoBehaviour
    {
        private Vector3 _parentedPositionLastFrame;
        
        // Update is called once per frame
        void Update()
        {
            Vector3 parentedPositionDelta = transform.parent.position - _parentedPositionLastFrame;
            ApplyParentMovementToInteractors(transform, parentedPositionDelta);
        }
        
        
        /// <summary>
        /// All movement of parent Character controller is applied to all held interactables, so they track along player movement and dont lag behind.
        /// We also need to account for interactables held by multiple interactors, so we divide the movement by the amount of interactors holding it.
        /// We do this recursively for all interactables held by interactors, in case of nested interactables (like weapon attachments).
        /// </summary>
        /// <param name="parentOfInteractors"></param>
        /// <param name="deltaPosition"></param>
        private void ApplyParentMovementToInteractors(Transform parentOfInteractors, Vector3 deltaPosition, int contributingInteractorAmount = 1)
        {
            foreach (var interactor in parentOfInteractors.GetComponentsInChildren<XRBaseInteractor>())
            {
                foreach (var selectedInteractable in interactor.interactablesSelected)
                {
                    if (selectedInteractable is not XRGrabInteractable) continue;
                    if (selectedInteractable.transform.GetComponent<Rigidbody>() is not Rigidbody interactableRigidbody) continue;

                    int interactableContributingInteractorAmount = HandPresence.CountRigidbodyInteractors(interactableRigidbody);
                    if(interactableContributingInteractorAmount > contributingInteractorAmount)
                        contributingInteractorAmount = interactableContributingInteractorAmount;
                    
                    //Divide by the amount of interactors contributing
                    Vector3 newPosition = interactableRigidbody.transform.position + deltaPosition / contributingInteractorAmount;
                    interactableRigidbody.transform.position = newPosition;
                    interactableRigidbody.MovePosition(newPosition);
                    
                    ApplyParentMovementToInteractors(selectedInteractable.transform, deltaPosition, contributingInteractorAmount);
                }
            }
            
            _parentedPositionLastFrame = transform.parent.position;
        }
    }
}
