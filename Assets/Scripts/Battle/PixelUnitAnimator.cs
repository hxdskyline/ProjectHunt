using UnityEngine;

namespace ProjectHunt.Battle
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PixelUnitAnimator : MonoBehaviour
    {
        public string resourceId;
        public float fps = 8f;

        private SpriteRenderer _spriteRenderer;
        private SpriteRenderer _rootRenderer;
        private Transform _lichVisualTransform;
        private PixelAnimationLibrary.ActionClip _currentClip;
        private string _currentAction;
        private bool _loop;
        private float _timer;
        private int _frameIndex;

        private void Awake()
        {
            _rootRenderer = GetComponent<SpriteRenderer>();
            _spriteRenderer = _rootRenderer;
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

                SetSprite(_currentClip.frames[_frameIndex]);
            }
        }

        public void Configure(string newResourceId)
        {
            resourceId = newResourceId;
            if (resourceId == "boss_lich")
            {
                EnsureLichVisualRenderer();
            }
        }

        public void PlayLoop(string actionName)
        {
            Play(actionName, true);
        }

        public void PlayOnce(string actionName)
        {
            Play(actionName, false);
        }

        public void Stop()
        {
            _currentClip = null;
            _currentAction = null;
            _loop = false;
            _timer = 0f;
            _frameIndex = 0;
        }

        public float GetDuration(string actionName)
        {
            var clip = PixelAnimationLibrary.GetClip(resourceId, actionName, fps);
            return clip?.Duration ?? 0f;
        }

        public int GetFrameCount(string actionName)
        {
            return PixelAnimationLibrary.GetFrameCount(resourceId, actionName, fps);
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
            SetSprite(_currentClip.frames[0]);
        }

        private void EnsureLichVisualRenderer()
        {
            if (_lichVisualTransform == null)
            {
                var visual = new GameObject("LichVisual", typeof(SpriteRenderer));
                visual.transform.SetParent(transform, false);
                _lichVisualTransform = visual.transform;
                _spriteRenderer = visual.GetComponent<SpriteRenderer>();
                _spriteRenderer.sortingOrder = _rootRenderer != null ? _rootRenderer.sortingOrder : 5;
                _spriteRenderer.flipX = true;
                if (_rootRenderer != null)
                {
                    _rootRenderer.enabled = false;
                }
            }
        }

        private void SetSprite(Sprite sprite)
        {
            if (_spriteRenderer == null)
            {
                return;
            }

            _spriteRenderer.sprite = sprite;
            if (resourceId == "boss_lich" && _lichVisualTransform != null && sprite != null)
            {
                // Lich walk is the only action authored on the shifted 138x138 canvas.
                // Attack/cast/death always use the normal offset, including image_12774 and image_13886.
                var usesWalkOffset = _currentAction == "walk" &&
                                     sprite.texture.width == 138 &&
                                     sprite.texture.height == 138;
                _lichVisualTransform.localPosition = usesWalkOffset
                    ? new Vector3(0.73f, -2.56f, 0f)
                    : new Vector3(-1.38f, -0.62f, 0f);
            }
        }
    }
}
