using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRFPSKit.HandPoses
{
    [CreateAssetMenu(fileName = "NewPoseData")]
    public class Pose : ScriptableObject
    {
        // Info for each hand
        public PoseInfo leftHandInfo = PoseInfo.Empty;
        public PoseInfo rightHandInfo = PoseInfo.Empty;

        public PoseInfo GetPoseInfoForHand(InteractorHandedness handType)
        {
            // Return Left or Right, you can use a dictionary or different pose appliers
            switch (handType)
            {
                case InteractorHandedness.Left:
                    return leftHandInfo;
                case InteractorHandedness.Right:
                    return rightHandInfo;
                case InteractorHandedness.None:
                    return PoseInfo.Empty;
            }

            // Return an empty 
            return PoseInfo.Empty;
        }

        public bool IsUsingOldQuaternionBasedPosing() => leftHandInfo.IsUsingOldQuaternionBasedPosing() || rightHandInfo.IsUsingOldQuaternionBasedPosing();
    }
}