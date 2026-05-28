using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

//Sockets override movement mode to instantaneous but doesnt reset until item is completely dropped. This is a fix for that
[RequireComponent(typeof(XRBaseInteractor))] [DisallowMultipleComponent]
[Obsolete(
    "XRSocketTrackingModeFix is deprecated and no longer used. " +
    "Use XRImprovedSocketInteractor instead. " +
    "This component is safe to remove."
)]
public class XRSocketTrackingModeFix : MonoBehaviour
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
