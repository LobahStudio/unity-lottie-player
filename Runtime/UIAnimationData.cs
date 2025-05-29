using Gilzoide.LottiePlayer;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "UIAnimationData", menuName = "GameData/UIAnimationData")]
public class UIAnimationData : ScriptableObject
{
    public List<UIAnimationDataSettings> animations;
    public UIAnimationDataSettings GetAnimationDataSettings(UIAnimationType animationType)
    {
        return animations.FirstOrDefault(x => x.animationType == animationType);
    }
}
[Serializable]
public class UIAnimationDataSettings
{
    public UIAnimationType animationType;
    public List<VideoClipSettings> videoClipsSettings;
}
[Serializable]
public class VideoClipSettings
{
    public VideoClip videoClip;
    public LottieAnimationAsset jsonFile;
    public bool isLooping;
    public bool playOnStart;
    public bool autoPlayNext;
    public VideoSize videoSize;
    [Button]
    public VideoSize GetVideoSize()
    {
        videoSize.width = (int) videoClip.width;
        videoSize.height = (int) videoClip.height;
        return videoSize;
    }
}
public enum UIAnimationType
{
    AttackViewJimmy,
    AttackViewTitle,
    SplashTitle,
    SplashJimmy,
    StrikeMode,
    CasualMode,
    AttackTarget,
    LoginJimmy,
    JimmySuperOffer,
    YouWin
}
[Serializable]
public struct VideoSize
{
    public int height;
    public int width;
}