using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public sealed class AudioManager : MonoBehaviour, IAudioService, IAudioMuteQuery
{
    [Header("Pool")]
    [SerializeField] private int initialVoices = 16;
    [SerializeField] private int maxVoices = 32;

    [Header("Voice stealing")]
    [SerializeField] private bool stealOldestWhenFull = true;

    [Header("Mixer (optional)")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string masterVolumeParam = "MasterVol";
    [SerializeField] private string musicVolumeParam = "MusicVol";
    [SerializeField] private string sfxVolumeParam = "SfxVol";

    [Header("Music")]
    [SerializeField] private MusicPlayer musicPlayer;

    private readonly Queue<AudioSource> free = new();
    private readonly LinkedList<AudioSource> busyOrder = new();
    private readonly Dictionary<AudioSource, LinkedListNode<AudioSource>> busyNode = new();
    private readonly Dictionary<AudioSource, SoundRef> voiceSound = new();

    private readonly Dictionary<SoundRef, int> playingCount = new();
    private readonly Dictionary<SoundRef, float> lastPlayTime = new();

    private bool unlockedOnce;

    private bool mutedAll;
    private bool mutedMusic;
    private bool mutedSfx;

    private float masterVol01 = 1f;
    private float musicVol01 = 1f;
    private float sfxVol01 = 1f;

    private void Awake()
    {
        WarmPool(initialVoices);

        if (musicPlayer == null)
        {
            var go = new GameObject("MusicPlayer");
            go.transform.SetParent(transform);
            musicPlayer = go.AddComponent<MusicPlayer>();
        }

        musicPlayer.Bind(this);
    }

    public AudioHandle Play(SoundRef s) => PlayInternal(s, default);
    public AudioHandle Play(SoundRef s, PlayOptions opt) => PlayInternal(s, opt);

    public void Stop(AudioHandle h)
    {
        if (!h.IsValid) return;
        ReleaseVoice(h.Source, h.Sound);
    }

    public void SetMuted(AudioBus bus, bool muted)
    {
        switch (bus)
        {
            case AudioBus.All:
                mutedAll = muted;
                if (mutedAll)
                {
                    StopAllByBus(AudioBus.All);
                    musicPlayer?.Pause();
                }
                ApplyMixerVolume(masterVolumeParam, mutedAll ? 0f : masterVol01);
                break;

            case AudioBus.Music:
                mutedMusic = muted;
                if (mutedMusic) musicPlayer?.Pause();
                ApplyMixerVolume(musicVolumeParam, mutedMusic ? 0f : musicVol01);
                break;

            case AudioBus.Sound:
                mutedSfx = muted;
                if (mutedSfx) StopAllByBus(AudioBus.Sound);
                ApplyMixerVolume(sfxVolumeParam, mutedSfx ? 0f : sfxVol01);
                break;
        }
    }

    public bool IsMuted(AudioBus bus) => IsMutedInternal(bus);

    bool IAudioMuteQuery.IsMuted(AudioBus bus) => IsMutedInternal(bus);

    public void SetVolume01(AudioBus bus, float v01)
    {
        v01 = Mathf.Clamp01(v01);

        switch (bus)
        {
            case AudioBus.All:
                masterVol01 = v01;
                if (!mutedAll) ApplyMixerVolume(masterVolumeParam, masterVol01);
                break;

            case AudioBus.Music:
                musicVol01 = v01;
                if (!mutedMusic) ApplyMixerVolume(musicVolumeParam, musicVol01);
                break;

            case AudioBus.Sound:
                sfxVol01 = v01;
                if (!mutedSfx) ApplyMixerVolume(sfxVolumeParam, sfxVol01);
                break;
        }
    }

    public float GetVolume01(AudioBus bus)
    {
        return bus switch
        {
            AudioBus.All => masterVol01,
            AudioBus.Music => musicVol01,
            AudioBus.Sound => sfxVol01,
            _ => 1f
        };
    }

    public void MusicPlay(SoundRef music, float volume01 = 1f, bool restartIfSame = false)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!unlockedOnce) return;
#endif
        if (mutedAll || mutedMusic) return;
        musicPlayer?.Play(music, volume01, restartIfSame);
    }

    public void MusicPause() => musicPlayer?.Pause();
    public void MusicStop() => musicPlayer?.Stop();

    public void UnlockWebGLAudio()
    {
        unlockedOnce = true;
        AudioListener.pause = false;
    }

    private bool IsMutedInternal(AudioBus bus)
    {
        return bus switch
        {
            AudioBus.All => mutedAll,
            AudioBus.Music => mutedMusic,
            AudioBus.Sound => mutedSfx,
            _ => false
        };
    }

    private void WarmPool(int count)
    {
        for (int i = 0; i < count; i++)
            free.Enqueue(CreateVoice());
    }

    private AudioSource CreateVoice()
    {
        var go = new GameObject("AudioVoice");
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f;
        return src;
    }

    private AudioHandle PlayInternal(SoundRef s, PlayOptions opt)
    {
        if (s == null || s.clip == null) return default;

#if UNITY_WEBGL && !UNITY_EDITOR
        if (!unlockedOnce) return default;
#endif

        if (mutedAll) return default;
        if (s.bus == AudioBus.Music && mutedMusic) return default;
        if (s.bus == AudioBus.Sound && mutedSfx) return default;

        if (s.cooldown > 0f)
        {
            if (lastPlayTime.TryGetValue(s, out var t) && Time.unscaledTime - t < s.cooldown)
                return default;
            lastPlayTime[s] = Time.unscaledTime;
        }

        playingCount.TryGetValue(s, out var cnt);
        if (s.maxSimultaneous > 0 && cnt >= s.maxSimultaneous)
            return default;

        var src = AcquireVoice();
        if (src == null) return default;

        ConfigureSource(src, s, opt);

        if (opt.Position.HasValue) src.transform.position = opt.Position.Value;
        if (opt.Follow != null) StartCoroutine(FollowRoutine(src, opt.Follow));

        src.Play();

        MarkBusy(src);
        voiceSound[src] = s;
        playingCount[s] = cnt + 1;

        if (!src.loop)
            StartCoroutine(ReleaseWhenDone(src, s));

        return new AudioHandle(src, s);
    }

    private void ConfigureSource(AudioSource src, SoundRef s, PlayOptions opt)
    {
        src.clip = s.clip;
        src.outputAudioMixerGroup = s.mixerGroup;

        float vol = s.volume;
        if (opt.VolumeMul.HasValue) vol *= opt.VolumeMul.Value;
        if (opt.VolumeAdd.HasValue) vol += opt.VolumeAdd.Value;
        src.volume = Mathf.Clamp01(vol);

        float pitch = s.pitch;
        if (s.randomPitch && s.pitchJitter > 0f)
            pitch += Random.Range(-s.pitchJitter, s.pitchJitter);

        if (opt.PitchMul.HasValue) pitch *= opt.PitchMul.Value;
        if (opt.PitchAdd.HasValue) pitch += opt.PitchAdd.Value;
        src.pitch = Mathf.Clamp(pitch, 0.1f, 3f);

        src.loop = s.loop;

        bool spatial = opt.SpatialOverride ?? s.spatial;
        if (spatial)
        {
            src.spatialBlend = Mathf.Clamp01(s.spatialBlend);
            src.minDistance = Mathf.Max(0.01f, s.minDistance);
            src.maxDistance = Mathf.Max(src.minDistance + 0.01f, s.maxDistance);
            src.rolloffMode = AudioRolloffMode.Logarithmic;
        }
        else
        {
            src.spatialBlend = 0f;
        }
    }

    private AudioSource AcquireVoice()
    {
        if (free.Count > 0)
            return free.Dequeue();

        int total = free.Count + busyOrder.Count;
        if (total < maxVoices)
            return CreateVoice();

        if (!stealOldestWhenFull || busyOrder.First == null)
            return null;

        var victim = busyOrder.First.Value;
        if (victim != null)
        {
            voiceSound.TryGetValue(victim, out var snd);
            ReleaseVoice(victim, snd);
        }
        return victim;
    }

    private void MarkBusy(AudioSource src)
    {
        if (busyNode.ContainsKey(src))
            return;

        var node = busyOrder.AddLast(src);
        busyNode[src] = node;
    }

    private IEnumerator ReleaseWhenDone(AudioSource src, SoundRef s)
    {
        while (src != null && src.isPlaying)
            yield return null;

        ReleaseVoice(src, s);
    }

    private void ReleaseVoice(AudioSource src, SoundRef s)
    {
        if (src == null) return;

        if (s != null && playingCount.TryGetValue(s, out var cnt))
            playingCount[s] = Mathf.Max(0, cnt - 1);

        src.Stop();
        src.clip = null;
        src.outputAudioMixerGroup = null;
        src.loop = false;
        src.spatialBlend = 0f;

        if (busyNode.TryGetValue(src, out var node))
        {
            busyOrder.Remove(node);
            busyNode.Remove(src);
        }

        voiceSound.Remove(src);
        free.Enqueue(src);
    }

    private IEnumerator FollowRoutine(AudioSource src, Transform target)
    {
        while (src != null && target != null && src.isPlaying)
        {
            src.transform.position = target.position;
            yield return null;
        }
    }

    private void StopAllByBus(AudioBus bus)
    {
        var toStop = new List<AudioSource>();

        foreach (var src in busyOrder)
        {
            if (src == null) continue;

            if (!voiceSound.TryGetValue(src, out var s) || s == null)
                continue;

            if (bus == AudioBus.All || s.bus == bus)
                toStop.Add(src);
        }

        foreach (var src in toStop)
        {
            voiceSound.TryGetValue(src, out var s);
            ReleaseVoice(src, s);
        }
    }

    private void ApplyMixerVolume(string param, float volume01)
    {
        if (mixer == null) return;
        if (string.IsNullOrWhiteSpace(param)) return;

        float db = Linear01ToDb(volume01);
        mixer.SetFloat(param, db);
    }

    private static float Linear01ToDb(float v01)
    {
        v01 = Mathf.Clamp01(v01);
        if (v01 <= 0.0001f) return -80f;
        return Mathf.Log10(v01) * 20f;
    }
}
