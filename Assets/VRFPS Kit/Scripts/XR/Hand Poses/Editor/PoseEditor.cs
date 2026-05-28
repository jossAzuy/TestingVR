#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using VRFPSKit.HandPoses; // your Pose type namespace

namespace VRFPSKit.HandPoses
{
    [CustomEditor(typeof(Pose))]
    public class PoseEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // draw the default inspector first
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            // target cast
            Pose pose = (Pose)target;

            // Show status and button depending on NeedsUpgrade()
            if (pose.IsUsingOldQuaternionBasedPosing())
            {
                EditorGUILayout.HelpBox("This Pose asset requires an upgrade. Upgrade it by opening it on an interactable", MessageType.Warning);
            }
        }
    }
}

#endif
