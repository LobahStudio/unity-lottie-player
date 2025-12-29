using System;
using System.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UI;

namespace Gilzoide.LottiePlayer
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class ImageLottiePlayer : MaskableGraphic
    {
        [Header("Animation Options")]
        [SerializeField] public LottieAnimationAsset _animationAsset;
        [SerializeField] public AutoPlayEvent _autoPlay = AutoPlayEvent.OnStart;
        [SerializeField] public bool _loop = true;

        [Header("Texture Options")]
        [SerializeField, Min(2)] public int _width = 128;
        [SerializeField, Min(2)] public int _height = 128;
        [SerializeField] protected bool _keepAspect = true;

        protected Texture2D _texture;
        protected NativeLottieAnimation _animation;
        protected float _time = 0;
        protected uint _currentFrame = 0;
        protected uint _lastRenderedFrame = 0;
        protected JobHandle _renderJobHandle;
        protected Coroutine _playCoroutine;
        private string _lastAnimationAssetCacheKey;

        public override Texture mainTexture => _texture;

        public bool IsPlaying => _playCoroutine != null;
        public Action onFinished;
        bool _isAlive = true;
        protected override void OnEnable()
        {
            base.OnEnable();
            RecreateAnimationIfNeeded();
            if (_autoPlay == AutoPlayEvent.OnEnable && Application.isPlaying)
            {
                Play();
            }
        }

        protected override void Start()
        {
            base.Start();
            if (_autoPlay == AutoPlayEvent.OnStart && Application.isPlaying)
            {
                Play();
            }
        }

        protected override void OnDisable()
        {
            Pause();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            DiscardRenderJob();
            DestroyImmediate(_texture);
            _animation.Dispose();
            base.OnDestroy();
        }
        public void Shutdown()
        {
            if (!_isAlive)
                return;

            _isAlive = false;

            if (_playCoroutine != null)
            {
                StopCoroutine(_playCoroutine);
                _playCoroutine = null;
            }
            try
            {
                if (_renderJobHandle.IsCompleted == false)
                {
                    _renderJobHandle.Complete();
                }
            }
            catch
            {
            }
            _lastRenderedFrame = _currentFrame;

            onFinished = null;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (!_animation.IsValid())
            {
                return;
            }

            Rect pixelAdjustedRect = GetPixelAdjustedRect();
            if (_keepAspect)
            {
                pixelAdjustedRect = pixelAdjustedRect.AspectFit(_animation.GetSize().GetAspect());
            }
            Color32 color = this.color;
            vh.AddVert(new Vector3(pixelAdjustedRect.xMin, pixelAdjustedRect.yMin), color, new Vector2(0f, 1f));
            vh.AddVert(new Vector3(pixelAdjustedRect.xMin, pixelAdjustedRect.yMax), color, new Vector2(0f, 0f));
            vh.AddVert(new Vector3(pixelAdjustedRect.xMax, pixelAdjustedRect.yMax), color, new Vector2(1f, 0f));
            vh.AddVert(new Vector3(pixelAdjustedRect.xMax, pixelAdjustedRect.yMin), color, new Vector2(1f, 1f));
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        public void SetAnimationAsset(LottieAnimationAsset animationAsset)
        {
            if (_animationAsset == animationAsset)
            {
                return;
            }
            Pause();
            _animationAsset = animationAsset;
            RecreateAnimationIfNeeded();
        }

        public void SetAnimation(NativeLottieAnimation animation)
        {
            if (_animation == animation)
            {
                return;
            }
            Pause();
            _animationAsset = null;
            RecreateAnimationIfNeeded(animation);
        }

        [ContextMenu("Play")]
        public void Play(float startTime = 0.1f)
        {
            Pause();
            _time = startTime;
            Unpause();
        }
        [ContextMenu("Play In Reverse")]
        public void PlayInReverse()
        {
            Pause();
            _time = (float)_animation.GetDuration();
            _playCoroutine = StartCoroutine(PlayReverseRoutine());
        }
        private IEnumerator PlayReverseRoutine()
        {
            _lastRenderedFrame = uint.MaxValue;
            float duration = (float)_animation.GetDuration();
            while (_loop || _time > 0)
            {
                _currentFrame = _animation.GetFrameAtTime(_time, _loop);//TODO: didnt this function with test it with _loop true values yet
                if (_currentFrame != _lastRenderedFrame)
                {
                    ScheduleRenderJob(_currentFrame);
                }
                yield return null;
                _time -= Time.deltaTime * 2;   ///this just to speed up the animation lma ykon in reverse otherwise hata5od L framerate L tabe3y
                if (_currentFrame != _lastRenderedFrame)
                {
                    CompleteRenderJob();
                }
            }
            CompleteRenderJob();
            _playCoroutine = null;
            onFinished?.Invoke();
        }
        [ContextMenu("Pause")]
        public void Pause()
        {
            if (_playCoroutine != null)
            {
                StopCoroutine(_playCoroutine);
                _playCoroutine = null;
            }
        }

        [ContextMenu("Unpause")]
        public void Unpause()
        {
            if (!IsPlaying && _animation.IsValid())
            {
                _playCoroutine = StartCoroutine(PlayRoutine());
            }
        }

        protected IEnumerator PlayRoutine()
        {
            // force render first frame
            _lastRenderedFrame = uint.MaxValue;

            float duration = (float)_animation.GetDuration();
            while (_loop || _time < duration)
            {
                _currentFrame = _animation.GetFrameAtTime(_time, _loop);
                if (_currentFrame != _lastRenderedFrame)
                {
                    ScheduleRenderJob(_currentFrame);
                }
                yield return null;
                _time += Time.deltaTime;
                if (_currentFrame != _lastRenderedFrame)
                {
                    CompleteRenderJob();
                }
            }
            CompleteRenderJob();
            _playCoroutine = null;
            onFinished?.Invoke();
        }

        public void RecreateAnimationIfNeeded(int renderFrame = -1)
        {
            if (_animationAsset != null && _animationAsset.CacheKey == _lastAnimationAssetCacheKey)
            {
                return;
            }

            if (_animationAsset != null)
            {
                _lastAnimationAssetCacheKey = _animationAsset.CacheKey;
                RecreateAnimationIfNeeded(_animationAsset.CreateNativeAnimation());
            }
            else
            {
                _lastAnimationAssetCacheKey = null;
                RecreateAnimationIfNeeded(NativeLottieAnimation.Invalid);
            }
        }

        protected void RecreateAnimationIfNeeded(NativeLottieAnimation newAnimation)
        {
            if (_animation.IsValid())
            {
                DiscardRenderJob();
                _animation.Dispose();
                SetVerticesDirty();
            }

            if (!newAnimation.IsValid())
            {
                return;
            }

            _animation = newAnimation;
            if (_texture == null
                || _width != _texture.width
                || _height != _texture.height)
            {
                DestroyImmediate(_texture);
                _texture = _animation.CreateTexture(_width, _height, false);
            }

            if (!Application.isPlaying)
            {
                RenderNow(renderFrame);
            }
        }
        public void ForceStop()
        {
            if (_playCoroutine == null)
            {
                return;
            }
            StopCoroutine(_playCoroutine);
            CompleteRenderJob();
            _playCoroutine = null;
        }
        public uint GetTotalFrame()
        {
            return _animation.GetTotalFrame();
        }
        protected void RenderNow(int renderFrame = -1)
        {
            if (renderFrame != -1)
            {
                _currentFrame = (uint)renderFrame;
            }
            _animation.Render(_currentFrame, _texture, keepAspectRatio: false);
            _texture.Apply(true);
        }

        protected void ScheduleRenderJob(uint frame)
        {
            _renderJobHandle = _animation.CreateRenderJob(frame, _texture, keepAspectRatio: false).Schedule();
        }

        protected void CompleteRenderJob()
        {
            _lastRenderedFrame = _currentFrame;
            _renderJobHandle.Complete();
            _texture.Apply(true);
        }

        protected void DiscardRenderJob()
        {
            _renderJobHandle.Complete();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            // Avoid crash
            if (IsActive() && !_animationAsset)
            {
                RecreateAnimationIfNeeded();
            }
        }
#endif

    }
}
