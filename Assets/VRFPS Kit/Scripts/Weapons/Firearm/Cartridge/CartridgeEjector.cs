using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace VRFPSKit
{
    public class CartridgeEjector : MonoBehaviour
    {
        private const float MinimumActionSpeedMultiplier01 = .2f;
        
        [Obsolete("CartridgeEjector now uses the currect GameObject as spawn point")]
        public Transform spawnPoint;
        
        public GameObject cartridgeItemPrefab;
        [Space] 
        public Vector3 defaultVelocity;
        public Vector3 randomVelocity = new (0.5f, 0.5f, 0.5f);
        [Space]
        public Vector3 defaultTorque = new (20, 20, 0);
        public Vector3 randomTorque = new (20, 20, 20);
        [Space]
        [Tooltip("How long before empty ejected casings are destroyed, set to -1 to never destroy")]
        public float casingDestructionTime = 5f;

        public event Action<Cartridge> EjectEvent;

        private float _actionPosition01LastFrame;
        private FirearmCyclingAction _action;
        private Firearm _firearm;

        private void LateUpdate()
        {
            _actionPosition01LastFrame = _action.GetActionPosition01();
        }
        
        private float ActionMovementSpeed01 () => (_action.GetActionPosition01() - _actionPosition01LastFrame); // Moving back completely over one frame is max speed


        /// <summary>
        /// If a cartridge is in the chamber, eject it
        /// </summary>
        public void TryEjectChamber()
        {
            Cartridge chamberCartridge = _firearm.chamberCartridge;
            
            //Cant eject empty chamber
            if (chamberCartridge.IsNull()) return;
            
            //Eject
            SpawnEjectedCartridge(chamberCartridge);
            _firearm.chamberCartridge = Cartridge.Empty;
            
            //Call eject event
            EjectEvent?.Invoke(chamberCartridge);
        }
        
        /// <summary>
        /// Spawns an ejected cartridge
        /// </summary>
        /// <param name="cartridge">Ejected cartridge properties</param>
        private void SpawnEjectedCartridge(Cartridge cartridge)
        {
            //Call events
            if (!cartridge.IsNull())
                EjectEvent?.Invoke(cartridge);
            
            //TODO curve
            EjectCartridge(cartridge, Mathf.Clamp(ActionMovementSpeed01(), MinimumActionSpeedMultiplier01, 1)); //Divide by constant, so old values work
        }
        
        
        /// <summary>
        /// Spawns an ejected cartridge
        /// </summary>
        /// <param name="cartridge">Ejected cartridge properties</param>
        private void EjectCartridge(Cartridge cartridge, float ejectSpeedMultiplier)
        {
            if (cartridge.Equals(Cartridge.Empty))
            {
                Debug.LogWarning("Tried ejecting an empty cartridge");
                return;
            }
            if(cartridgeItemPrefab == null)
            {
                Debug.LogWarning("CartridgeEjector has no cartridgeItemPrefab assigned!");
                return;
            }

            //Instantiate cartridge
            GameObject obj = Instantiate(cartridgeItemPrefab, transform.position, transform.rotation);
            
            StartCoroutine(DelayCartridgeCollision(obj));
            
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            
            //Apply Random Linear Velocity
            Vector3 randomizedVelocity = defaultVelocity + RandomVector3(randomVelocity);
            Vector3 worldSpaceVelocity = transform.TransformDirection(randomizedVelocity);
            worldSpaceVelocity *= ejectSpeedMultiplier;
            rb.AddForce(worldSpaceVelocity, ForceMode.VelocityChange);
            
            //Apply random angular velocity
            rb.maxAngularVelocity = Single.PositiveInfinity;
            Vector3 eulerTourqe = defaultTorque + RandomVector3(randomTorque);
            eulerTourqe *= ejectSpeedMultiplier;
            
            Vector3 worldSpaceEulerTourqe = transform.TransformVector(eulerTourqe);
            rb.AddTorque(worldSpaceEulerTourqe,
                ForceMode.VelocityChange);

            //Apply cartridge item values
            CartridgeItem cartridgeItem = obj.GetComponent<CartridgeItem>();
            cartridgeItem.cartridge = cartridge;
            
            //If we ejected empty casing, Schedule Cartridge Destruction,
            //you can remove this if you want Casings to never disappear!a
            if(cartridge.bulletType == BulletType.Empty_Casing)
                StartCoroutine(DestroyCartridgeLater(obj));
        }

        private IEnumerator DelayCartridgeCollision(GameObject cartridge)
        {
            Array.ForEach(cartridge.GetComponentsInChildren<Collider>(),  col => col.enabled = false);
            yield return new WaitForSeconds(.05f);
            Array.ForEach(cartridge.GetComponentsInChildren<Collider>(),  col => col.enabled = true);
        }
        
        private IEnumerator DestroyCartridgeLater(GameObject cartridge)
        {
            if (casingDestructionTime <= 0)
                yield break;    
            
            yield return new WaitForSeconds(casingDestructionTime);
            
            Destroy(cartridge);
        }
        
        private void Awake()
        {
            _action = GetComponentInParent<FirearmCyclingAction>();
            _firearm = GetComponentInParent<Firearm>();
            
#pragma warning disable CS0618 // Type or member is obsolete
            if (spawnPoint != null)
            {
                Debug.LogWarning("CartridgeEjector no longer uses 'spawnPoint' transform, cartridge ejection needs reconfiguring. (Read whole message) \n\n"+
                                 "Move your CartridgeEjector component from the main Firearm object to it's own 'Ejector' gameObject.\n" +
                                 "To disable this warning, you also need to set 'spawnPoint' to be unassigned");
            }
#pragma warning restore CS0618 // Type or member is obsolete
        }
        
        private Vector3 RandomVector3(Vector3 bounds) => new (Random.Range(-bounds.x, bounds.x), Random.Range(-bounds.y, bounds.y), Random.Range(-bounds.z, bounds.z));
    }
}