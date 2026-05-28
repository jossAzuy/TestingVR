using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRFPSKit
{
    [RequireComponent(typeof(LineRenderer))]
    public class LaserRenderer : MonoBehaviour
    {
        public Transform rayOrigin;
        public LayerMask collisionMask = ~0;
        public float maxDistance = 1000f;
        public bool hideWhenNotHeld = true;
        public Light rayLight;

        private LineRenderer _line;
        private XRGrabInteractable _grabbable;

        private void Update()
        {
            if (hideWhenNotHeld && _grabbable != null && !_grabbable.isSelected)
            {
                _line.enabled = false;
                if (rayLight != null)
                    rayLight.enabled = false;
                return;
            }

            _line.enabled = true;
            if (rayLight != null)
                rayLight.enabled = true;

            UpdateRayLength();
        }

        private void UpdateRayLength()
        {
            Transform origin = rayOrigin != null ? rayOrigin : transform;
            Vector3 rayStart = origin.position;
            Vector3 rayDirection = origin.forward;

            Vector3 rayEnd = rayStart + rayDirection * maxDistance;
            if (Physics.Raycast(rayStart, rayDirection, out RaycastHit hit, maxDistance, collisionMask, QueryTriggerInteraction.Ignore))
                rayEnd = hit.point;

            _line.SetPosition(0, rayStart);
            _line.SetPosition(1, rayEnd);

            if (rayLight != null)
                rayLight.transform.position = rayEnd;
        }
        
        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.positionCount = 2;
            _line.useWorldSpace = true;

            _grabbable = GetComponentInParent<XRGrabInteractable>();
        }
    }
}