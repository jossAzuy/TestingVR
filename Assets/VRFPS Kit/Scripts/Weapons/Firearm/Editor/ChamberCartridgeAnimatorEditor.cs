#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRFPSKit;

[CustomEditor(typeof(ChamberCartridgeAnimator))]
public class ChamberCartridgeAnimatorEditor : Editor
{
    //private bool _editMode = false;
    
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();
        
        // Reference to the target script
        ChamberCartridgeAnimator animator = (ChamberCartridgeAnimator)target;

        if (GUILayout.Button("Mirror all"))
        {
            ChamberCartridgeAnimationKeyframe[] mirroredKeyframes = new ChamberCartridgeAnimationKeyframe[animator.keyframes.Length];
            foreach (ChamberCartridgeAnimationKeyframe keyframe in animator.keyframes)
                mirroredKeyframes[System.Array.IndexOf(animator.keyframes, keyframe)] = keyframe.GetMirrored(animator.mirrorAxis, animator.mirrorRotation);
            
            animator.keyframes = mirroredKeyframes;
            EditorUtility.SetDirty(animator);
        }
            /*
        if (_editMode)
        {
            if (GUILayout.Button("Save", GUILayout.Height(30)))
            {
            }
        }
        else
        {
            if (GUILayout.Button("Edit", GUILayout.Height(30)))
            {
            }
        }*/
    }
}
#endif