using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.Video;

public class VideoPlayerHandler : MonoBehaviour, IPoolObject
{
    public VideoPlayer videoPlayer;
    public RenderTexture baseRendererTexture;
    private Action _onVideoFinished;
    private int currentVideoIndex;
    private UIAnimationDataSettings currentSettings;
    public bool IsActivated { get; set; }
    private void Awake()
    {
        videoPlayer.loopPointReached += OnVideoFinished;
    }
    public UIAnimationType GetAnimationType() 
    {
        return currentSettings.animationType;
    }
    public void OnVideoFinished(VideoPlayer source)
    {
        if (currentSettings.videoClipsSettings[currentVideoIndex].isLooping)
        {
            return;
        }
        if (currentVideoIndex + 1 >= currentSettings.videoClipsSettings.Count)
        {
            _onVideoFinished?.Invoke();
            return;
        }
        if (currentSettings.videoClipsSettings[currentVideoIndex].autoPlayNext)
        {
            currentVideoIndex++;
            StartPlayVideo(currentSettings.videoClipsSettings[currentVideoIndex]);
        }
    }
    public void StartPlayVideo(VideoClipSettings videoClipSettings)
    {
        videoPlayer.clip = videoClipSettings.videoClip;
        videoPlayer.Prepare();
        videoPlayer.isLooping = videoClipSettings.isLooping;
        videoPlayer.Play();
    }
    public void Activate()
    {
        IsActivated = true;
        gameObject.SetActive(true);
    }
    public IPoolObject Create(Transform parent)
    {
        var newVideoPlayerGameOject = Instantiate(gameObject);
        var newVideoPlayer = newVideoPlayerGameOject.GetComponent<VideoPlayerHandler>();
        newVideoPlayer.transform.SetParent(parent);
        return newVideoPlayer;
    }
    public void Release()
    {
        IsActivated = false;
        gameObject.SetActive(false);
        _onVideoFinished = null;
        videoPlayer.targetTexture?.Release();
        Destroy(videoPlayer.targetTexture);
        videoPlayer.targetTexture = null;
    }

    [Button]
    public void TestSize()
    {
        Debug.Log("Size > " + videoPlayer.clip.height + "   >   " + videoPlayer.clip.width);
    }
    public RenderTexture CreateTexture(VideoSize videoSize) 
    {
        RenderTexture renderTexture = new RenderTexture(videoSize.width, videoSize.height, 0, RenderTextureFormat.ARGB32);
        renderTexture.useMipMap = false;
        renderTexture.autoGenerateMips = false;
        renderTexture.Create();
        renderTexture.colorBuffer.GetNativeRenderBufferPtr(); // Force allocation
        return renderTexture;
    }    
    internal RenderTexture StartPlayVideo(UIAnimationDataSettings animationDataSettings)
    {
        currentVideoIndex = 0;
        currentSettings = animationDataSettings;
        var videoClipSettings = currentSettings.videoClipsSettings[currentVideoIndex];
        if (videoClipSettings.videoSize.width == 0)
        {
            videoClipSettings.GetVideoSize();
        }
        var newTexture = CreateTexture(videoClipSettings.videoSize);
        videoPlayer.targetTexture = newTexture;
        StartPlayVideo(currentSettings.videoClipsSettings[currentVideoIndex]);
        return newTexture;
    }
    internal void PlayNext()
    {
        if (currentVideoIndex + 1 >= currentSettings.videoClipsSettings.Count)
        {
            Debug.LogError("No More CLips");
            return;
        }
        currentVideoIndex++;
        var videoClipSettings = currentSettings.videoClipsSettings[currentVideoIndex];
        StartPlayVideo(currentSettings.videoClipsSettings[currentVideoIndex]);
    }
    public void RegisterOnVideoFinished(Action onFinished) 
    {
        _onVideoFinished += onFinished;
    }

    internal void Pause()
    {
        videoPlayer.Pause();
    }
}
