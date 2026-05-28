using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRFPSKit
{
    /// <summary>
    /// Shoots a physical bullet projectile, as opposed to say shotgun pellets
    /// </summary>
    public class FirearmBulletShooter : MonoBehaviour
    {
        public BallisticProfile ballisticProfile;
        [Space]
        public GameObject bulletPrefab;
        
        private Firearm _firearm;

        /// <summary>
        /// Calls for server to shoot a bullet with specified properties
        /// </summary>
        /// <param name="cartridge">Specifies bullet properties</param>
        public void ShootBullet(Cartridge cartridge, Vector3 bulletPosition, Quaternion bulletRotation)
        {
            if (ballisticProfile == null)
            {
                Debug.LogError($"BulletShooter on {gameObject.name} needs a ballistic profile assigned.");
                return;
            }

            GameObject obj = Instantiate(bulletPrefab, bulletPosition, bulletRotation);
            Bullet bullet = obj.GetComponent<Bullet>();

            BulletIgnoreCollision(bullet);
            
            //Track which BulletShooter shot the bullet
            bullet.shooter = this;
            
            //Apply bullet properties
            bullet.Initialize(ballisticProfile);
            bullet.bulletType = cartridge.bulletType;
        }

        /// <summary>
        /// Calls to shoot a bullet with specified properties
        /// </summary>
        /// <param name="cartridge">Specifies bullet properties</param>
        /// <param name="bulletSpawnPosition">Client sends bullet spawn position</param>
        /// <param name="bulletSpawnRotation">Client sends bullet spawn rotation</param>
        private void FirearmShootEvent(Cartridge cartridge, Vector3 bulletSpawnPosition, Quaternion bulletSpawnRotation)
        {
            int projectileAmount = 1;
            
            //Prevent null error
            if (ballisticProfile) projectileAmount = ballisticProfile.projectileAmount;
            
            for (int i = 0; i < projectileAmount; i++)
                ShootBullet(cartridge, bulletSpawnPosition, bulletSpawnRotation);
        }
        
        private void Awake()
        {
            _firearm = GetComponentInParent<Firearm>();
            if (_firearm == null)
            {
                Debug.LogError("BulletShooter could not find a Firearm component in its parents. It will not work correctly");
                return;
            }
            
            //Listen to Firearm's Shoot event (which is when we should spawn a bullet)
            //Also Send bullet spawn orientation on the server
            _firearm.ShootEvent += cartridge =>
            {
                FirearmShootEvent(cartridge, transform.position, transform.rotation);
            };
                
        }

        private void BulletIgnoreCollision(Bullet bullet)
        {
            foreach (var bulletCollider in bullet.GetComponentsInChildren<Collider>())
            {
                //Ignore collision between bullet & firearm colliders
                foreach (var firearmCollider in _firearm.GetComponentsInChildren<Collider>())
                    Physics.IgnoreCollision(bulletCollider, firearmCollider);
                
                //Ignore collision between bullet & firearm attachments
                //Iterate all interactors on the firearm
                foreach (var attachmentSocket in _firearm.GetComponentsInChildren<XRBaseInteractor>())
                    //Iterate all firearm attachments on those interactors
                    foreach (var attachment in attachmentSocket.interactablesSelected)
                        //Iterate all colliders on those attachments and ignore collision with the bullet
                        foreach (var attachmentCollider in attachment.transform.GetComponentsInChildren<Collider>())
                            Physics.IgnoreCollision(bulletCollider, attachmentCollider);

                //Ignore collision with the owning player's colliders (prevent self-hit)
                Player owningPlayer = TryGetOwningPlayer();
                if (owningPlayer != null)
                {
                    foreach (var playerCollider in owningPlayer.GetComponentsInChildren<Collider>())
                    {
                        Physics.IgnoreCollision(bulletCollider, playerCollider);
                    }
                }
            }
        }

        public Player TryGetOwningPlayer()
        {
            XRGrabInteractable firearmGrabbable = GetComponentInParent<XRGrabInteractable>();
            if (!firearmGrabbable) return null;
            if (!firearmGrabbable.isSelected) return null;
            XRBaseInteractor selector = (XRBaseInteractor)firearmGrabbable.interactorsSelecting[0];
            Player player = selector.GetComponentInParent<Player>();
            if (!player) return null;
            
            return player;
        }

        /// <summary>
        /// Draw a cone that represents the maximum projectile spread angle
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!ballisticProfile) return;
            float gizmosLineLength = 15;
            
            Gizmos.color = Color.red;

            if (ballisticProfile.randomSpreadAngle == 0)
                Gizmos.DrawLine(transform.position, transform.position + (transform.forward * gizmosLineLength));
            else
            {
                //Draw a cone representing the random spread angle
                int lineAmount = 25;
                
                //Divide a circle in to x steps, and then iterate with a fixed angle interval for the whole circle
                for (float radianAngle = 0; radianAngle < Mathf.PI * 2; radianAngle += Mathf.PI * 2 / lineAmount)
                {
                    Quaternion circularOffsetDirection = Quaternion.Euler(Mathf.Sin(radianAngle) * ballisticProfile.randomSpreadAngle,
                        Mathf.Cos(radianAngle) * ballisticProfile.randomSpreadAngle, 0);
                    Vector3 localLineDirection = circularOffsetDirection * Vector3.forward;
                    Vector3 worldLineDirection = transform.TransformDirection(localLineDirection);
                    Vector3 lineEndPoint = transform.position + worldLineDirection * gizmosLineLength;
                    
                    Gizmos.DrawLine(transform.position, lineEndPoint);
                }
            }
        }
    }
}