using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Climbing;

namespace VRFPSKit
{
    [RequireComponent(typeof(ClimbInteractable))]
    public class Ladder : MonoBehaviour
    {
        [Space]
        [Tooltip("If player is higher than this relative Y level, climb interaction will be ended and player will be teleported to top")] 
        public float maxClimbY = 2f;
        public Transform climbTarget;
        public bool useTeleportTargetYaw;
        [Space][Space]
        public AudioSource climbSound;

        private ClimbProvider _currentClimber;
        private ClimbInteractable _climbInteractable;
        private float _climbStartTime;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            _climbInteractable = GetComponent<ClimbInteractable>();

            //Play sound when selected
            _climbInteractable.selectEntered.AddListener(ClimbGrabSound);

            //Track the current climb provider
            _climbInteractable.selectEntered.AddListener(_ => _currentClimber = _climbInteractable.climbProvider);
            
            //Call climb start and end events
            _climbInteractable.selectExited.AddListener(_ =>
            {
                //Make sure climb interaction is completely over (we account for two hands possibly grabbing)
                if (_climbInteractable.isSelected) return;
                
                OnClimbEnd();
            });
            _climbInteractable.selectEntered.AddListener(_ =>
            {
                //Make sure climb interaction just started
                if (!_climbInteractable.isSelected) return;
                
                OnClimbStart();
            });
        }

        // Update is called once per frame
        private void Update()
        {
            TryEndClimbBecauseTooHighPosition();
        }

        /// <summary>
        /// Will be called when climb interaction is started
        /// (only first grab)
        /// </summary>
        private void OnClimbStart()
        {
            _climbStartTime = Time.time;
        }

        /// <summary>
        /// Will be called when climb interaction is completely over
        /// (no hands holding the ladder anymore)
        /// </summary>
        private void OnClimbEnd()
        {
            //Teleport player to bottom if it is lower than the ladder
            //(Prevents phasing through floor)
            if(GetClimberRelativeYPosition() < 0)
                TeleportClimberToBottom();

            //Teleport player to top target if it is higher than configured max (e.g. reached the top)
            if(GetClimberRelativeYPosition() > maxClimbY)
                TeleportClimberToTop();

            _currentClimber = null;
        }

        private void TryEndClimbBecauseTooHighPosition()
        {
            if (!_climbInteractable.isSelected) return; //No teleport if not climbing
            if (_currentClimber == null) return;
            if (climbTarget == null) return;
            if (GetClimberRelativeYPosition() < maxClimbY) return;
            if (Time.time - _climbStartTime < 0.2f) return; //Prevent immediate teleport on grab (Initial values can be broken)

            TeleportClimberToTop();

            //Cancel ladder interaction
            _climbInteractable.interactionManager.CancelInteractableSelection((IXRSelectInteractable)_climbInteractable);
        }

        private void TeleportClimberToBottom()
        {
            if (_currentClimber == null)
            {
                Debug.LogWarning("TeleportClimberToBottom(), climber was null!");
                return;
            }
    
            //Teleport player to bottom of ladder
            Vector3 climberPos = _currentClimber.transform.position;
            climberPos.y = transform.position.y;
            _currentClimber.transform.position = climberPos;
        }

        private void TeleportClimberToTop()
        {
            if (climbTarget == null) return;
            if (_currentClimber == null)
            {
                Debug.LogWarning("TeleportClimberToTop(), climber was null!");
                return;
            }

            //Teleport to climb target
            _currentClimber.transform.position = climbTarget.position;
            
            if (useTeleportTargetYaw)
            {
                //Grab Yaw from target
                Vector3 currentEuler = _currentClimber.transform.eulerAngles;
                Vector3 targetEuler = climbTarget.eulerAngles;
                currentEuler.y = targetEuler.y;

                _currentClimber.transform.eulerAngles = currentEuler;
            }
        }

        private float GetClimberRelativeYPosition()
        {
            if (_currentClimber == null) return 0;
            Vector3 climberRelativePos = _currentClimber.transform.position - transform.position;

            return climberRelativePos.y;
        }

        // This method is called by the editor to draw gizmos
        private void OnDrawGizmosSelected()
        {
            // Save current matrix
            Matrix4x4 oldMatrix = Gizmos.matrix;

            // Apply this object’s local transform matrix
            Gizmos.matrix = transform.localToWorldMatrix;
            
            // Draw a plane at the bottom of the ladder
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(1, .001f, 1));
            
            // Draw a plane at the maximum Y level for climbing
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(new Vector3(0, maxClimbY, 0), new Vector3(1, .001f, 1));
            
            // Restore matrix so you don’t mess up other gizmos
            Gizmos.matrix = oldMatrix;
        }

        private void ClimbGrabSound(SelectEnterEventArgs args)
        {
            if (climbSound == null) return;
            
            //Move sound to grab position
            Vector3 grabPos = args.interactorObject.transform.position;
            climbSound.transform.position = grabPos;
            climbSound.Play();
        }
    }
}
