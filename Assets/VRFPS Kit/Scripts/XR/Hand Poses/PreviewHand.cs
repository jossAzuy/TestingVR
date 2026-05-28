using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VRFPSKit.HandPoses
{
    [SelectionBase]
    [ExecuteInEditMode]
    public class PreviewHand : BaseHand
    {
        public void MirrorAndApplyPose(PreviewHand sourceHand)
        {
            // Mirror and apply the joint values
            PoseInfo pose = PoseInfo.FromPreviewHand(sourceHand);
            PoseInfo mirroredPose = GetMirroredPose(pose);
            ApplyPoseInfo(mirroredPose);
        }

        private static PoseInfo GetMirroredPose(PoseInfo info)
        {
            PoseInfo mirroredPose = new PoseInfo(
                MirrorPosition(info.attachPosition),
                MirrorRotation(info.attachRotation),
                MirrorDigitOrientations(info.indexOrientation),
                MirrorDigitOrientations(info.middleOrientation),
                MirrorDigitOrientations(info.ringOrientation),
                MirrorDigitOrientations(info.pinkyOrientation),
                 MirrorDigitOrientations(info.thumbOrientation)
            );
            
            return mirroredPose;
        }

        private static Quaternion MirrorRotation(Quaternion mirrorRotation)
        {
            mirrorRotation.y *= -1.0f;
            mirrorRotation.z *= -1.0f;
            return mirrorRotation;
        }

        private static Vector3 MirrorPosition(Vector3 sourceHand)
        {
            Vector3 mirroredPosition = sourceHand;
            mirroredPosition.x *= -1.0f;
            return mirroredPosition;
        }
        
        private static DigitOrientation[] MirrorDigitOrientations(DigitOrientation[] originalOrientations)
        {
            List<DigitOrientation> mirroredOrientations = new List<DigitOrientation>();

            foreach (DigitOrientation orientation in originalOrientations)
            {
                DigitOrientation mirroredOrientation = new DigitOrientation
                {
                    relativeDigitEndPoint = Vector3.Scale(orientation.relativeDigitEndPoint, new Vector3(-1, 1, 1)),
                };
                mirroredOrientations.Add(mirroredOrientation);
            }

            return mirroredOrientations.ToArray();
        }
    }
}