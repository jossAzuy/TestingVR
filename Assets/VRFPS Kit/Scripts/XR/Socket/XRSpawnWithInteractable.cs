using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRFPSKit
{
    /// <summary>
    /// This component allows a socket to spawn an interactable object attached.
    /// </summary>
    [RequireComponent(typeof(XRBaseInteractor))]
    public class XRSpawnWithInteractable : MonoBehaviour
    {
        public bool respawnOnDeath = true;
        public XRBaseInteractable selectedSpawnPrefab;

        public void Start()
        {
            SpawnInteractable();
        }
        
        private void CMD_Respawn() => Respawn();
        
        public void Respawn()
        {
            if (!respawnOnDeath) return;
            if (GetComponent<XRBaseInteractor>().hasSelection) return; // Already holding something
            
            SpawnInteractable();
        }

        private void SpawnInteractable()
        {
            GameObject spawnedObject = Instantiate(selectedSpawnPrefab.transform.gameObject, transform.position, transform.rotation);

            //Get IXRSelectInteractable component on spawned object
            if (spawnedObject.GetComponent<IXRSelectInteractable>() is not { } interactable) return;
            XRBaseInteractor interactor = GetComponent<XRBaseInteractor>();
            XRInteractionManager manager = interactor.interactionManager;
            
            manager.RegisterInteractor((IXRInteractor)interactor);
            manager.RegisterInteractable(interactable);
            
            manager.SelectEnterUnconditionally(interactor, interactable);
            
            if (interactable.transform.GetComponent<Rigidbody>() is { } rb)
            {
                //Fix for physics issues when spawning into socket
                rb.isKinematic = false;
            }
        }
        
        private void Awake()
        {
            if(GetComponentInParent<Damageable>() is { } damageable)
                damageable.ResetHealthEvent += CMD_Respawn;
        }
    }
}
