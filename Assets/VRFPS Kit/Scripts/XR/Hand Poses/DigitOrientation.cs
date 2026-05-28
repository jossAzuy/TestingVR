using System;
using UnityEngine;

namespace VRFPSKit.HandPoses
{
    [Serializable]
    public struct DigitOrientation
    {
        //Point relative to the hand orientation transform, where digit should point to
        public Vector3 relativeDigitEndPoint;

        public DigitOrientation(Vector3 relativeDigitEndPoint)
        {
            this.relativeDigitEndPoint = relativeDigitEndPoint;
        }

        // Helper: world point from the handOrientationTransform
        public Vector3 GetWorldPoint(Transform handOrientationTransform)
        {
            return handOrientationTransform.TransformPoint(relativeDigitEndPoint);
        }
    }
}
