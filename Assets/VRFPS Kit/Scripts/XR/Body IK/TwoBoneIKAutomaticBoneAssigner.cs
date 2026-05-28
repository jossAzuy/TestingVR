using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;

namespace VRFPSKit
{
    [RequireComponent(typeof(TwoBoneIKConstraint))]
    public class TwoBoneIKAutomaticBoneAssigner : MonoBehaviour
    {
        public HumanBodyBones rootBone;
        public HumanBodyBones midBone;
        public HumanBodyBones tipBone;
        
        void Awake()
        {
            TwoBoneIKConstraint ikConstraint = GetComponent<TwoBoneIKConstraint>();
            Animator bodyAnimator = GetComponentInParent<VRBodySkin>()?.skinAnimator;
            if (bodyAnimator == null)
            {
                Debug.LogError($"TwoBoneIKAutomaticBoneAssigner couldn't find a VRBodySkin in its parents or the animator is null.", this);
                return;
            }
            
            ikConstraint.data.root = bodyAnimator.GetBoneTransform(rootBone);
            ikConstraint.data.mid = bodyAnimator.GetBoneTransform(midBone);
            ikConstraint.data.tip = bodyAnimator.GetBoneTransform(tipBone);
        }
    }
}
