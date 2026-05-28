using UnityEngine;
using UnityEngine.Animations.Rigging;

public class HandIKTrackingDelayFix : MonoBehaviour
{
    private TwoBoneIKConstraint _ikConstraint;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _ikConstraint = GetComponent<TwoBoneIKConstraint>();
    }

    public void CatchUp()
    {
        _ikConstraint.data.tip.position = _ikConstraint.data.target.position; 
        _ikConstraint.data.tip.rotation = _ikConstraint.data.target.rotation; 
    }
}
