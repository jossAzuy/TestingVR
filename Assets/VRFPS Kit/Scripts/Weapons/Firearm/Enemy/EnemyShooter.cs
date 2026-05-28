using UnityEngine;


namespace VRFPSKit
{
    /// <summary>
    /// Simple enemy weapon controller that aims at a target and fires toward it.
    /// If a Bullet prefab is assigned, it spawns a physical projectile.
    /// Otherwise it falls back to a raycast shot that applies damage directly.
    /// </summary>
    public class EnemyShooter : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;

        [Header("Aiming")]
        public Transform muzzle;
        public bool rotateTowardsTarget = true;
        public bool lockRotationToY = true;
        public float turnSpeed = 720f;

        [Header("Firing")]
        public float fireInterval = 1f;
        public float shotWindupTime = 0.25f;
        public float maxFireDistance = 30f;
        public LayerMask lineOfSightMask = ~0;

        [Header("Projectile")]
        public GameObject bulletPrefab;
        public BallisticProfile ballisticProfile;
        public BulletType bulletType = BulletType.FMJ;

        [Header("Raycast Fallback")]
        public bool useRaycastIfNoBulletPrefab = true;
        public float raycastDamage = 15f;
        public LayerMask raycastMask = ~0;

        [Header("Die Feedback")]
        public GameObject deathFeedbackPlayer;
        public float destroyDelayAfterDeath = 0f;


        private float _nextFireTime;
        private float _shotReadyTime;
        private bool _shotQueued;
        private Vector3 _queuedShotPoint;
        private Damageable _damageable;
        private bool _isDead;

        private void Awake()
        {
            _damageable = GetComponentInParent<Damageable>();
            if (_damageable != null)
                _damageable.DeathEvent += HandleDeath;
        }

        private void OnDestroy()
        {
            if (_damageable != null)
                _damageable.DeathEvent -= HandleDeath;
        }

        private void Update()
        {
            if (_isDead)
                return;

            if (_shotQueued)
            {
                if (Time.time >= _shotReadyTime)
                    ReleaseQueuedShot();

                return;
            }

            if (target == null)
                return;

            if (rotateTowardsTarget)
                AimAtTarget();

            if (Time.time < _nextFireTime)
                return;

            if (!CanSeeTarget())
                return;

            if (Vector3.Distance(GetMuzzlePosition(), target.position) > maxFireDistance)
                return;

            QueueShot();
        }

        private void HandleDeath()
        {
            if (_isDead)
                return;

            Debug.Log($"EnemyShooter on {gameObject.name} died.");
            _isDead = true;
            if (deathFeedbackPlayer != null)
                deathFeedbackPlayer.SendMessage("PlayFeedbacks", SendMessageOptions.DontRequireReceiver);

            Destroy(gameObject, Mathf.Max(0f, destroyDelayAfterDeath));

            _shotQueued = false;
            enabled = false;
        }

        private void QueueShot()
        {
            _shotQueued = true;
            _shotReadyTime = Time.time + Mathf.Max(0f, shotWindupTime);
            _queuedShotPoint = GetTargetPoint();
            _nextFireTime = Time.time + Mathf.Max(0.01f, fireInterval);
        }

        private void ReleaseQueuedShot()
        {
            _shotQueued = false;

            if (_isDead)
                return;

            if (target == null)
                return;

            if (Vector3.Distance(GetMuzzlePosition(), target.position) > maxFireDistance)
                return;

            if (!CanSeeTarget())
                return;

            Fire(_queuedShotPoint);
        }

        private void Fire(Vector3 shotTargetPoint)
        {
            Vector3 fireOrigin = GetMuzzlePosition();
            Vector3 fireDirection = (shotTargetPoint - fireOrigin).normalized;

            if (bulletPrefab != null)
            {
                SpawnBullet(fireOrigin, fireDirection);
                return;
            }

            if (useRaycastIfNoBulletPrefab)
                FireRaycast(fireOrigin, fireDirection);
        }

        private void SpawnBullet(Vector3 origin, Vector3 direction)
        {
            GameObject projectileObject = Instantiate(bulletPrefab, origin, Quaternion.LookRotation(direction));

            if (projectileObject.TryGetComponent(out Bullet bullet))
            {
                bullet.shooter = null;
                bullet.bulletOwner = BulletOwner.Enemy;
                bullet.Initialize(ballisticProfile);
                bullet.bulletType = bulletType;
            }

            IgnoreSelfCollisions(projectileObject);
        }

        private void FireRaycast(Vector3 origin, Vector3 direction)
        {
            if (!Physics.Raycast(origin, direction, out RaycastHit hit, maxFireDistance, raycastMask, QueryTriggerInteraction.Ignore))
                return;

            if (hit.collider.GetComponentInParent<Player>() != null)
            {
                if (hit.collider.GetComponentInParent<IDamageReciever>() is IDamageReciever damageReciever)
                    damageReciever.TakeDamage(raycastDamage);
            }
        }

        private void IgnoreSelfCollisions(GameObject projectile)
        {
            Collider[] projectileColliders = projectile.GetComponentsInChildren<Collider>();
            Collider[] selfColliders = GetComponentsInParent<Collider>();

            foreach (Collider projectileCollider in projectileColliders)
            {
                foreach (Collider selfCollider in selfColliders)
                    Physics.IgnoreCollision(projectileCollider, selfCollider);
            }
        }

        private void AimAtTarget()
        {
            Vector3 targetPosition = GetTargetPoint();
            Vector3 aimDirection = targetPosition - transform.position;

            if (lockRotationToY)
                aimDirection.y = 0;

            if (aimDirection.sqrMagnitude <= 0.0001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(aimDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        private bool CanSeeTarget()
        {
            Vector3 origin = GetMuzzlePosition();
            Vector3 targetPoint = GetTargetPoint();
            Vector3 direction = targetPoint - origin;
            float distance = direction.magnitude;

            if (distance <= 0.001f)
                return true;

            direction /= distance;

            //If nothing on the obstacle mask blocks the ray, the target is visible.
            if (!Physics.Raycast(origin, direction, out RaycastHit hit, distance, lineOfSightMask, QueryTriggerInteraction.Ignore))
                return true;

            //If the first thing hit is part of the target, line of sight is clear.
            return hit.collider.GetComponentInParent<Player>() == target.GetComponentInParent<Player>();
        }

        private Vector3 GetMuzzlePosition()
        {
            return muzzle != null ? muzzle.position : transform.position;
        }

        private Vector3 GetTargetPoint()
        {
            if (target == null)
                return transform.position + transform.forward;

            return target.position;
        }
    }
}