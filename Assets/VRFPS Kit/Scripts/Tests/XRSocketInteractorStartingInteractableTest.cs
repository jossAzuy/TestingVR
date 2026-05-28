using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRFPSKit
{
    [RequireComponent(typeof(XRSocketInteractor))]
    public class XRSocketInteractorStartingInteractableTest : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Update()
        {
            XRSocketInteractor socket = GetComponent<XRSocketInteractor>();
            var socketStartingSelectedInteractable = socket.startingSelectedInteractable;
            
            if (!socket.interactionManager.CanSelect(socket, socketStartingSelectedInteractable))
            {
                Debug.LogError("XRSocketInteractor.startingSelectedInteractable: Cannot select the start interactable, dumping relevant filter info:");
                XRFilterDebugger.DebugSelectFilters(socket, socketStartingSelectedInteractable);
            }
        }
    }
}
