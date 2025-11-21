using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using System.Collections;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Clip Keys (Addressable)")]
    [SerializeField] private string buttonSoundKey = "/Assets/Mahjong assets/MahjongAssets/Audio/Sound_ButtonPress.mp3";
    [SerializeField] private string wildSoundKey = "/Assets/Mahjong assets/MahjongAssets/Audio/ASN_IconWW.mp3";
    [SerializeField] private string reelStopSoundKey = "/Assets/Mahjong assets/MahjongAssets/Audio/Sound_SpinStop.mp3";
    [SerializeField] private string freeSpinActivationSoundKey = "/Assets/Mahjong assets/MahjongAssets/Audio/FSN_FreeSpinsResult.mp3";
    [SerializeField] private string freeSpinBGMusicKey = "/Assets/Mahjong assets/MahjongAssets/Audio/FSN_Background.mp3";
    [SerializeField] private string freeSpinResultSoundKey = "/Assets/Mahjong assets/MahjongAssets/Audio/FSN_FreeSpinsResult.mp3";
    [SerializeField] private string freeSpinScoringSoundKey = "";
    [SerializeField] private string reel3FreeSpinSoundKey = "/Assets/Mahjong assets/MahjongAssets/Audio/ASN_Hit_01(Reel 3 free spin).mp3";
    [SerializeField] private string reel4FreeSpinSoundKey = "/Assets/Mahjong assets/MahjongAssets/Audio/ASN_Hit_02(Reel 4 freespin).mp3";
    [SerializeField] private string reel5FreeSpinCelebrationSoundKey = "/Assets/Mahjong assets/MahjongAssets/Audio/ASN_Hit_03(Reel 5 freespin).mp3";
    [SerializeField] private string fivematchSoundKey = "/Assets/winning mask 1 mp3/ASN_Hit_05.mp3";

    [SerializeField] private AudioSource bigWinLoopSource;
    [SerializeField] private AudioSource smallWinLoopSource;
    [SerializeField] private AudioSource smallWinLoopSourceEnd;
    [SerializeField] private AudioSource freeSpinBGMusicSource;
    [SerializeField] private AudioSource freeSpinWinsSource;
    [SerializeField] private AudioSource tensionSource;

    // Cache for loaded audio clips
    private Dictionary<string, AudioClip> loadedClips = new Dictionary<string, AudioClip>();
    private Dictionary<string, AsyncOperationHandle<AudioClip>> loadingHandles = new Dictionary<string, AsyncOperationHandle<AudioClip>>();

    // Dictionary to track which AudioSource is playing which sound
    private Dictionary<string, AudioSource> activeAudioSources = new Dictionary<string, AudioSource>();
    private List<AudioSource> audioSources = new List<AudioSource>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // Pre-load essential sounds (like button clicks)
            PreloadEssentialSounds();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void PreloadEssentialSounds()
    {
        // Load immediately needed sounds
        LoadAudioClip(buttonSoundKey, "button");
        LoadAudioClip(wildSoundKey, "wild");
        LoadAudioClip(reelStopSoundKey, "reelstop");
    }

    public void PlaySound(string element)
    {
        if (element == "finalscore")
            return;
        
        string key = GetClipKey(element);
        if (string.IsNullOrEmpty(key)) return;

        // Check if already loaded
        if (loadedClips.TryGetValue(key, out AudioClip clip))
        {
            PlayClipImmediate(element, clip);
        }
        else
        {
            // Load and play
            LoadAudioClip(key, element, (loadedClip) =>
            {
                if (loadedClip != null)
                {
                    PlayClipImmediate(element, loadedClip);
                }
            });
        }
    }

    public void PlaySoundOneShot(string element)
    {
        string key = GetClipKey(element);
        if (string.IsNullOrEmpty(key)) return;

        if (loadedClips.TryGetValue(key, out AudioClip clip))
        {
            AudioSource source = GetAvailableAudioSource();
            source.PlayOneShot(clip);
        }
        else
        {
            LoadAudioClip(key, element, (loadedClip) =>
            {
                if (loadedClip != null)
                {
                    AudioSource source = GetAvailableAudioSource();
                    source.PlayOneShot(loadedClip);
                }
            });
        }
    }

    private void PlayClipImmediate(string element, AudioClip clip)
    {
        // Stop existing sound for this element before playing again
        if (activeAudioSources.TryGetValue(element, out AudioSource existingSource))
        {
            if (existingSource.isPlaying)
                existingSource.Stop();
        }

        AudioSource source = GetAvailableAudioSource();
        source.clip = clip;
        source.Play();

        // Store the source after it's playing
        activeAudioSources[element] = source;
    }

    private void LoadAudioClip(string key, string element, System.Action<AudioClip> onComplete = null)
    {
        // Check if already loading
        if (loadingHandles.ContainsKey(key))
        {
            return;
        }

        var handle = Addressables.LoadAssetAsync<AudioClip>(key);
        loadingHandles[key] = handle;

        handle.Completed += (asyncHandle) =>
        {
            loadingHandles.Remove(key);

            if (asyncHandle.Status == AsyncOperationStatus.Succeeded)
            {
                loadedClips[key] = asyncHandle.Result;
                onComplete?.Invoke(asyncHandle.Result);
            }
            else
            {
                onComplete?.Invoke(null);
            }
        };
    }

    public void StopSound(string element)
    {
        if (activeAudioSources.TryGetValue(element, out AudioSource source))
        {
            source.Stop();
        }
    }

    public void StopAllSounds()
    {
        foreach (var source in audioSources)
        {
            source.Stop();
        }

        // Clear the active sources dictionary
        activeAudioSources.Clear();
    }

    public float GetSoundDuration(string element)
    {
        string key = GetClipKey(element);
        if (loadedClips.TryGetValue(key, out AudioClip clip))
        {
            return clip.length;
        }
        return 0f;
    }

    public bool IsSoundPlaying(string element)
    {
        if (activeAudioSources.TryGetValue(element, out AudioSource source))
        {
            return source.isPlaying;
        }
        return false;
    }

    /// <summary>
    /// Plays a cycling spin sound based on the spin number (1-10)
    /// </summary>
    /// <param name="spinNumber">The spin number (1-10), will be clamped to this range</param>
    public void PlayCyclingSpinSound(int spinNumber)
    {
        // Clamp spin number to 1-10 range
        spinNumber = Mathf.Clamp(spinNumber, 1, 10);

        string soundKey = $"spinsound{spinNumber}";
        PlaySound(soundKey);

        // Debug.Log($"[SoundManager] Playing cycling spin sound {spinNumber}: {soundKey}");
    }

    // ==========================================
    // NEW: MASK REEL SPECIFIC SOUND METHODS
    // ==========================================

    public void PlayPinkHighlightMusic()
    {
        
    }

    public void StopPinkHighlightMusic()
    {
        
    }

    public void PlayMaskReelBGMusic()
    {
        
    }

    public void StopMaskReelBGMusic()
    {
        
    }

    public void PlayMaskSpinning()
    {
        
    }

    public void StopMaskSpinning()
    {
        
    }

    public void PlayBigWinLoop()
    {
        if (bigWinLoopSource != null && !bigWinLoopSource.isPlaying)
        {
            bigWinLoopSource.Play();
        }
    }
    public void StopBigWinLoop()
    {
        if (bigWinLoopSource != null && bigWinLoopSource.isPlaying)
        {
            bigWinLoopSource.Stop();
        }
    }
    public void PlaySmallWinLoop()
    {
        if (smallWinLoopSource != null && !smallWinLoopSource.isPlaying)
        {
            StartCoroutine(PlaySmallWinMerged());
        }
    }

    IEnumerator PlaySmallWinMerged()
    {
        smallWinLoopSource.Play();
        yield return new WaitForSeconds(1.254f);
        PlaySmallWinLoopEnd();
    }
    
    public void PlaySmallWinLoopEnd()
    {
        if (smallWinLoopSourceEnd != null && !smallWinLoopSourceEnd.isPlaying)
        {
            smallWinLoopSourceEnd.Play();
        }
    }

    public void StopSmallWinLoop()
    {
        if (smallWinLoopSource != null && smallWinLoopSource.isPlaying)
        {
            smallWinLoopSource.Stop();
        }
    }

    public void PlayFreeSpinMusicLoop()
    {
        if (freeSpinBGMusicSource != null && !freeSpinBGMusicSource.isPlaying)
        {
            freeSpinBGMusicSource.Play();
        }
    }

    public void StopPlayFreeSpinMusicLoop()
    {
        if (freeSpinBGMusicSource != null && freeSpinBGMusicSource.isPlaying)
        {
            freeSpinBGMusicSource.Stop();
        }
    }

    public void PlayFreeWinsLoop()
    {
        if (freeSpinWinsSource != null && !freeSpinWinsSource.isPlaying)
        {
            // Debug.Log("freesinwin sound");
            // freeSpinWinsSource.Play();
        }
    }

    public void StopPlayFreeWinsLoop()
    {
        if (freeSpinWinsSource != null && freeSpinWinsSource.isPlaying)
        {
            freeSpinWinsSource.Stop();
        }
    }

    public void PlayTensionLoop()
    {
        if (tensionSource != null && !tensionSource.isPlaying)
        {
            tensionSource.Play();
        }
    }

    public void StopTensionLoop()
    {
        if (tensionSource != null && tensionSource.isPlaying)
        {
            tensionSource.Stop();
        }
    }

    private string GetClipKey(string element)
    {
        switch (element.ToLower())
        {
            case "button": return buttonSoundKey;
            case "wild": return wildSoundKey;
            case "reelstop": return reelStopSoundKey;
            case "freespin": return freeSpinActivationSoundKey;
            case "freespinmusic": return freeSpinBGMusicKey;
            case "freespinresult": return freeSpinResultSoundKey;
            case "freespinscoring": return freeSpinScoringSoundKey;
            case "freespin1": return reel3FreeSpinSoundKey;
            case "freespin2": return reel4FreeSpinSoundKey;
            case "freespin3": return reel5FreeSpinCelebrationSoundKey;
            case "fivematch": return fivematchSoundKey;

            default:
                // Debug.LogWarning("Unknown element sound: " + element);
                return null;
        }
    }

    public void SetMute(bool mute)
    {
        // Mute all managed sources
        foreach (var source in audioSources)
        {
            source.mute = mute;
        }

        // Mute special sources if assigned
        if (bigWinLoopSource != null) bigWinLoopSource.mute = mute;
        if (smallWinLoopSource != null) smallWinLoopSource.mute = mute;
        if (freeSpinBGMusicSource != null) freeSpinBGMusicSource.mute = mute;
        if (freeSpinWinsSource != null) freeSpinWinsSource.mute = mute;
        if (tensionSource != null) tensionSource.mute = mute;

        // Update all AudioSources on the object, including dynamically added ones
        AudioSource[] allSources = GetComponents<AudioSource>();
        foreach (AudioSource source in allSources)
        {
            source.mute = mute;
        }

        // Log the mute state to verify it's working
        // Debug.Log("Sound muted: " + mute);
    }

    private AudioSource GetAvailableAudioSource()
    {
        foreach (AudioSource source in audioSources)
        {
            if (!source.isPlaying)
                return source;
        }

        // If no available, create a new one
        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        audioSources.Add(newSource);
        return newSource;
    }

    private void OnDestroy()
    {
        // Release all loaded audio clips
        foreach (var handle in loadingHandles.Values)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
        loadingHandles.Clear();
        loadedClips.Clear();
    }
}