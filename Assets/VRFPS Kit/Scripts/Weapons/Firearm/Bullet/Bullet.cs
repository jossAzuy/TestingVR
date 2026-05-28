using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace VRFPSKit
{
    public enum BulletOwner
    {
        Player,
        Enemy
    }

    /// <summary>
    /// Represents a physical bullet that has been fired
    /// </summary>
    public class Bullet : MonoBehaviour
    {
        private const float MaxLifetime = 15;
        private const float ImpactEffectSearchRadius = .3f;
        private const float MinimumVelocity01 = .005f;

        public AudioSource hitSound;
        public GameObject defaultImpactEffect;
        public GameObject tracerTail;
        public GameObject bulletCollisionFeedbackPlayer;
        public BulletType bulletType;
        public BulletOwner bulletOwner;
        public FirearmBulletShooter shooter;

        private BallisticProfile _ballisticProfile;
        
        //NOTE events will only be called on server
        public event Action<Bullet> HitEvent;
        public event Action<Bullet, IDamageReciever> HitDamageableEvent;

        private Rigidbody _rigidbody;
        // Start is called before the first frame update
        void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();

            if (_ballisticProfile == null)
            {
                Debug.LogError($"Bullet on {gameObject.name} has no ballistic profile assigned.");
                enabled = false;
                return;
            }
            
            //Bullet apply randomSpreadAngle to rotation
            if(_ballisticProfile.randomSpreadAngle != 0)
                transform.rotation *= Quaternion.Euler(
                    Random.Range(-_ballisticProfile.randomSpreadAngle, _ballisticProfile.randomSpreadAngle) , 
                    Random.Range(-_ballisticProfile.randomSpreadAngle, _ballisticProfile.randomSpreadAngle), 0);//TODO this doesnt work
            
            _rigidbody.AddForce(transform.forward * _ballisticProfile.startVelocity, ForceMode.VelocityChange);
            _rigidbody.useGravity = false; //Use custom gravity solution
            
            //Rigidbody drag from profile
            _rigidbody.linearDamping = _ballisticProfile.drag;
            
            tracerTail.SetActive(bulletType == BulletType.Tracer);
            
            Invoke(nameof(Despawn), MaxLifetime);
        }

        private void FixedUpdate()
        {
            //Custom gravity implementation
            _rigidbody.AddForce(Physics.gravity * _ballisticProfile.gravityScale, ForceMode.Acceleration);
            
            //Despawn bullet if remaining velocity is to low, not if it is 0 though cause that means it hasn't started moving yet
            //1 is starting velocity, 0 is standing still
            if (GetRemainingVelocity01() < MinimumVelocity01 && GetRemainingVelocity01() != 0){ DestroyBullet();}
        }

        public void Initialize(BallisticProfile ballisticProfile)
        {
            _ballisticProfile = ballisticProfile;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_rigidbody.isKinematic) return; //Do nothing if bullet is destroyed

            Bullet otherBullet = collision.collider.GetComponentInParent<Bullet>();
            if (otherBullet != null && otherBullet != this)
            {
                if (otherBullet.bulletOwner != bulletOwner)
                {
                    PlayBulletCollisionFeedback();
                    otherBullet.PlayBulletCollisionFeedback();
                    Destroy(gameObject);
                    Destroy(otherBullet.gameObject);
                }

                return;
            }
            
            //Simple 3-case flow:
            //1) Firearm hit -> block the bullet
            //2) Damageable hit -> apply damage and impact feedback
            //3) World hit -> destroy bullet and spawn default impact feedback

            //!If this bullet hit the player that fired it, ignore impact effects and damage (prevent self-hit visuals)
            Player owningPlayer = shooter?.TryGetOwningPlayer();
            if (owningPlayer != null)
            {
                Player hitPlayer = collision.collider.GetComponentInParent<Player>();
                if (hitPlayer == owningPlayer)
                {
                    DestroyBullet();
                    return;
                }
            }

            //!Treat the player's firearm as a shield: bullets that hit it are destroyed immediately.
            if (collision.collider.GetComponentInParent<Firearm>() != null)
            {
                PlayImpactEffect(collision.collider, collision.GetContact(0).point, collision.GetContact(0).normal, true, true);
                Destroy(gameObject);
                return;
            }

            //If we hit something that cannot take damage, treat it as world impact.
            if (collision.collider.GetComponentInParent<IDamageReciever>() == null)
            {
                PlayImpactEffect(collision.collider, collision.GetContact(0).point, collision.GetContact(0).normal, true);
                Destroy(gameObject);
                return;
            }

            //Call Hit Event
            HitEvent?.Invoke(this);
            
            //Get damageable component in collider or in parents
            Collider dmgCollider = collision.collider;
            if (dmgCollider.gameObject.GetComponentInParent<IDamageReciever>() is IDamageReciever damageReciever)
            {
                float damage = _ballisticProfile.baseDamage;
                if(_ballisticProfile.scaleDamageWithVelocity)
                {
                    float impulseScale01 = collision.GetContact(0).impulse.magnitude / _rigidbody.mass / _ballisticProfile.startVelocity;
                    damage *= impulseScale01;
                }
                damage = Mathf.Max(damage); //Damage can't be less than 0
                
                //Apply damage to damageable
                damageReciever.TakeDamage(damage);
                
                //Call Hit Damageable Event
                HitDamageableEvent?.Invoke(this, damageReciever);
            }

            //Play hit effects on clients
            PlayImpactEffect(collision.collider, collision.GetContact(0).point, collision.GetContact(0).normal);

            
            //Bouncing is performed by physics material
            
            //Use impact angle to determine velocity loss.
            float impactAngle = Vector3.Angle(collision.GetContact(0).normal, -transform.forward); //0 degrees = completely along normal
            float impactAngle01 = Mathf.Clamp01(Mathf.InverseLerp(0, 90, impactAngle));//0 = completely along normal, 1 = completely perpendicular to normal
            _rigidbody.linearVelocity *= impactAngle01; //Straight on impact will result in complete stop, completely perpendicular will mean no energy loss

        }
        
        private void DestroyBullet()
        {
            //Stop moving
            _rigidbody.isKinematic = true;

            //Schedule destruction, let sound play out first
            Invoke(nameof(Despawn), 2);
        }

        private void PlayBulletCollisionFeedback()
        {
            if (bulletCollisionFeedbackPlayer != null)
                bulletCollisionFeedbackPlayer.SendMessage("PlayFeedbacks", SendMessageOptions.DontRequireReceiver);
        }

        private float GetRemainingVelocity01() => Mathf.InverseLerp(0, _ballisticProfile.startVelocity, _rigidbody.linearVelocity.magnitude); 
        
        private void PlayImpactEffect(Collider impactCollider, Vector3 impactPoint, Vector3 impactNormal, bool forceDefaultImpactEffect = false, bool playImpactAudio = false)
        {
            //!If the impact point is on the owning player, skip spawning impact effects
            Player owningPlayer = shooter?.TryGetOwningPlayer();
            if (owningPlayer != null)
            {
                foreach (Collider nearby in Physics.OverlapSphere(impactPoint, ImpactEffectSearchRadius))
                {
                    if (nearby.GetComponentInParent<Player>() == owningPlayer)
                        return;
                }
            }

            if(hitSound != null)
                hitSound.Play();

            //Hide bullet renderer on all clients
            foreach(MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>())
                Destroy(renderer);

            //Locally spawn the particle effect
            Quaternion normalQuaternion = Quaternion.LookRotation(impactNormal);
            GameObject impactEffectPrefab = forceDefaultImpactEffect ? defaultImpactEffect : GetImpactEffect(impactCollider);

            GameObject impactEffectObj = Instantiate(impactEffectPrefab, impactPoint, normalQuaternion);

            if (playImpactAudio && impactEffectObj.TryGetComponent(out AudioSource impactAudioSource))
                impactAudioSource.Play();
        }

        /// <summary>
        /// Method that tries to fetch a BulletImpactEffect component on nearby colliders.
        /// Doing it this way allows for clients to decide which effect to use on their own,
        /// instead of having to network impact effects (complicated since we can't easily send
        /// Prefab references over the network).
        /// </summary>
        /// <param name="impactPoint"></param>
        /// <returns></returns>
        private GameObject GetImpactEffect(Collider impactCollider)
        {
            if (impactCollider.GetComponentInParent<BulletImpactEffect>() is BulletImpactEffect bulletImpactEffect &&
                bulletImpactEffect.impactEffect != null)
                return bulletImpactEffect.impactEffect;

            return defaultImpactEffect;
        }

        private void Despawn()
        {
            Destroy(gameObject);
        }
    }
}