using QuickOutline;
using UnityEngine;

namespace VRFPSKit
{
    public class FirearmHint : MonoBehaviour
    {
        const string PLAYER_PREFS_HIDE_HINTS_KEY = "HintsHidden";
        const float blinkSpeed = 1;
        const float blinkAmount = 2;
        
        [Tooltip("Should the hint immediately be visible if condition is met? ")]
        public bool alwaysShowHints; 
        [Tooltip("This is useful for making very sure the player sees the hint. (Useful in a tutorial etc.)")]
        public bool respectIfPlayerHasDisabledHints = true;
        
        public GameObject outlinedObject;
        
        protected float _hintTime;
        
        protected Firearm _firearm;
        protected FirearmTrigger _trigger;
        protected Outline _outline;

        protected virtual bool HintConditionMet() => true;
        
        protected virtual void TriedToShoot()
        {
            if(HintConditionMet()) _hintTime = Time.time;
        }

        private void RenderOutline()
        {
            float timeSinceHint = Time.time - _hintTime;
            float blinkDuration = (blinkAmount - .25f) * blinkSpeed; // Sin wave will be at -1 when ending
                
            if (HintConditionMet() && (timeSinceHint < blinkDuration || alwaysShowHints))
            
            if (HintConditionMet() && (timeSinceHint < blinkDuration || alwaysShowHints))
            /*if (_firearm.isOwned && HintConditionMet() && 
                (timeSinceHint < blinkDuration || alwaysShowHints) //should we blink?
                && (!GetHidingHints_SettingsValue() || !respectIfPlayerHasDisabledHints))//Are we hiding hints?*/
            {
                _outline.enabled = true;
                
                Color blinkColor = _outline.OutlineColor;
                blinkColor.a = Mathf.InverseLerp(-1, 1, Mathf.Sin(Mathf.PI * 2 * timeSinceHint * blinkSpeed));
                _outline.OutlineColor = blinkColor;
            }
            else
            {
                _outline.enabled = false;
            }
        }

        protected virtual void Update()
        {
            //TODO proxy visualiser
            RenderOutline();
        }

        protected virtual void Awake()
        {
            _firearm = GetComponentInParent<Firearm>();
            _trigger = GetComponentInParent<FirearmTrigger>();

            _trigger.PressEvent += TriedToShoot;
            
            _outline = outlinedObject.AddComponent<Outline>();
            _outline.OutlineMode = Outline.Mode.OutlineAll;
            _outline.OutlineColor = Color.white;
            _outline.OutlineWidth = 12f;
            _outline.enabled = false;
        }
        
        
        public static bool GetHidingHints_SettingsValue() => PlayerPrefs.GetInt(PLAYER_PREFS_HIDE_HINTS_KEY, 0) == 1;
        
        public static void SetHidingHints_SettingsValue(bool hiding)
        {
            PlayerPrefs.SetInt(PLAYER_PREFS_HIDE_HINTS_KEY, hiding ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
