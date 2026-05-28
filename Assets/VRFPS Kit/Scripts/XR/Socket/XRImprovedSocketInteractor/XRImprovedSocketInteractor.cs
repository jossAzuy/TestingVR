using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRFPSKit
{
    /// <summary>
    /// Extended XRSocketInteractor with various fixes for smooth interactions.
    /// </summary>
    public partial class XRImprovedSocketInteractor : XRSocketInteractor
    {
        //Fixes XRSocketInteractor failing to restore Rigidbody kinematic and movement mode state when using instantaneous attach.
        private const bool FixInstantAttachTrackingMode = true;
        
        [Space]
        [Header("XRImprovedSocketInteractor Settings")]
        
        [Tooltip("Prevents sockets from auto-attaching nearby interactables unless they were previously held by a player.")]
        public bool requirePriorPlayerHold = true;
        
        [Tooltip("Prevents collision with the rigidbody that is attached in socket interaction.")] [FormerlySerializedAs("preventAttachedInteractableCollision")] 
        public bool suppressAttachedCollision = true;
        
        [Tooltip("Instantly attaches any interactable that is hovered. Will steal interactables from hands")]
        public bool stealInteractableOnHover = false;

        protected override void Awake()
        {
            base.Awake();
            
            HeldFilter_SubscribeEvents();
            TrackingModeFix_SubscribeEvents();
            AttachedInteractableCollisionFixer_SubscribeEvents();
            InstantAttach_SubscribeEvents();
        }

        protected override void Start()
        {
            //Unity ruined starting selected interactable, we want it to use SelectEnterUnconditionally() so it ignores CanSelect()
            XRBaseInteractable startingSelectedInteractableCopy = startingSelectedInteractable;
            startingSelectedInteractable = null;
            
            //Call regular StartSelectInteractable
            base.Start();
            
            if (interactionManager != null && startingSelectedInteractableCopy != null)
                StartCoroutine(SelectWhenReady(startingSelectedInteractableCopy));
        }

        private IEnumerator SelectWhenReady(XRBaseInteractable interactable)
        {
            //Wait until interactable is active
            while (!interactable.gameObject.activeInHierarchy)
                yield return null;

            //Wait one extra frame to ensure XR finished registering it
            yield return null;

            interactionManager.SelectEnterUnconditionally(this, interactable);
        }
    }
}