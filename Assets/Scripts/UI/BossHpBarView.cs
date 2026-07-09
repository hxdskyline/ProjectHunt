using UnityEngine;
using UnityEngine.UI;

namespace ProjectHunt.UI
{
    public sealed class BossHpBarView : MonoBehaviour
    {
        public Slider slider;
        public Text label;

        public void SetBossName(string bossName)
        {
            if (label != null)
            {
                label.text = bossName;
            }
        }

        public void SetValue(float current, float max)
        {
            if (slider != null)
            {
                slider.maxValue = max;
                slider.value = current;
            }
        }
    }
}
