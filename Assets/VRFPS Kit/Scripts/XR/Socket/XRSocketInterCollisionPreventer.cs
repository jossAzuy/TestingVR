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
    /// Prevents collision between the two rigidbodies that make up a socket interaction (interactor & interactable)
    /// </summary>
    [RequireComponent(typeof(XRSocketInteractor))]
    [Obsolete(
        "XRSocketInterCollisionPreventer is deprecated and no longer used. " +
        "Use XRImprovedSocketInteractor instead. " +
        "This component is safe to remove."
    )]
    public class XRSocketInterCollisionPreventer : MonoBehaviour
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
