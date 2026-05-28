using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRFPSKit.HandPoses
{
    [Serializable]
    public class PoseInfo
    {
        // Used for upgrading old poses that used absolute rotations instead of offsets
        public static readonly List<Quaternion> OldDefaultFingerRotations = new List<Quaternion> { Quaternion.Euler(285.1245f, 106.6239f, 61.11311f), Quaternion.Euler(-6.767f, -4.17f, 338.53f), Quaternion.Euler(0, 0, 353.13f), Quaternion.Euler(0, 0, 0), Quaternion.Euler(279.6F, 18.78f, 151.9f), Quaternion.Euler(-1.28f, 3.884f, 332.78f), Quaternion.Euler(0, 0, 352.45f), Quaternion.Euler(0, 0, 0), Quaternion.Euler(290.8f, 338.5f, 190.55f), Quaternion.Euler(1.69f, 3.068f, 336.58f), Quaternion.Euler(-3.37f, 2.49f, 350.37f), Quaternion.Euler(0, 0, 0), Quaternion.Euler(359f, 357.93f, 166.5f), Quaternion.Euler(-6.14f, -1.3f, 341), Quaternion.Euler(3.58f, -9.9f, 354.6f), Quaternion.Euler(0, 0, 0), Quaternion.Euler(-4.72f, -1.6f, 343.63f), Quaternion.Euler(-1.76f, -7, 1), Quaternion.Euler(0, 0, 0), };
        
        public Vector3 attachPosition = Vector3.zero;
        public Quaternion attachRotation = Quaternion.identity;
        
        public DigitOrientation[] indexOrientation;
        public DigitOrientation[] middleOrientation;
        public DigitOrientation[] ringOrientation;
        public DigitOrientation[] pinkyOrientation;
        public DigitOrientation[] thumbOrientation;

        [Obsolete("Use new fingerRotationOffsets list instead.")] [HideInInspector]
        public List<Quaternion> fingerRotations = new();
        
        public static PoseInfo Empty => new PoseInfo(Vector3.zero, Quaternion.identity, Array.Empty<DigitOrientation>(), Array.Empty<DigitOrientation>(), Array.Empty<DigitOrientation>(), Array.Empty<DigitOrientation>(), Array.Empty<DigitOrientation>());
        
        public PoseInfo(Vector3 attachPosition, Quaternion attachRotation, 
            DigitOrientation[] indexOrientation, DigitOrientation[] middleOrientation, 
            DigitOrientation[] ringOrientation, DigitOrientation[] pinkyOrientation, 
            DigitOrientation[] thumbOrientation)
        {
            this.attachPosition = attachPosition;
            this.attachRotation = attachRotation;
            this.indexOrientation = indexOrientation;
            this.middleOrientation = middleOrientation;
            this.ringOrientation = ringOrientation;
            this.pinkyOrientation = pinkyOrientation;
            this.thumbOrientation = thumbOrientation;
        }
        
        public static PoseInfo FromPreviewHand(PreviewHand hand)
        {
            PoseInfo info = Empty;
            
            info.attachPosition = hand.GetAttachPositionOffset();
            info.attachRotation = hand.GetAttachRotationOffset();

            // Save rotations from the hand's current joints
            info.indexOrientation = hand.GetJointOrientations(hand.indexJoints);
            info.middleOrientation = hand.GetJointOrientations(hand.middleJoints);
            info.ringOrientation = hand.GetJointOrientations(hand.ringJoints);
            info.pinkyOrientation = hand.GetJointOrientations(hand.pinkyJoints);
            info.thumbOrientation = hand.GetJointOrientations(hand.thumbJoints);

            return info;
        }
        
#pragma warning disable CS0618
        public bool IsUsingOldQuaternionBasedPosing() => fingerRotations.Count > 0 && indexOrientation.Length == 0;
#pragma warning restore CS0618

        public static PoseInfo Lerp(PoseInfo a, PoseInfo b, float t)
        {
            PoseInfo result = new PoseInfo
            (
                Vector3.Lerp(a.attachPosition, b.attachPosition, t),
                Quaternion.Lerp(a.attachRotation, b.attachRotation, t),
                LerpFingerOrientation(a.indexOrientation, b.indexOrientation, t),
                LerpFingerOrientation(a.middleOrientation, b.middleOrientation, t),
                LerpFingerOrientation(a.ringOrientation, b.ringOrientation, t),
                LerpFingerOrientation(a.pinkyOrientation, b.pinkyOrientation, t),
                LerpFingerOrientation(a.thumbOrientation, b.thumbOrientation, t)
            );
            return result;
        }

        private static DigitOrientation[] LerpFingerOrientation(DigitOrientation[] a, DigitOrientation[] b, float t)
        {
            if (a.Length != b.Length)
            {
                Debug.LogError($"Trying to lerp between two DigitOrientation arrays of different lengths: {a.Length} and {b.Length}. Returning a.");
                return a;
            }
            
            DigitOrientation[] result = a.ToArray();
            
            Vector3 aDigitStartPoint = Vector3.zero; // Assuming the joint starts at the origin relative to the hand orientation transform
            Vector3 bDigitStartPoint = Vector3.zero; // Assuming the joint starts at the origin relative to the hand orientation transform
            Vector3 lastEndPoint = Vector3.zero;
            for (int i = 0; i < a.Length; i++)
            {
                Vector3 aDigitVector = (a[i].relativeDigitEndPoint - aDigitStartPoint);
                Vector3 bDigitVector = (b[i].relativeDigitEndPoint - bDigitStartPoint);
                
                Vector3 lerpedDirection = Vector3.Lerp(aDigitVector.normalized, bDigitVector.normalized, t).normalized;
                //Give the lerped direction length, use the average length of the two original vectors
                Vector3 lerpedDigitVector = lerpedDirection * (aDigitVector.magnitude + bDigitVector.magnitude) / 2f;
                Vector3 lerpedEndPoint = lastEndPoint + lerpedDigitVector;
                
                DigitOrientation newOrientation = new DigitOrientation(lerpedEndPoint);
                result[i] = newOrientation;

                //End point is the start point for the next joint
                aDigitStartPoint = a[i].relativeDigitEndPoint;
                bDigitStartPoint = b[i].relativeDigitEndPoint;
                lastEndPoint = newOrientation.relativeDigitEndPoint;
            }

            return result;
        }
    }
}