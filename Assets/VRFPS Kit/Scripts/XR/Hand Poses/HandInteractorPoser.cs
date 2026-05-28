using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRFPSKit.HandPoses
{
    //TODO needs to be derived from NetworkBehaviour so we can network ownership of hand poses and sync them across clients 
    //TODO player skins use this too, which means the networked component cant be on the skin, and needs to be part of the player
    public class HandInteractorPoser : BaseHand
    {
        public XRBaseInteractor[] interactors;
        [Tooltip("Clenched fist pose for the hand")]
        public Pose closedHandPose = null;
        public InputActionProperty gripAnimationAction;
        
        private XRHandPoseContainer _poseContainer;
        
        private void LateUpdate()
        {
            ApplyPoseInfo(GetCurrentPose());
            //TODO would be better if we only updated digits here, not hand offset.
            //     Hand offset is only relevant to update in FixedUpdate when physics moves the hand
        }
        
        private void FixedUpdate()
        {
            ApplyPoseInfo(GetCurrentPose());
            //TODO would be better if we only hand offset.
            //     Hand offset is only relevant to update in FixedUpdate when physics moves the hand
        }

        private PoseInfo GetCurrentPose()
        {
            if (_poseContainer == null)
                return GetEmptyHandedPose();
            
            return _poseContainer.pose.GetPoseInfoForHand(handType);
        }

        public void RefreshPose()
        {
            var interactor = GetActiveInteractor();
            var interactable = GetActiveInteractable();
            
            if (interactor == null || interactable == null) 
            {
                ApplyDefaultPose();
                return;
            }
            
            //subscribing to selectExited event on interactable is a good idea
            if (GetComponentInChildren<Animator>() is { } animator) animator.enabled = false;
            
            //Get index of current interactor in interactable selecting list
            int interactorIndex = interactable.interactorsSelecting.FindIndex(selectingInteractor => 
                (XRDirectInteractor)selectingInteractor == interactor);
            
            //TODO this is a temp fix for interactorIndex returning -1, need to find a better solution
            if(interactorIndex == -1) interactorIndex = 0; //Default to 0 if index not found
             
            //Find a hand pose with a matching interactor index
            foreach (var pose in interactable.transform.GetComponents<XRHandPoseContainer>())
                if(pose.interactorIndex == interactorIndex)
                    _poseContainer = pose;

            if (!_poseContainer)
            {
                Debug.LogWarning($"No hand pose container found for interactable {interactable.transform.gameObject.name} with interactor index: {interactorIndex}");
                return;
            }
            
            currentAttachTransform = _poseContainer.GetAttachPoint();
        }
        
        
        protected virtual PoseInfo GetEmptyHandedPose()
        {
            PoseInfo openHand = defaultPose.GetPoseInfoForHand(handType);
            PoseInfo closedHand = closedHandPose.GetPoseInfoForHand(handType);
            
            float gripValue = gripAnimationAction.action.ReadValue<float>();
            PoseInfo finalPose = PoseInfo.Lerp(openHand, closedHand, gripValue);

            return finalPose;
        }
        
        private XRDirectInteractor GetActiveInteractor()
        {
            foreach (XRDirectInteractor interactor in interactors)
                if(interactor.hasSelection)
                    return interactor;
            return null;
        }

        private IXRSelectInteractable GetActiveInteractable()
        {
            XRDirectInteractor interactor = GetActiveInteractor();
            if(interactor == null) return null;
            return interactor.firstInteractableSelected;
        }

        /// <summary>
        /// Called when one of the interactors enters selection.
        /// </summary>
        /// <param name="args"></param>
        private async void OnSelectEntered(SelectEnterEventArgs args)
        {
            GetActiveInteractable();
            //We need to cache interactable if we use Task.Delay(), will start referencing other interactables for some reason otherwise
            
            //Wait one frame for potential interactor transfer to complete before getting interactor index
            await Task.Delay(50);
            
            RefreshPose();
        }
        
        /// <summary>
        /// Called when one of the interactors exits selection.
        /// </summary>
        /// <param name="args"></param>
        private void OnSelectExited(SelectExitEventArgs args)
        {
            currentAttachTransform = ((XRDirectInteractor)args.interactorObject).attachTransform;
            _poseContainer = null;
            
            if (GetComponentInChildren<Animator>() is { } animator) animator.enabled = true;

            CharacterController characterController = GetComponentInParent<CharacterController>();
            if(characterController == null){ Debug.LogError("XRHandPoser couldn't find CharacterController in parent, which is needed to find other hand posers on player"); return;}
            foreach (var poser in characterController.GetComponentsInChildren<HandInteractorPoser>())
                poser.RefreshPose();
            
            RefreshPose();
        }
        
        // Update is called once per frame
        protected void Start()
        {
            LinkWithInteractors();
            InvokeRepeating(nameof(LinkWithInteractors), 0.3f, 2);
            ApplyDefaultPose();
        }
        
        private void LinkWithInteractors()
        {
            if(interactors.Length == 0) return;
            if(currentAttachTransform != null) return; //Already linked, no need to do it again

            currentAttachTransform = interactors[0].attachTransform;
            foreach (var interactor in interactors)
            {
                interactor.selectEntered.RemoveListener(OnSelectEntered);
                interactor.selectExited.RemoveListener(OnSelectExited);
                
                interactor.selectEntered.AddListener(OnSelectEntered);
                interactor.selectExited.AddListener(OnSelectExited);
            }
            
            //TODO we need this delay, otherwise attachTransform or something doesn't work properly and hand offset breaks
            Invoke(nameof(ApplyDefaultPose), .5f);
        }
    }
}
