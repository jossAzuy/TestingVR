using UnityEngine;

namespace VRFPSKit {
    public class VRBodyVestTargetTracking : MonoBehaviour
    {
        public VRBodySkinBinder skinBinder;

        // Update is called once per frame
        void Update()
        {
            if (skinBinder.GetSkin() is not { } skin) return;
            if (skin.vestTarget is not { } vestTarget)
            {
                Debug.LogWarning($"Player skin '{skin.name}' doesn't have a vest target assigned. Cannot track vest.");
                return;
            }
            
            transform.position = vestTarget.position;
            transform.rotation = vestTarget.rotation;
        }
    }
}