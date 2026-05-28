using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRFPSKit
{
    /// <summary>
    /// Prevents Interactables from lagging behind one frame per every nested socket.
    /// Essentially emulates making the interactable transform a child.
    /// </summary>
    [RequireComponent(typeof(XRSocketInteractor))] [DisallowMultipleComponent]
    [Obsolete(
        "XRSocketDelayedTrackingFix is deprecated and no longer used. " +
        "Use XRImprovedSocketInteractor instead. " +
        "This component is safe to remove."
    )]
    public class XRSocketDelayedTrackingFix : MonoBehaviour
    {
#if UNITY_EDITOR
        private void OnValidate()
        {
            Debug.LogError(
                $"{GetType().Name} is deprecated and should be removed. " +
                "Replace your XRSocketInteractor with XRImprovedSocketInteractor.",
                this
            );
        }
#endif
    }
}
