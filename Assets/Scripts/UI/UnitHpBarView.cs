using UnityEngine;

namespace ProjectHunt.UI
{
    public sealed class UnitHpBarView : MonoBehaviour
    {
        public SpriteRenderer backgroundRenderer;
        public SpriteRenderer fillRenderer;
        public Vector3 worldOffset = new Vector3(0f, 1.35f, 0f);
        public float backgroundWidth = 1.6f;
        public float backgroundHeight = 0.16f;
        public float fillWidth = 1.44f;
        public float fillHeight = 0.10f;

        private Transform _target;
        private float _maxHp = 1f;

        public void Bind(Transform target, float maxHp)
        {
            _target = target;
            _maxHp = Mathf.Max(1f, maxHp);
            ApplyVisualSizes();
            SetValue(maxHp);
            LateUpdate();
        }

        public void SetValue(float currentHp)
        {
            if (fillRenderer == null)
            {
                return;
            }

            var normalized = Mathf.Clamp01(currentHp / _maxHp);
            fillRenderer.transform.localScale = new Vector3(fillWidth * normalized, fillHeight, 1f);
            fillRenderer.transform.localPosition = new Vector3(-(fillWidth - (fillWidth * normalized)) * 0.5f, 0f, 0f);
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            transform.position = _target.position + worldOffset;
        }

        private void OnValidate()
        {
            ApplyVisualSizes();
        }

        private void ApplyVisualSizes()
        {
            if (backgroundRenderer != null)
            {
                backgroundRenderer.transform.localScale = new Vector3(backgroundWidth, backgroundHeight, 1f);
            }

            if (fillRenderer != null)
            {
                fillRenderer.transform.localScale = new Vector3(fillWidth, fillHeight, 1f);
            }
        }
    }
}
