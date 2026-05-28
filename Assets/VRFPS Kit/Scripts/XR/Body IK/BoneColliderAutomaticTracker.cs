using System;
using UnityEngine;

namespace VRFPSKit
{
public class BoneColliderAutomaticTracker : MonoBehaviour
{
    public HumanBodyBones humanBodyBone;

    private Transform _bone;
    private Vector3 _localPositionOffset;
    private Quaternion _localRotationOffset;

    private void Awake()
    {
        // Find VRBodySkin in parents
        var vrBodySkin = GetComponentInParent<VRBodySkin>();
        if (vrBodySkin == null)
        {
            Debug.LogError($"[{name}] No VRBodySkin found in parents.");
            enabled = false;
            return;
        }

        // Ensure animator exists
        var animator = vrBodySkin.skinAnimator;
        if (animator == null)
        {
            Debug.LogError($"[{name}] VRBodySkin found, but animator is null.");
            enabled = false;
            return;
        }

        // Get the bone transform from animator
        _bone = animator.GetBoneTransform(humanBodyBone);
        if (_bone == null)
        {
            Debug.LogError($"[{name}] Bone '{humanBodyBone}' not found in Humanoid rig!");
            enabled = false;
            return;
        }

        // Calculate offsets in *bone's local space*
        _localPositionOffset = _bone.InverseTransformPoint(transform.position);
        _localRotationOffset = Quaternion.Inverse(_bone.rotation) * transform.rotation;
    }

    private void LateUpdate()
    {
        if (_bone == null) return;

        // Apply tracked position + rotation with offsets
        transform.position = _bone.TransformPoint(_localPositionOffset);
        transform.rotation = _bone.rotation * _localRotationOffset;
    }
}
}