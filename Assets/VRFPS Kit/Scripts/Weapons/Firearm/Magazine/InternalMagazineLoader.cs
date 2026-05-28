using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRFPSKit
{
    /// <summary>
    /// Loads cartridge items that enter trigger in to magazine
    /// </summary>
    [RequireComponent(typeof(FirearmCyclingAction))]
    public class InternalMagazineLoader : MagazineLoader
    {
        private const float MinimumTimeSinceEjection = .5f;
        
        [Space]
        public bool requireActionBack = true;
        public float actionBackLoadingThreshold01 = .9f;
        
        private FirearmCyclingAction _cyclingAction;
        private float _lastEjectTime;
        
        protected override void TryLoadCartridges()
        {
            if(requireActionBack && _cyclingAction.GetActionPosition01() < actionBackLoadingThreshold01) return;
            if (Time.time - _lastEjectTime < MinimumTimeSinceEjection) return;
            
            base.TryLoadCartridges();
        }
        
        // Start is called before the first frame update
        protected override void Awake()
        {
            base.Awake();
            _cyclingAction = GetComponent<FirearmCyclingAction>();
        }

        protected void Start()
        {
            if(_cyclingAction.Ejector)
                _cyclingAction.Ejector.EjectEvent += _ => _lastEjectTime = Time.time;
        }
    }
}