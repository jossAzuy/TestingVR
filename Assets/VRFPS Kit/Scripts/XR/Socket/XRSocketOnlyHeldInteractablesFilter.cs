using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRFPSKit
{
    [RequireComponent(typeof(XRSocketInteractor))] [DisallowMultipleComponent]
    [Obsolete(
        "XRSocketOnlyHeldInteractablesFilter is deprecated and no longer used. " +
        "Use XRImprovedSocketInteractor instead. " +
        "This component is safe to remove."
    )]
    public class XRSocketOnlyHeldInteractablesFilter : MonoBehaviour
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