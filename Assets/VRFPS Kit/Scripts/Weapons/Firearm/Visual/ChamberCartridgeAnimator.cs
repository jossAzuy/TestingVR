using System;
using System.Linq;
using UnityEngine;
using VRFPSKit;

public class ChamberCartridgeAnimator : MonoBehaviour
{
    [Header("Flip feed animation on an axis to match magazine feeding. Default side depends on weapon so look up first")]
    [Tooltip("Set to -1 on axes you want mirrored. (1 = normal, -1 = mirrored)")]
    public Vector3 mirrorAxis = Vector3.one;
    [Tooltip("The transform (usually the weapon's bolt) that pushes the cartridge into the chamber")]
    public Transform cartridgePusherTransform;
    public bool mirrorRotation = true;
    [Space]
    [Header("Feed Animation Keyframes")]
    public ChamberCartridgeAnimationKeyframe[] keyframes;
    public Vector3 cartridgePivotOffset;

    private float _closestPusherTransformDistanceYet;
    private Vector3 _offsetFromPusherToCartridgePivot;
    private Vector3 _defaultPosition;
    private Quaternion _defaultRotation;
    
    private FirearmCyclingAction _cyclingAction;
    private Firearm _firearm;

    private void LateUpdate()
    {
        if (IsPushingCartridge())
        {
            AnimatePath();
            _closestPusherTransformDistanceYet = GetOffsetDelta().magnitude;
        }
        
        if (_closestPusherTransformDistanceYet < .01f)
            //Follow back during ejection
            transform.localPosition = _defaultPosition + GetOffsetDelta();
        
        if(_firearm.chamberCartridge.IsNull() && _cyclingAction.GetLoadingCartridge().IsNull()) 
            _closestPusherTransformDistanceYet = Mathf.Infinity;
        
        //TODO CartridgeRenderer 2 frame delay to show
    }
    
    private void AnimatePath()
    {
        if (keyframes.Length < 2) return;
        
        float animationZ = GetOffsetDelta().z;
        ChamberCartridgeAnimationKeyframe lerpedKeyframe = GetLerpedKeyframeForZ(animationZ);
        
        if(ShouldMirror())
            lerpedKeyframe = lerpedKeyframe.GetMirrored(mirrorAxis, mirrorRotation);
        
        transform.localPosition = _defaultPosition + lerpedKeyframe.position;
        transform.localRotation = _defaultRotation * Quaternion.Euler(lerpedKeyframe.rotation);
    }

    public ChamberCartridgeAnimationKeyframe GetLerpedKeyframeForZ(float zPos)
    {
        ChamberCartridgeAnimationKeyframe[] sortedKeyframes = keyframes.OrderBy(k => k.position.z).ToArray();
        
        ChamberCartridgeAnimationKeyframe keyBefore = sortedKeyframes[0];
        ChamberCartridgeAnimationKeyframe keyAfter = sortedKeyframes[sortedKeyframes.Length - 1];
        for (int i = 0; i < sortedKeyframes.Length - 1; i++)
        {
            float keyframe0 = sortedKeyframes[i].position.z;
            float keyframe1 = sortedKeyframes[i + 1].position.z;

            if (zPos >= keyframe0 && zPos <= keyframe1 || 
                zPos <= keyframe0 && zPos >= keyframe1)
            {
                keyBefore = sortedKeyframes[i];
                keyAfter = sortedKeyframes[i + 1];
                break;
            }
        }
        
        float interpolationT01 = Mathf.Clamp01(Mathf.InverseLerp(keyBefore.position.z, keyAfter.position.z, zPos));
        Vector3 lerpPos = Vector3.Lerp(keyBefore.position, keyAfter.position, interpolationT01);
        Vector3 lerpRot = Quaternion.Lerp(Quaternion.Euler(keyBefore.rotation), Quaternion.Euler(keyAfter.rotation), interpolationT01).eulerAngles;
        
        return new ChamberCartridgeAnimationKeyframe(lerpPos, lerpRot);
    }
    
