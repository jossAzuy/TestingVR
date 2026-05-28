using System;
using UnityEngine;

namespace VRFPSKit
{
    public class BottleDestruction : MonoBehaviour
    {
        public Damageable topDamageable;
        public Damageable bottomDamageable;
        
        public GameObject whole;
        public GameObject top;
        public GameObject bottom;

        private Vector3 _startPosition;
        private Quaternion _startRotation;


        public void Update()
        {
            whole.SetActive(topDamageable.IsAlive() && bottomDamageable.IsAlive());
            top.SetActive(topDamageable.IsAlive() && bottomDamageable.IsDead());
            bottom.SetActive(topDamageable.IsDead() && bottomDamageable.IsAlive());
            
            topDamageable.GetComponent<MeshCollider>().enabled = topDamageable.IsAlive();
            bottomDamageable.GetComponent<MeshCollider>().enabled = bottomDamageable.IsAlive();

            GetComponent<Rigidbody>().isKinematic = topDamageable.IsDead() && bottomDamageable.IsDead();
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            _startPosition = transform.position;
            _startRotation = transform.rotation;
            
            topDamageable.ResetHealthEvent += ResetObject;
            bottomDamageable.ResetHealthEvent += ResetObject;
        }

        private void ResetObject()
        {
            Rigidbody rb = GetComponent<Rigidbody>();   
            rb.position = _startPosition;
            rb.rotation = _startRotation;
            
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
