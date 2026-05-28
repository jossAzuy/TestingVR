#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace VRFPSKit.HandPoses
{
    [CustomEditor(typeof(XRHandPoseContainer))]
    public class PoseContainerEditor : Editor
    {
        private XRHandPoseContainer poseContainer = null;

        private void OnEnable()
        {
            if(target is XRHandPoseContainer container)
                poseContainer = container;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Open Pose Editor"))
                PoseWindow.Open(poseContainer.pose, poseContainer);
        }
    }
}
#endif
