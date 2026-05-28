using System;
using UnityEngine;

namespace VRFPSKit
{
    public class IKTargetFollower : MonoBehaviour
    {
        public Transform transformToFollow;

        private void FixedUpdate()
        {
            //We can only update IK targets in FixedUpdate to stay in sync with physics updates
            transform.position = transformToFollow.position;
            transform.rotation = transformToFollow.rotation;
        }
    }
}
