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
    /// Prevents collision between the two rigidbodies that make up a socket interaction (interactor & interactable)
    /// </summary>
    public partial class XRImprovedSocketInteractor
    {
        private void InteractionPreventCollisions(SelectEnterEventArgs args)
        {
            //If the feature is disabled, return
            if (!suppressAttachedCollision) return;
                
            Rigidbody interactorRigidbody = GetComponentInParent<Rigidbody>();
            Rigidbody interactableRigidbody = args.interactableObject.transform.GetComponentInParent<Rigidbody>();
            
            //Make sure both rigidbodies exist
            if (!interactorRigidbody || !interactableRigidbody) return;
            
            UpdateCollisionLayer(interactorRigidbody, interactableRigidbody, true);
        }
        
        private void RevertInteractionPreventCollisions(SelectExitEventArgs args)
        {
            //If the feature is disabled, return
            if (!suppressAttachedCollision) return;
            
            Rigidbody interactorRigidbody = GetComponentInParent<Rigidbody>();
            Rigidbody interactableRigidbody = args.interactableObject.transform.GetComponentInParent<Rigidbody>();
            
            //Make sure both rigidbodies exist
            if (!interactorRigidbody || !interactableRigidbody) return;
            
            UpdateCollisionLayer(interactorRigidbody, interactableRigidbody, false);
        }
        
        private static void UpdateCollisionLayer(Rigidbody rb1, Rigidbody rb2, bool preventInterCollision)
        {
            //Update physics layer of all children
            foreach (Collider rb1Collider in rb1.GetComponentsInChildren<Collider>())
                foreach (Collider rb2Collider in rb2.GetComponentsInChildren<Collider>())
                {
                    //No need to prevent collisions with triggers
                    if (rb1Collider.isTrigger || rb2Collider.isTrigger) continue;
                        
                    Physics.IgnoreCollision(rb1Collider, rb2Collider, preventInterCollision);
                }
        }
        
        private void AttachedInteractableCollisionFixer_SubscribeEvents()
        {
            selectEntered.AddListener(InteractionPreventCollisions);
            selectExited.AddListener(RevertInteractionPreventCollisions);
        }
    }
}