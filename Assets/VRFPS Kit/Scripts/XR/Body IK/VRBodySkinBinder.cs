using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRFPSKit
{
public class VRBodySkinBinder : MonoBehaviour
{
    public bool hideHeadOfLocalPlayer = true;
    public Transform headTarget;
    public Transform leftHandControllerTarget;
    public Transform rightHandControllerTarget;
    [Space] 
    public XRBaseInteractor[] leftHandInteractors;
    public XRBaseInteractor[] rightHandInteractors;

    private VRBodySkin _skin;

    // Update is called once per frame
    void Update()
    {
        UpdateSkinBinding();
    }

    private void UpdateSkinBinding()
    {
        _skin = GetComponentInChildren<VRBodySkin>();
        if (_skin == null)
        {
            Debug.LogWarning("VRBodySkinBinder: No VRBodySkin found in children. Your player doesn't have a body ):");
            return;
        }

        _skin.leftHand.vrTarget = leftHandControllerTarget;
        _skin.rightHand.vrTarget = rightHandControllerTarget;
        _skin.head.vrTarget = headTarget;

        _skin.leftHandInteractors = leftHandInteractors;
        _skin.rightHandInteractors = rightHandInteractors;
        
        _skin.SetHideHead(hideHeadOfLocalPlayer);
    }

    public VRBodySkin GetSkin() => _skin;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        UpdateSkinBinding();
    }
}
}
