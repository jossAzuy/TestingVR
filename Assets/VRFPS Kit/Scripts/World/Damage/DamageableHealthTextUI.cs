using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VRFPSKit
{
    public class DamageableHealthTextUI : MonoBehaviour
    {
        public TMP_Text text;
        private Damageable _damageable;

        // Update is called once per frame
        void Update()
        {
            int health = (int)_damageable.health;
            
            if(health > 0)
                text.text = $"HP: {health}\u2665";
            else 
                text.text = "Dead";
        }

        private void Awake()
        {
            _damageable = GetComponentInParent<Damageable>();
        }
    }
}