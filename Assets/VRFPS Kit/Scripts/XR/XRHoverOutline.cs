using QuickOutline;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRFPSKit
{
    [RequireComponent(typeof(XRBaseInteractable))]
    public class XRHoverOutline : MonoBehaviour
    {
        private const float FadeInTime = .5f;
        private const float OutlineAlpha01 = .3f;
        
        public GameObject outlinedObject;
        
        private float _hoverStartTime = -1;
        
        private Outline _outline;
        private XRBaseInteractable _interactable;

        // Update is called once per frame
        private void Update()
        {
            float outlineAlpha01 = 0;
            
            if (HoveredByHand()) 
            {
                //Reset time if just hovered
                if(_hoverStartTime < 0) 
                    _hoverStartTime = Time.time;

                //Fade in outline
                outlineAlpha01 = Mathf.Clamp01(Mathf.InverseLerp(0, FadeInTime, Time.time - _hoverStartTime));
                _outline.enabled = true;
            }
            else
            {
                _hoverStartTime = -1;
                _outline.enabled = false;
            }
            
            //Apply new outline color with alpha
            Color fadeColor = _outline.OutlineColor;
            fadeColor.a = Mathf.SmoothStep(0, OutlineAlpha01, outlineAlpha01);
            _outline.OutlineColor = fadeColor;
        }

        private bool HoveredByHand()
        {
            foreach (var hoverInteractor in _interactable.interactorsHovering)
            {
                //Is hoverInteractor a hand interactor?
                if (hoverInteractor is not XRDirectInteractor handInteractor) continue;
                
                //TODO we want to check if this interactable is the main hovered interactable,
                //TODO unfortunately no way to do that i know of...
                //Is this object the only hovered interactable of the hand interactor?
                if (handInteractor.interactablesHovered.Count != 1) continue;

                return true;
            }

            //Couldn't find any hand interactor hovering over this interactable
            return false;
        }
        
        private void Awake()
        {
            if(outlinedObject == null) outlinedObject = gameObject;
            
            //Add and configure outline component
            _outline = outlinedObject.AddComponent<Outline>();
            _outline.OutlineMode = Outline.Mode.OutlineAll;
            _outline.OutlineColor = Color.white;
            _outline.OutlineWidth = 12f;
            _outline.enabled = false;
            
            _interactable = GetComponent<XRBaseInteractable>();
            _interactable.hoverExited.AddListener(_ => _hoverStartTime = -1);
        }
        
        private void OnDestroy()
        {
            Destroy(_outline);
        }
    }
}
