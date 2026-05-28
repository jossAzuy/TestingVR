using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;

namespace VRFPSKit
{
    [RequireComponent(typeof(MultiParentConstraint))]
    public class MultiParentConstraintBoneAssigner : MonoBehaviour
    {
        public HumanBodyBones headBone;
        
        void Awake()
        {
            MultiParentConstraint ikConstraint = GetComponent<MultiParentConstraint>();
            Animator bodyAnimator = GetComponentInParent<VRBodySkin>()?.skinAnimator;
            if (bodyAnimator == null)
            {
                Debug.LogError($"HeadIKAutomaticBoneAssigner couldn't find a VRBodySkin in its parents or the animator is null.", this);
                return;
            }
            
            ikConstraint.data.constrainedObject = bodyAnimator.GetBoneTransform(headBone);
        }
    }
}
