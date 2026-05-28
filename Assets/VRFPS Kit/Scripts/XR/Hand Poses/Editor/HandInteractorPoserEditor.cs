#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace VRFPSKit.HandPoses
{
    [CustomEditor(typeof(HandInteractorPoser))]
    public class HandInteractorPoserEditor : Editor
    {
        private HandInteractorPoser poser = null;
        private Transform activeJoint = null;

        private void OnEnable()
        {
            poser = target as HandInteractorPoser;
        }

        private void OnSceneGUI()
        {
            Debug_DrawHandPoseOrientationEndPoints();
        }
        private void Debug_DrawHandPoseOrientationEndPoints()
        {
            PoseInfo poseInfo = poser.defaultPose.GetPoseInfoForHand(poser.HandType);
            List<DigitOrientation> orientations = new List<DigitOrientation>();
            orientations.AddRange(poseInfo.indexOrientation);
            orientations.AddRange(poseInfo.middleOrientation);
            orientations.AddRange(poseInfo.ringOrientation);
            orientations.AddRange(poseInfo.pinkyOrientation);
            orientations.AddRange(poseInfo.thumbOrientation);

            foreach (DigitOrientation orientation in orientations)
            {
                Handles.Button(poser.handOrientationTransform.TransformPoint(orientation.relativeDigitEndPoint),
                    Quaternion.identity, 0.01f, 0.005f, Handles.SphereHandleCap);
            }
        }
    }
}
#endif