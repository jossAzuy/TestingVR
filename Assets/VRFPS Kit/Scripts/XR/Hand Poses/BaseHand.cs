using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRFPSKit.HandPoses
{
    public abstract class BaseHand : MonoBehaviour
    {
        private const int INDEX_DEPTH = 3;
        private const int MIDDLE_DEPTH = 3;
        private const int RING_DEPTH = 3;
        private const int PINKY_DEPTH = 3;
        private const int THUMB_DEPTH = 3;

        [Tooltip("A lot of hand skeletons use a different forward direction for fingers, define this forward direction accordingly here.")]
        public Vector3 digitForwardDirection = Vector3.forward;
        [Tooltip("Neutral pose for the hand")]
        public Pose defaultPose = null;
        
        [Tooltip("Which is the transform that will orient the hand according to hand pose offsets etc.")]
        public Transform handOrientationTransform;
        [Space] 
        public Transform indexRoot;
        public Transform middleRoot;
        public Transform ringRoot;
        public Transform pinkyRoot;
        public Transform thumbRoot;
        [Space]
        [Space]

        // What kind of hand is this?
        [SerializeField] protected InteractorHandedness handType = InteractorHandedness.None;
        public InteractorHandedness HandType => handType;
        
        [HideInInspector] public Transform currentAttachTransform;

        #region Digit Orientations
        
        [HideInInspector] public Transform[] indexJoints;
        [HideInInspector] public Transform[] middleJoints;
        [HideInInspector] public Transform[] ringJoints;
        [HideInInspector] public Transform[] pinkyJoints;
        [HideInInspector] public Transform[] thumbJoints;

        private float[] _digitDefaultTwistAngles; //Tracks default twist angles for each digit so we have a reference point
        
        public Transform[] AllJoints() => Array.Empty<Transform>().Concat(indexJoints).Concat(middleJoints).Concat(ringJoints).Concat(pinkyJoints).Concat(thumbJoints).ToArray();

        public DigitOrientation[] GetJointOrientations(Transform[] joints)
        {
            List<DigitOrientation> orientations = new List<DigitOrientation>();

            for (int i = 0; i < joints.Length - 1; i++)
            {
                Transform joint = joints[i];
                Transform nextJoint = joints[i + 1];
                Vector3 endPoint = nextJoint.position;
                Vector3 relativeEndPoint = InverseTransformPointUnscaled(handOrientationTransform, endPoint);

                orientations.Add(new DigitOrientation(relativeEndPoint));
            }

            return orientations.ToArray();
        }
        
        private void ApplyFingerOrientations(Transform[] joints, DigitOrientation[] digitOrientations)
        {
            if (digitOrientations == null || digitOrientations.Length == 0)
            {
                Debug.LogError("Trying to apply empty hand pose.");
                return;
            }

            // axis in joint-local space that represents the finger forward (e.g. Vector3.forward)
            Vector3 axisLocal = digitForwardDirection.normalized;

            for (int i = 0; i < joints.Length - 1; i++)
            {
                Transform joint = joints[i];
                DigitOrientation orientation = digitOrientations[i];

                // target in world space
                Vector3 worldEndPoint = TransformPointUnscaled(handOrientationTransform, orientation.relativeDigitEndPoint);
                Vector3 toTarget = worldEndPoint - joint.position;
                if (toTarget.sqrMagnitude < 1e-8f) continue;
                Vector3 targetDir = toTarget.normalized;

                // --- 1) extract current twist (roll) from local rotation around axisLocal ---
                Quaternion currentLocal = joint.localRotation;
                DecomposeSwingTwist(currentLocal, axisLocal, out Quaternion currentSwing, out Quaternion storedTwist);
                // storedTwist is twist in local space that we want to keep

                // --- 2) align forward in WORLD space: take joint's current forward (world) -> targetDir ---
                Vector3 currentFwdWorld = joint.TransformDirection(digitForwardDirection).normalized;
                Quaternion alignWorld = Quaternion.FromToRotation(currentFwdWorld, targetDir);
                Quaternion newWorldRot = alignWorld * joint.rotation; // world rotation after aligning forward

                // --- 3) convert aligned world rotation back to local (respect parent) ---
                Quaternion parentWorld = joint.parent != null ? joint.parent.rotation : Quaternion.identity;
                Quaternion newLocal = Quaternion.Inverse(parentWorld) * newWorldRot;
                newLocal = Normalize(newLocal);

                // --- 4) remove twist from newLocal (get its swing) and then reapply storedTwist: finalLocal = swingNew * storedTwist ---
                DecomposeSwingTwist(newLocal, axisLocal, out Quaternion newSwing, out Quaternion newTwist);
                Quaternion finalLocal = newSwing * storedTwist;
                finalLocal = Normalize(finalLocal);

                joint.localRotation = finalLocal;
            }
        }

        #endregion

        private void SetDigitTwist(Transform joint, float twistAngle)
        {
            int jointIndex = Array.IndexOf(AllJoints(), joint);
            if (jointIndex == -1)
            {
                Debug.LogWarning($"Joint {joint.name} not found in AllJoints().");
                return;
            }
            
            float defaultTwistAngle = _digitDefaultTwistAngles[jointIndex];
            Vector3 jointRot = joint.localRotation.eulerAngles;
            jointRot.y = defaultTwistAngle + twistAngle;
            //joint.localRotation = Quaternion.Euler(jointRot);
        }
        
        protected virtual void Awake()
        {
            CollectJoints();
            PopulateDigitDefaultTwists();
            
            if(handOrientationTransform == null)
                Debug.LogError($"{name} BaseHand: Hand Orientation Transform is not assigned in the inspector.");
        }
        
        private Transform[] CollectJoints(Transform root, int depth)
        {
            List<Transform> result = new List<Transform> { root };

            if (depth <= 1) return result.ToArray();
            
            //Throw if there are not enough children
            if (root.childCount == 0) throw new System.InvalidOperationException($"Couldn't collect finger joints, finger root {root.name} does not have enough children. Is your hand skeleton missing end bones?");
            
            Transform child = root.GetChild(0);
            result.AddRange(CollectJoints(child, depth - 1));

            return result.ToArray();
        }

        public void ApplyDefaultPose()
        {
            ApplyPose(defaultPose);
        }

        public void ApplyPose(Pose pose)
        {
            // Get the proper info using hand's type
            PoseInfo poseInfo = pose.GetPoseInfoForHand(handType);

            ApplyPoseInfo(poseInfo);
        }
        
        public void ApplyPoseInfo(PoseInfo poseInfo)
        {
            // Position, and rotate, this differs on the type of hand
            ApplyOffset(poseInfo.attachPosition, poseInfo.attachRotation);

            //Still support old Quaternion-based posing for now, just so upgrading old poses is possible
            if (poseInfo.IsUsingOldQuaternionBasedPosing())
            {
                Debug.LogError($"Hand pose is using old Quaternion-based posing. Please upgrade the pose by opening it.");
                ApplyFingerRotations(poseInfo);
                return;
            }
            
            // Apply finger orientations 
            ApplyFingerOrientations(indexJoints, poseInfo.indexOrientation);
            ApplyFingerOrientations(middleJoints, poseInfo.middleOrientation);
            ApplyFingerOrientations(ringJoints, poseInfo.ringOrientation);
            ApplyFingerOrientations(pinkyJoints, poseInfo.pinkyOrientation);
            ApplyFingerOrientations(thumbJoints, poseInfo.thumbOrientation);
        }

        #region Hand Orientation
        protected void ApplyOffset(Vector3 positionOffset, Quaternion rotationOffset)
        {
            if(currentAttachTransform == null)
            {
                handOrientationTransform.localPosition = positionOffset;
                handOrientationTransform.localRotation = rotationOffset;
                return;
            }
            
            handOrientationTransform.rotation = currentAttachTransform.rotation * rotationOffset;
            handOrientationTransform.position = TransformPointUnscaled(currentAttachTransform, positionOffset);
        }

        public Vector3 GetAttachPositionOffset()
        {
            if (!currentAttachTransform) return handOrientationTransform.localPosition;

            return InverseTransformPointUnscaled(currentAttachTransform, handOrientationTransform.position);
        }

        public Quaternion GetAttachRotationOffset()
        {
            if (!currentAttachTransform) return handOrientationTransform.localRotation;
            
            return Quaternion.Inverse(currentAttachTransform.rotation) * handOrientationTransform.rotation;
        }
        
        private static Vector3 TransformPointUnscaled(Transform transform, Vector3 position)
        {
            var localToWorldMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            return localToWorldMatrix.MultiplyPoint3x4(position);
        }
        
        private static Vector3 InverseTransformPointUnscaled(Transform transform, Vector3 position)
        {
            var worldToLocalMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).inverse;
            return worldToLocalMatrix.MultiplyPoint3x4(position);
        }
        #endregion

        #region Old Quaternion-based Posing (Deprecated)
        private List<Transform> GetOldJoints(){
            List<Transform> result = new List<Transform>();
            
            //These are how we collected joints in the old system, keep it for backwards compatibility
            result.AddRange(CollectJoints(indexRoot, 4));
            result.AddRange(CollectJoints(middleRoot, 4 ));
            result.AddRange(CollectJoints(ringRoot, 4));
            result.AddRange(CollectJoints(pinkyRoot, 4));
            result.AddRange(CollectJoints(thumbRoot.GetChild(0), 3));
            
            return result;
        }

        private void ApplyFingerRotations(PoseInfo pose)
        {
#pragma warning disable CS0618
            List<Quaternion> rotations = pose.fingerRotations;
            var joints = GetOldJoints();
            // Set the local rotation of each joint
            for (int i = 0; i < joints.Count; i++)
                joints[i].localRotation = rotations[i];
#pragma warning restore CS0618
        }
        #endregion
        
        private void CollectJoints()
        {
            if(thumbRoot == null || indexRoot == null || middleRoot == null || ringRoot == null || pinkyRoot == null)
                Debug.LogError("One or more finger roots are not assigned in the inspector. Please assign all finger roots.");
            
            indexJoints = CollectJoints(indexRoot, INDEX_DEPTH + 1);
            middleJoints = CollectJoints(middleRoot, MIDDLE_DEPTH + 1);
            ringJoints = CollectJoints(ringRoot, RING_DEPTH + 1);
            pinkyJoints = CollectJoints(pinkyRoot, PINKY_DEPTH + 1);
            thumbJoints = CollectJoints(thumbRoot, THUMB_DEPTH + 1);
        }
        
        private void PopulateDigitDefaultTwists()
        {
            var joints = AllJoints();
            _digitDefaultTwistAngles = new float[joints.Length];

            for (int i = 0; i < joints.Length; i++)
                _digitDefaultTwistAngles[i] = joints[i].localEulerAngles.y;
        }
        
        private static Quaternion Normalize(Quaternion q)
        {
            float m = Mathf.Sqrt(q.x*q.x + q.y*q.y + q.z*q.z + q.w*q.w);
            if (m > 1e-6f) return new Quaternion(q.x/m, q.y/m, q.z/m, q.w/m);
            return Quaternion.identity;
        }

        /// <summary>
        /// Decompose q (in the same space as axisLocal) into swing and twist where twist rotates around axisLocal.
        /// Returns (swing, twist) and guarantees q ~= swing * twist.
        /// axisLocal must be normalized.
        /// </summary>
        private static void DecomposeSwingTwist(Quaternion q, Vector3 axisLocal, out Quaternion swing, out Quaternion twist)
        {
            // project q's vector part onto axis
            Vector3 r = new Vector3(q.x, q.y, q.z);
            Vector3 proj = Vector3.Project(r, axisLocal);

            twist = new Quaternion(proj.x, proj.y, proj.z, q.w);
            twist = Normalize(twist);

            // ensure twist has same sign as q (optional but avoids 180° sign flips)
            if (Quaternion.Dot(twist, q) < 0f)
                twist = new Quaternion(-twist.x, -twist.y, -twist.z, -twist.w);

            swing = q * Quaternion.Inverse(twist);
            swing = Normalize(swing);
        } 
    }
}
