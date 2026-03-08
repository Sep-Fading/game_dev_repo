using Demo.Scripts.Runtime.Item;
using KINEMATION.FPSAnimationFramework.Runtime.Playables;
using TMPro;
using UnityEngine;

namespace Demo.Scripts.Runtime
{
    public class UIDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI display;
        private string txt = "";
        void Update()
        {
            int magcount = gameObject.GetComponentInChildren<Weapon>().GetMagazineCount();
            txt = "";
            while (magcount > 0)
            {
                txt += "I";
                magcount--;
            }
            display.text = txt;
        }
    }
}