using Gilzoide.LottiePlayer;
using Sirenix.OdinInspector;
using System;
using System.Diagnostics;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;

public class UIJsonAnimationPlayer : MonoBehaviour
{
    public UIAnimationType animationType;
    public ImageLottiePlayer lottiePlayer;
    public JobLottieRawImagePlayer jobLottieRawImage;
    public UIAnimationData uIAnimationData;
    public UnityEvent onVideoFinished;
    UIAnimationDataSettings currentSettings;
    Action _onVideoFinished;
    int currentVideoIndex;
    public void Start()
    {
        currentVideoIndex = 0;
        lottiePlayer.onFinished += OnAnimationStepFinished;
        currentSettings = uIAnimationData.GetAnimationDataSettings(animationType);
        if (currentSettings.videoClipsSettings[currentVideoIndex].playOnStart)
        {
            StartPlayVideo();
        }
    }
    [Button]
    public int AdjustSize(int frameIndex = -1)
    {
        var setting = uIAnimationData.GetAnimationDataSettings(animationType);
        lottiePlayer._animationAsset = setting.videoClipsSettings[0].jsonFile;
        lottiePlayer._width = setting.videoClipsSettings[0].jsonFile.Size.x;
        lottiePlayer._height = setting.videoClipsSettings[0].jsonFile.Size.y;
        lottiePlayer.GetComponent<RectTransform>().sizeDelta = new Vector2(lottiePlayer._width, lottiePlayer._height);
        lottiePlayer.RecreateAnimationIfNeeded(frameIndex == -1 ? Mathf.RoundToInt(lottiePlayer.GetTotalFrame() / 2f) : frameIndex);
        lottiePlayer.gameObject.SetActive(false);
        lottiePlayer.gameObject.SetActive(true);
#if UNITY_EDITOR
        EditorUtility.SetDirty(lottiePlayer);
#endif
        return Mathf.RoundToInt(lottiePlayer.GetTotalFrame() / 2f);
    }
    public void OnAnimationStepFinished()
    {
        if (currentSettings.videoClipsSettings[currentVideoIndex].isLooping)
        {
            return;
        }
        if (currentVideoIndex + 1 >= currentSettings.videoClipsSettings.Count)
        {
            _onVideoFinished?.Invoke();
            onVideoFinished?.Invoke();
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
        lottiePlayer.color = new Color(1, 1, 1, 1);
        lottiePlayer.ForceStop();
        lottiePlayer._animationAsset = videoClipSettings.jsonFile;
        lottiePlayer._loop = videoClipSettings.isLooping;
        //lottiePlayer._autoPlay = AutoPlayEvent.OnEnable;
        StartPlay();
    }
    private void StartPlay()
    {
        lottiePlayer.enabled = false;
        lottiePlayer.enabled = true;
        lottiePlayer.Play(0.2f);
    }
    public IPoolObject Create(Transform parent)
    {
        var newVideoPlayerGameOject = Instantiate(gameObject);
        var newVideoPlayer = newVideoPlayerGameOject.GetComponent<VideoPlayerHandler>();
        newVideoPlayer.transform.SetParent(parent);
        return newVideoPlayer;
    }
    internal void StartPlayVideo()
    {
        currentVideoIndex = 0;
        if (currentSettings == null)
        {
            currentSettings = uIAnimationData.GetAnimationDataSettings(animationType);
        }
        StartPlayVideo(currentSettings.videoClipsSettings[currentVideoIndex]);
    }
    [Button]
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
    internal void RegisterWhenFinished(Action onAnimationFinished)
    {
        _onVideoFinished += onAnimationFinished;
    }

    internal void Hide(bool v)
    {
        lottiePlayer.color = new Color(1, 1, 1, 0);
    }

    internal void StopPlaying()
    {
        lottiePlayer.ForceStop();
    }
}
