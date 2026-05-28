using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

namespace VRFPSKit
{
    /// <summary>
    /// Instantly attaches any interactable that is hovered. Will steal interactables from hands.
    ///
    /// Works by calling TryInstantAttachInteractable() on hover enter, which checks if an interaction
    /// with the hovered interactable would be valid (filters, layers, etc) and if so steals and selects the interactable.
    /// </summary>
    public partial class XRImprovedSocketInteractor
    {
        //Small cooldown to prevent instant reattach after select exit. (Would make it impossible to remove an item from the socket)
        private const float ReattachCooldownDelay = .3f;    
        
        private float _lastHoverExitTime;
        private XRSocketInteractor _socket;
        private void TryInstantAttachInteractable(IXRSelectInteractable interactable)
        {
            //Return if instant attaching is disabled
            if (!stealInteractableOnHover) return;
                
            //Check if selection between this interactor and interactable is valid.
            //This ignores whether the interactable is already selected by another
            //interactor so we can steal it from the hand, but still checks filters
            //and other conditions that would prevent selection.
            if (!IsSelectionValid(_socket.interactionManager, _socket, interactable)) return;
            
            //Don't allow socket to hold more than 1
            if (_socket.interactablesSelected.Count > 0) return;
            //Cooldown for instant attach after select exit
            if (Time.time - _lastHoverExitTime < ReattachCooldownDelay) return;

            //Cancel any previous selection of this interactable (e.g. by hand) so we can steal it
            _socket.interactionManager.CancelInteractableSelection(interactable);
            //Instantly transfer magazine from hand to socket when in range
            _socket.interactionManager.SelectEnterUnconditionally(_socket, interactable);
        }
        
        void InstantAttach_SubscribeEvents()
        {
            _socket = GetComponent<XRSocketInteractor>();
            _socket.hoverExited.AddListener(_ => _lastHoverExitTime = Time.time);
            _socket.hoverEntered.AddListener(args => TryInstantAttachInteractable((IXRSelectInteractable)args.interactableObject));
        }
        
        /// <summary>
        /// Returns true when interaction-layer overlap AND select filters accept the interaction.
        /// Intentionally skips CanSelect / IsSelectableBy so sockets can "steal" items.
        /// 
        /// Uses reflection to call the protected XRInteractionManager methods.
        /// </summary>
        static bool IsSelectionValid(XRInteractionManager manager, IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            var mgrType = typeof(XRInteractionManager);
            MethodInfo _processSelectFiltersMethod= mgrType.GetMethod("ProcessSelectFilters", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo _hasInteractionLayerOverlapMethod= mgrType.GetMethod("HasInteractionLayerOverlap", BindingFlags.Static | BindingFlags.NonPublic);
            if (_hasInteractionLayerOverlapMethod == null || _processSelectFiltersMethod == null)
            {
                Debug.LogError("XRSocketStealer: Required XRInteractionManager methods not found via reflection. \n" +
                               "Using wrong version of Unity XR Interaction Toolkit? \n");
                return false;
            }
            
            //Layer overlap check
            //Using internal HasInteractionLayerOverlap(interactor, interactable)
            if (!(bool)_hasInteractionLayerOverlapMethod.Invoke(manager, new object[] { interactor, interactable }))
                return false;

            //Built-in select filters
            //Using internal ProcessSelectFilters(interactor, interactable)
            if (!(bool)_processSelectFiltersMethod.Invoke(manager, new object[] { interactor, interactable }))
                return false;
            
            return true;
        }
    }
}
