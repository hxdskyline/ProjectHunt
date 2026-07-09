using UnityEngine;

namespace ProjectHunt.Battle
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PixelUnitAnimator : MonoBehaviour
    {
        public string resourceId;
        public float fps = 8f;

        private SpriteRenderer _spriteRenderer;
        private PixelAnimationLibrary.ActionClip _currentClip;
        private string _currentAction;
        private bool _loop;
        private float _timer;
        private int _frameIndex;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (_currentClip == null || _currentClip.frames == null || _currentClip.frames.Length == 0)
            {
                return;
            }

            _timer += Time.deltaTime;
            while (_timer >= _currentClip.frameDuration)
            {
                _timer -= _currentClip.frameDuration;
                _frameIndex++;

                if (_frameIndex >= _currentClip.frames.Length)
                {
                    if (_loop)
                    {
                        _frameIndex = 0;
                    }
                    else
                    {
                        _frameIndex = _currentClip.frames.Length - 1;
                    }
                }

                _spriteRenderer.sprite = _currentClip.frames[_frameIndex];
            }
        }

        public void Configure(string newResourceId)
        {
            resourceId = newResourceId;
        }

        public void PlayLoop(string actionName)
        {
            Play(actionName, true);
        }

        public void PlayOnce(string actionName)
        {
            Play(actionName, false);
        }

        public float GetDuration(string actionName)
        {
            var clip = PixelAnimationLibrary.GetClip(resourceId, actionName, fps);
            return clip?.Duration ?? 0f;
        }

        private void Play(string actionName, bool loop)
        {
            if (string.IsNullOrWhiteSpace(resourceId) || string.IsNullOrWhiteSpace(actionName))
            {
                return;
            }

            if (_currentAction == actionName && _loop == loop)
            {
                return;
            }

            var clip = PixelAnimationLibrary.GetClip(resourceId, actionName, fps);
            if (clip == null || clip.frames == null || clip.frames.Length == 0)
            {
                return;
            }

            _currentAction = actionName;
            _currentClip = clip;
            _loop = loop;
            _timer = 0f;
            _frameIndex = 0;
            _spriteRenderer.sprite = _currentClip.frames[0];
        }
    }
}
