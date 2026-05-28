using System.Collections.Generic;
using System.Linq;
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
    /// A socket interaction filter that ensures the XRSocketInteractor only accepts interactables
    /// that have been previously held by the player. 
    /// 
    /// This script tracks interactables that are currently selected by a hand (XRBaseInputInteractor)
    /// and only allows the socket to interact with them. Hovering and selection of unheld or idle
    /// items is ignored, preventing sockets from automatically "vacuuming" objects in the scene.
    /// 
    /// The script maintains a temporary list of hovered held interactables, updates it on hover
    /// enter/exit, and provides delayed cleanup to allow smooth interaction transitions.
    /// </summary>
    public partial class XRImprovedSocketInteractor
    {
        // Start is called before the first frame update
        void HeldFilter_SubscribeEvents()
        {
            selectFilters.Add(new XRSelectFilterDelegate(OnlyAcceptHeldInteractablesFilter));
            hoverFilters.Add(new XRHoverFilterDelegate(OnlyAcceptHeldInteractablesFilter));
        }
        
        private bool OnlyAcceptHeldInteractablesFilter(IXRInteractor interactor, IXRInteractable interactable)
        {
            //Skip filter if it is disabled
            if (!requirePriorPlayerHold) return true;
            //Allow if already selected by socket
            if (IsSelecting(interactable) || IsHovering(interactable)) return true;

            bool interactableHeld = IsInteractableHeld((IXRSelectInteractable)interactable);

            return interactableHeld;//TODO breakpoint here, figure out why filter is returning true on select with wrong conditions
        }

        private bool IsInteractableHeld(IXRSelectInteractable interactable) =>
            interactable.interactorsSelecting.Any(interactor => interactor is XRBaseInputInteractor);
    }
}