using System;
using UnityEngine;

namespace VRFPSKit
{

[Serializable]
public struct ChamberCartridgeAnimationKeyframe{
    public Vector3 position;
    public Vector3 rotation;
    
    public ChamberCartridgeAnimationKeyframe(Vector3 position, Vector3 rotation)
    {
        this.position = position;
        this.rotation = rotation;
    }

    public ChamberCartridgeAnimationKeyframe GetMirrored(Vector3 mirrorAxis, bool mirrorRotation)
    {
        Vector3 newPos = Vector3.Scale(position, mirrorAxis);
        Vector3 newRot = rotation;
        
        if (mirrorRotation)
        {
            // Convert rotation to matrix, apply scale flip, then back to quaternion
            Matrix4x4 m = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(rotation), Vector3.one);
            m = Matrix4x4.Scale(mirrorAxis) * m * Matrix4x4.Scale(mirrorAxis); 
            newRot = m.rotation.eulerAngles;
        }
        
        return new ChamberCartridgeAnimationKeyframe(newPos, newRot);
    }
}
}
