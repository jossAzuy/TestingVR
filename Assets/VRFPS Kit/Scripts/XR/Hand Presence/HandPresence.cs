using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRFPSKit
{
    [RequireComponent(typeof(Rigidbody))]
    public class HandPresence : MonoBehaviour
    {
        //TODO anchors dont track body movement properly yet (maybe attach to camera)
        //TODO adjust anchors
        public VRBodySkinBinder skinBinder;
        public SphereCollider handPositionAnchorBounds;
        [Space]
        public Transform trackedController;
        public Vector3 handPositionOffset;
        public Vector3 handRotationOffset;
        [Space]
        [Header("Weight Simulation")]
        public bool disablePhysics = false;
        public float linearAcceleration = 1.2f;
        public float angularAcceleration = 1f;
        public float twoHandedWeightMultiplier = .6f;//TODO adjust this now that we have inertia instead
        public AnimationCurve weightAccelerationMultiplierCurve = AnimationCurve.Linear(0, 1, 100, 0);

        [Space]
        [Tooltip("How much the tip of a long object will lag behind.")]
        public float tensorInertiaScale = 15f;

        [Space]
        public float handColliderReenableDelay = .25f;
        [Tooltip("How far away can hand be before we teleport it back to the tracked controller.")]
        public float maxDistanceFromTrackedController = .3f;
        
        private float _xrOriginYRotationLastFrame;
        
        private Rigidbody _rigidbody;
        private Collider[] _handColliders;
        private XROrigin _xrOrigin;
        private float _lastGrabTime;
        
        
        // Update is called once per frame
        void FixedUpdate()
        {
            if (trackedController == null) return;
            
            _rigidbody.isKinematic = disablePhysics;
            
            //Hand tracking
            if (!disablePhysics)
            {
                HandsOutOfRangeCheck();
                ApplyPhysicsTrackingForce();
            }
            else
            {
                transform.position = GetTargetPosition();
                transform.rotation = GetTargetRotation();
            }
            
            //Update body skin tracking positions (hands)
            skinBinder.GetSkin()?.UpdateHandsTracking();
        }

        private void ApplyPhysicsTrackingForce()
        {
            //Track controller position
            Vector3 positionDelta = GetTargetPosition() - transform.position;
            Vector3 newLinearVelocity = positionDelta * linearAcceleration;

            //Slower linear velocity the more weight is held & depending on if two held handed
            newLinearVelocity *= GetWeightAccelerationMultiplier();
            
            _rigidbody.linearVelocity = newLinearVelocity;
            
            Quaternion rotationDelta = GetTargetRotation() * Quaternion.Inverse(transform.rotation);
            rotationDelta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            Vector3 rotationDiff = angle * axis.normalized;
            
            Vector3 newAngularVelocity = rotationDiff * Mathf.Deg2Rad * angularAcceleration;
            
            //Slower angular velocity the more tensor inertia there is along axis
            newAngularVelocity /= CalculateTotalTensorInertia(axis);
            
            _rigidbody.angularVelocity = newAngularVelocity;
            _rigidbody.maxAngularVelocity = 100;
        }

        private Vector3 EnforcePositionAnchor(Vector3 position)
        {
            //Do we have an anchor?
            if(handPositionAnchorBounds == null) return position;
            
            float anchorRadius = handPositionAnchorBounds.radius;
            Vector3 handPosRelativeToAnchor = position - handPositionAnchorBounds.transform.position;
            
            //Are we inside anchor?
            if (handPosRelativeToAnchor.magnitude < anchorRadius) return position;
            
            //Stay within anchor
            Vector3 clampedRelativeHandPos = handPosRelativeToAnchor.normalized * anchorRadius;
            Vector3 clampedHandPos = handPositionAnchorBounds.transform.position + clampedRelativeHandPos;
            
            return clampedHandPos;
        }

        
        /// <summary>
        /// Calculates how total tensor inertia (tip of a long object will lag behind) is applied all items held by this hand.
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        private float CalculateTotalTensorInertia(Vector3 axis)
        {
            float totalInertia = 0f;
            
            foreach (var interactor in transform.GetComponentsInChildren<XRBaseInteractor>())
            {
                foreach (var selected in interactor.interactablesSelected)
                {
                    if (selected is not XRGrabInteractable grab) continue;

                    // now you can compute a per‑axis inertia on that item:
                    totalInertia += CalculateNestedTensorInertia(grab, axis);
                }
            }

            totalInertia = Mathf.Max(totalInertia, 1f); // ensure we have a minimum inertia to avoid division by zero
            
            return totalInertia;
        }

        /// <summary>
        /// Calculates how much tensor inertia (tip of a long object will lag behind) is applied a grabbable and it's
        /// nested items (attachnebts) that is held by this hand.
        /// </summary>
        /// <param name="grab"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        private float CalculateNestedTensorInertia(XRGrabInteractable grab, Vector3 axis)
        {
            //Calculate inertia of the grabbable itself
            float result = CalculateTensorInertia(grab.GetComponentInParent<Rigidbody>(), axis);
            
            //Iterate through all nested attached interactables and add their inertia
            foreach (var interactor in grab.GetComponentsInChildren<XRBaseInteractor>())
                foreach (var nestedInteractable in interactor.interactablesSelected)
                    if (nestedInteractable is XRGrabInteractable nestedGrabbable)
                        result += CalculateNestedTensorInertia(nestedGrabbable, axis);
            
            return result;
        }

        /// <summary>
        /// Calculates how much tensor inertia (tip of a long object will lag behind) is applied a rigidbody that is held by this hand.
        /// </summary>
        /// <param name="rb"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        private float CalculateTensorInertia(Rigidbody rb, Vector3 axis)
        {
            // get world‑space inertia along that axis from the provided Rigidbody
            Quaternion tensorRot = rb.inertiaTensorRotation;
            Vector3 localAxis = axis.normalized;
            Vector3 axisInInertiaSpace = Quaternion.Inverse(tensorRot) * localAxis;
            float inertiaTensor = Vector3.Dot(axisInInertiaSpace, Vector3.Scale(rb.inertiaTensor, axisInInertiaSpace));

            // clamp & scale
            float effectiveInertia = Mathf.Abs(inertiaTensor) * tensorInertiaScale;
            effectiveInertia = Mathf.Max(effectiveInertia, 0.5f);

            // Parallel‑axis: distance from hand to item's COM
            Vector3 comWorld = rb.worldCenterOfMass;
            Vector3 handPos = GetTargetPosition();  // or wherever your “hand” transform is
            Vector3 handToCOM = handPos - comWorld;
            // only perpendicular distance matters:
            Vector3 handToCOMPerp = handToCOM - Vector3.Dot(handToCOM, localAxis) * localAxis;
            float r = handToCOMPerp.magnitude;

            return effectiveInertia + rb.mass * r * r;
        }

        private void HandsOutOfRangeCheck()
        {
            //Check if hands are too far from tracked controller (could happen when teleporting or due to physics blocking hands)
            if (Vector3.Distance(transform.position, GetTargetPosition()) > maxDistanceFromTrackedController
                 && Time.time - _lastGrabTime > 1f) //Avoid recentering right after grabbing an item, as we want that to feel physical
                RecenterHand();
        }
        
        public void RecenterHand()
        {
            transform.position = GetTargetPosition();
            transform.rotation = GetTargetRotation();

            //Force update all held interactables to prevent them from lagging behind
            foreach (var handInteractor in GetComponentsInChildren<XRDirectInteractor>())
            {
                foreach (var heldInteractable in handInteractor.interactablesSelected)
                {
                    if(heldInteractable is not XRGrabInteractable grabbable) continue;
                    
                    MarkXRGrabbableTargetPoseDirty(grabbable);
                }
            }
        }

        private void Update()
        {
            if(!Mathf.Approximately(_xrOriginYRotationLastFrame, _xrOrigin.transform.rotation.eulerAngles.y))
                RecenterHand();
            
            _xrOriginYRotationLastFrame = _xrOrigin.transform.rotation.eulerAngles.y;
        }
        
        private Vector3 GetTargetPosition()
        {
            if (trackedController == null) return Vector3.zero;
            Vector3 targetPosition = trackedController.position;
            targetPosition += handPositionOffset;
            targetPosition = EnforcePositionAnchor(targetPosition);
            
            return targetPosition;
        }
        
        private Quaternion GetTargetRotation()
        {
            if (trackedController == null) return Quaternion.identity;
            
            Quaternion targetRotation = trackedController.rotation;
            targetRotation *= Quaternion.Euler(handRotationOffset);
            
            return targetRotation;
        }
        
        public static int CountRigidbodyInteractors(Rigidbody rb)
        {
            int result = 0;
            foreach (var interactable in rb.GetComponentsInChildren<XRGrabInteractable>())
                foreach (var interactor in interactable.interactorsSelecting)
                    result++;
            
            return result;
        }

        private float GetWeightAccelerationMultiplier()
        {
            float weight = GetCurrentInteractableWeight();

            //Apply two handed weight multiplier if holding interactable two handed
            if (IsInteractableHeldTwoHanded()) 
                weight *= twoHandedWeightMultiplier;
            
            return Mathf.Clamp01(weightAccelerationMultiplierCurve.Evaluate(weight));
        }
        
        private bool IsInteractableHeldTwoHanded()
        {
            foreach (var handInteractor in GetComponentsInChildren<XRBaseInteractor>())
            {
                if (handInteractor.interactablesSelected.Count == 0) return false;
                Rigidbody interactableRigidbody = handInteractor.interactablesSelected[0].transform.GetComponentInParent<Rigidbody>();
                if (interactableRigidbody == null) return false;
                if (CountRigidbodyInteractors(interactableRigidbody) < 2) return false;
                
                return true;
            }

            return false;
        }

        private float GetCurrentInteractableWeight()
        {
            float weightSum = 0;
            foreach (var handInteractor in GetComponentsInChildren<XRBaseInteractor>())
                foreach (var handInteractable in handInteractor.interactablesSelected)
                    weightSum += CalculateNestedInteractableWeight((XRBaseInteractable)handInteractable);
            
            return weightSum;
        }

        public static float CalculateNestedInteractableWeight(XRBaseInteractable interactable)
        {
            Rigidbody interactableRigidbody = interactable.GetComponentInParent<Rigidbody>();

            float nestedInteractableWeightSum = 0;
            foreach (var nestedInteractor in interactableRigidbody.GetComponentsInChildren<XRBaseInteractor>())
                foreach (var nestedInteractable in nestedInteractor.interactablesSelected)
                    nestedInteractableWeightSum += CalculateNestedInteractableWeight((XRBaseInteractable)nestedInteractable);

            float simulatedMass = 0;
            foreach(var simulatedWeight in interactableRigidbody.GetComponentsInChildren<IXRSimulatedWeight>())
                simulatedMass += simulatedWeight.GetSimulatedMass();

            return interactableRigidbody.mass + nestedInteractableWeightSum + simulatedMass;
        }

        private async void UpdateHandColliders()
        {
            if (!IsSelectingAnInteractable())
                //Delay in milliseconds
                await Task.Delay((int)(handColliderReenableDelay * 1000));
            
            foreach (Collider handCollider in _handColliders)
                handCollider.enabled = !IsSelectingAnInteractable();
        }
        
        private bool IsSelectingAnInteractable()
        {
            foreach (var handInteractor in GetComponentsInChildren<XRBaseInteractor>())
                if (handInteractor.hasSelection)
                    return true;

            return false;
        }
        
        /// <summary>
        /// When an item is grabbed, align the hand with the interactable's attach point.
        /// </summary>
        /// <param name="interactor"></param>
        /// <param name="itemAttachPoint"></param>
        private void AlignHandsWithInteractable(XRDirectInteractor interactor, Transform itemAttachPoint)
        {
            // interactor expressed in hand-presence local space:
            // interactor.position == hand.TransformPoint(interactorDeltaPos)
            Vector3 interactorDeltaPos = transform.InverseTransformPoint(interactor.transform.position);
            Quaternion interactorDeltaRot = Quaternion.Inverse(transform.rotation) * interactor.transform.rotation;
        
            // Solve: itemAttachPoint_world == newHandWorld * interactorDelta
            // => newHandWorld = itemAttachPoint_world * inverse(interactorDelta)
            Quaternion attachedHandRot = itemAttachPoint.rotation * Quaternion.Inverse(interactorDeltaRot);
            Vector3 attachedHandPos = itemAttachPoint.TransformPoint(interactorDeltaPos);
        
            // Apply via rigidbody (physics-friendly)
            _rigidbody.MoveRotation(attachedHandRot);
            _rigidbody.MovePosition(attachedHandPos);
        }

        void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _handColliders = GetComponentsInChildren<Collider>();
            _xrOrigin = GetComponentInParent<XROrigin>();

            foreach (var handInteractor in GetComponentsInChildren<XRBaseInteractor>())
            {
                //Update hand colliders when selecting/exiting interactable
                handInteractor.selectEntered.AddListener(_ => UpdateHandColliders());
                handInteractor.selectExited.AddListener(_ => UpdateHandColliders());
                
                //Record last grab time
                handInteractor.selectExited.AddListener(_ => _lastGrabTime = Time.time);
                
                //Align Hands With Attach point when selecting
                handInteractor.selectEntered.AddListener(args => AlignHandsWithInteractable(
                    (XRDirectInteractor)args.interactorObject,
                    args.interactableObject.GetAttachTransform(args.interactorObject)));
                
            }
        }
        
        private void MarkXRGrabbableTargetPoseDirty(XRGrabInteractable grabbable)
        {
            // Get the private field 'm_IsTargetPoseDirty'
            FieldInfo fieldInfo = typeof(XRGrabInteractable).GetField("m_IsTargetPoseDirty", BindingFlags.NonPublic | BindingFlags.Instance);
        
            if (fieldInfo == null)
            {
                Debug.LogWarning("Could not find m_IsTargetPoseDirty field.");
                return;
            }
            
            fieldInfo.SetValue(grabbable, true);
        }
    }
}
