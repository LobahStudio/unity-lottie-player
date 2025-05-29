using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Jobs;
using Unity.Collections;
using Gilzoide.LottiePlayer;
using System;

public class JobLottieRawImagePlayer : MonoBehaviour
{
    [Header("Lottie Settings")]
    public LottieAnimationAsset lottieAsset;
    public float fps = 30f;
    public bool loop = true;
    public RawImage uiTarget;
    public int width = 256;
    public int height = 256;

    private NativeLottieAnimation animation;
    private Texture2D texture;
    private float time;
    public float frameDuration;
    private uint currentFrame;
    private uint lastRenderedFrame = uint.MaxValue;
    private JobHandle renderJobHandle;
    private Coroutine playRoutine;
    [SerializeField] private bool isPlaying = true;
    public Action onFinished;
    private string lastAnimationAssetCacheKey;

    // Start the animation
    public void StartPlay()
    {
        if (lottieAsset == null || uiTarget == null)
        {
            Debug.LogError("Lottie asset and RawImage target must be assigned.");
            enabled = false;
            return;
        }

        animation = lottieAsset.CreateNativeAnimation();
        var size = animation.GetSize();
        width = width <= 0 ? size.x : width;
        height = height <= 0 ? size.y : height;

        texture = animation.CreateTexture(width, height, true);
        uiTarget.texture = texture;
        isPlaying = true;
        playRoutine = StartCoroutine(PlayRoutine());
    }

    // Get total frames of the animation
    public uint GetTotalFrame()
    {
        return animation.GetTotalFrame();
    }

    // Coroutine for playing the animation
/*    IEnumerator PlayRoutine()
    {
        float duration = (float)animation.GetDuration();
        float frameDuration = 1f / fps;
        float accumulatedTime = 0f;

        lastRenderedFrame = uint.MaxValue;
        isPlaying = true;

        while (isPlaying)
        {
            accumulatedTime += Time.deltaTime;

            while (accumulatedTime >= frameDuration)
            {
                time += frameDuration;
                accumulatedTime -= frameDuration;

                if (loop && time >= duration)
                {
                    time = 0f;
                }
                else if (!loop && time >= duration)
                {
                    isPlaying = false;
                    break;
                }

                currentFrame = animation.GetFrameAtTime(time, loop);

                if (currentFrame != lastRenderedFrame)
                {
                    ScheduleRenderJob(currentFrame);
                    CompleteRenderJob();
                }
            }

            yield return null;
        }
        CompleteRenderJob();
        isPlaying = false;
        onFinished?.Invoke();
    }*/
    IEnumerator PlayRoutine()
    {
        float duration = (float)animation.GetDuration();
        frameDuration = 1f / fps;

        lastRenderedFrame = uint.MaxValue;
        isPlaying = true;

        while (isPlaying)
        {
            time += frameDuration;

            if (loop && time >= duration)
            {
                time = 0f;
            }
            else if (!loop && time >= duration)
            {
                break;
            }

            currentFrame = animation.GetFrameAtTime(time, loop);

            if (currentFrame != lastRenderedFrame)
            {
                ScheduleRenderJob(currentFrame);
                CompleteRenderJob();
            }

            yield return new WaitForSeconds(frameDuration);
        }

        CompleteRenderJob();
        isPlaying = false;
        onFinished?.Invoke();
    }

    // Schedule a render job for the current frame
    void ScheduleRenderJob(uint frame)
    {
        renderJobHandle = animation.CreateRenderJob(frame, texture, keepAspectRatio: false).Schedule();
    }

    // Complete the render job
    void CompleteRenderJob()
    {
        renderJobHandle.Complete();
        texture.Apply();
        lastRenderedFrame = currentFrame;
    }

    // Stop the animation
    public void Stop()
    {
        isPlaying = false;
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }
    }

    // Recreate the animation and texture if needed
    public void RecreateAnimationIfNeeded(int renderFrame = -1)
    {
        if (!lottieAsset)
        {
            return;
        }
        else if (lottieAsset.CacheKey != lastAnimationAssetCacheKey)
        {
            animation.Dispose();
            lastAnimationAssetCacheKey = lottieAsset.CacheKey;
        }

        if (!animation.IsValid())
        {
            animation = lottieAsset.CreateNativeAnimation();
        }

        if (uiTarget.texture == null || texture == null || width != texture.width || height != texture.height)
        {
            DestroyImmediate(texture);
            texture = animation.CreateTexture(width, height, true);
            uiTarget.texture = texture;
        }

        if (!Application.isPlaying)
        {
            RenderNow(renderFrame);
        }
    }

    // Render the current frame immediately
    protected void RenderNow(int renderFrame = -1)
    {
        if (renderFrame != -1)
        {
            currentFrame = (uint)renderFrame;
        }
        animation.Render(currentFrame, texture, keepAspectRatio: false);
        texture.Apply(true);
    }

    // Force stop the animation
    public void ForceStop()
    {
        if (playRoutine == null)
        {
            return;
        }
        StopCoroutine(playRoutine);
        CompleteRenderJob();
        playRoutine = null;
    }

    // Play the animation from a specific start time
    public void Play(float startTime = 0.1f)
    {
        Pause();
        time = startTime;
        Unpause();
    }

    // Pause the animation
    [ContextMenu("Pause")]
    public void Pause()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }
    }

    // Unpause the animation
    [ContextMenu("Unpause")]
    public void Unpause()
    {
        if (!isPlaying && animation.IsValid())
        {
            playRoutine = StartCoroutine(PlayRoutine());
        }
    }

    // Clean up when the object is destroyed
    void OnDestroy()
    {
        Stop();
        if (texture != null)
        {
            Destroy(texture);
        }
        animation.Dispose();
    }

#if UNITY_EDITOR
    // Handle the case when the script is being used in the Unity Editor
    protected void OnValidate()
    {
        // Avoid crash
        if (gameObject.activeSelf && lottieAsset != null)
        {
            RecreateAnimationIfNeeded();
        }
    }
#endif
}
