using UnityEngine;

namespace VRFPSKit
{
    [RequireComponent(typeof(Damageable), typeof(Rigidbody))]
    public class RangePhysicsObjectResettable : MonoBehaviour
    {
        private Vector3 _startPosition;
        private Quaternion _startRotation;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            _startPosition = transform.position;
            _startRotation = transform.rotation;
            
            GetComponent<Damageable>().ResetHealthEvent += ResetObject;
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