    private bool IsPushingCartridge() => GetOffsetDelta().magnitude < _closestPusherTransformDistanceYet;

    private bool ShouldMirror()
    {
        if (_firearm.magazine == null) return false;
        if (_firearm.magazine.GetComponentInChildren<MagazineCartridgeFeedRenderer>() is not MagazineCartridgeFeedRenderer feedRenderer) return false;
        if (feedRenderer.ShouldFlipCartridges()) return false;

        return true;
    }

    private void Awake()
    {
        if(cartridgePusherTransform == null) Debug.LogError("cartridgePusherTransform is null on ChamberCartridgeAnimator");
        
        _cyclingAction = GetComponentInParent<FirearmCyclingAction>();
        _firearm = GetComponentInParent<Firearm>();
        
        _offsetFromPusherToCartridgePivot = Vector3.zero;
        _offsetFromPusherToCartridgePivot = GetOffsetDelta();

        _defaultPosition = transform.localPosition;
        _defaultRotation = transform.localRotation;
    }
    
    private Vector3 GetOffsetDelta()
    {
        Vector3 pusherInParent = transform.parent.InverseTransformPoint(cartridgePusherTransform.position);
        //Vector3 pivotInParent   = transform.parent.InverseTransformPoint(transform.position);
        Vector3 currentOffset = pusherInParent;// - pivotInParent;
        return currentOffset - _offsetFromPusherToCartridgePivot;
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        //Gizmos dont support showing in play mode
        if (Application.isPlaying) return;
        
        foreach (var keyframe in keyframes)
        {
            DrawPrefabGizmos(gameObject, transform, keyframe, Color.white);
            //Handles.Label(i);
            
            //Draw mirrored keyframes
            //if(mirrorAxis != Vector3.one)
            //    DrawPrefabGizmos(gameObject, transform, keyframe.GetMirrored(mirrorAxis, mirrorRotation), new Color(.1f,.1f,.1f));
        }
    }
    private void DrawPrefabGizmos(GameObject prefab, Transform parent, ChamberCartridgeAnimationKeyframe keyframe, Color color)
    {
        Vector3 place = keyframe.position - cartridgePivotOffset;
        Quaternion rot = Quaternion.Euler(keyframe.rotation) * transform.rotation;

        Matrix4x4 parentMatrix = parent.localToWorldMatrix;
        Matrix4x4 prefabRootMatrix = parentMatrix * Matrix4x4.TRS(place, rot, Vector3.one);

        var previousMatrix = Gizmos.matrix;
        Gizmos.color = color;

        foreach (var mesh in prefab.GetComponentsInChildren<MeshFilter>())
        {
            if (mesh.sharedMesh == null) continue;

            // build chain of transforms from prefab root -> ... -> mesh
            var chain = new System.Collections.Generic.List<Transform>();
            Transform t = mesh.transform;
            while (t != null && t != prefab.transform)
            {
                chain.Add(t);
                t = t.parent;
            }
            chain.Reverse(); // now in root->child->...->mesh order

            // compose local matrix from prefab root to mesh
            Matrix4x4 localFromRoot = Matrix4x4.identity;
            foreach (var node in chain)
            {
                localFromRoot = localFromRoot * Matrix4x4.TRS(node.localPosition, node.localRotation, node.localScale);
            }

            // apply prefab root placement then the local-from-root to get final gizmo matrix for this mesh
            Gizmos.matrix = prefabRootMatrix * localFromRoot;

            // draw mesh at origin (it will be transformed by Gizmos.matrix)
            Gizmos.DrawWireMesh(mesh.sharedMesh);
        }

        Gizmos.matrix = previousMatrix;
    }

#endif

    private void OnValidate()
    {
        if(keyframes != null && keyframes.Length > 0)
        {
            //Always sort keyframes by time
            Array.Sort(keyframes, (a, b) =>
                Mathf.Abs(a.position.z).CompareTo(Mathf.Abs(b.position.z)));
        }
    }
}