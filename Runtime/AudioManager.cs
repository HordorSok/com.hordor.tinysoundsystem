using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("Pool")]
    [SerializeField] private int initialVoices = 16;
    [SerializeField] private int maxVoices = 32;

    [Header("Voice stealing (when max reached)")]
    [SerializeField] private bool stealOldestWhenFull = true;

    private readonly Queue<AudioSource> free = new();
    private readonly LinkedList<AudioSource> busyOrder = new(); // oldest -> newest
    private readonly Dictionary<AudioSource, LinkedListNode<AudioSource>> busyNode = new();

    private readonly Dictionary<SoundRef, int> playingCount = new();
    private readonly Dictionary<SoundRef, float> lastPlayTime = new();

    private bool unlockedOnce;

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        WarmPool(initialVoices);
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
        return src;
    }

    // -------------------- Public API --------------------

    /// <summary>Basic one-liner.</summary>
    public static AudioHandle PlaySound(SoundRef s) => I != null ? I.PlayInternal(s, default) : default;

    /// <summary>One-liner with modifiers (pitchAdd/volumeMul/etc.).</summary>
    public static AudioHandle PlaySound(SoundRef s, PlayOptions opt) => I != null ? I.PlayInternal(s, opt) : default;

    public static void Stop(AudioHandle h)
    {
        if (I == null || !h.IsValid) return;
        I.ReleaseVoice(h.Source, h.Sound);
    }

    /// <summary>
    /// Call this on first user gesture (button/pointerdown/keydown) in WebGL.
    /// Safe to call multiple times.
    /// </summary>
    public static void UnlockWebGLAudio()
    {
        if (I == null) return;
        I.unlockedOnce = true;
        AudioListener.pause = false;
        // Nothing else needed here; the key is: don't auto-play before gesture.
    }

    // -------------------- Internals --------------------

    private AudioHandle PlayInternal(SoundRef s, PlayOptions opt)
    {
        if (s == null || s.clip == null) return default;

        // WebGL: do not play until user gesture unlock (optional hard gate).
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!unlockedOnce)
            return default;
#endif

        // cooldown
        if (s.cooldown > 0f)
        {
            if (lastPlayTime.TryGetValue(s, out var t) && Time.unscaledTime - t < s.cooldown)
                return default;
            lastPlayTime[s] = Time.unscaledTime;
        }

        // per-sound limit
        playingCount.TryGetValue(s, out var cnt);
        if (s.maxSimultaneous > 0 && cnt >= s.maxSimultaneous)
            return default;

        var src = AcquireVoice();
        if (src == null) return default;

        ConfigureSource(src, s, opt);

        // position / follow
        if (opt.Position.HasValue) src.transform.position = opt.Position.Value;
        if (opt.Follow != null) StartCoroutine(FollowRoutine(src, opt.Follow));

        src.Play();

        MarkBusy(src);
        playingCount[s] = cnt + 1;

        if (!src.loop)
            StartCoroutine(ReleaseWhenDone(src, s));

        return new AudioHandle(src, s);
    }

    private void ConfigureSource(AudioSource src, SoundRef s, PlayOptions opt)
    {
        src.clip = s.clip;
        src.outputAudioMixerGroup = s.mixerGroup;

        // volume
        float vol = s.volume;
        if (opt.VolumeMul.HasValue) vol *= opt.VolumeMul.Value;
        if (opt.VolumeAdd.HasValue) vol += opt.VolumeAdd.Value;
        src.volume = Mathf.Clamp01(vol);

        // pitch
        float pitch = s.pitch;
        if (s.randomPitch && s.pitchJitter > 0f)
            pitch += Random.Range(-s.pitchJitter, s.pitchJitter);

        if (opt.PitchMul.HasValue) pitch *= opt.PitchMul.Value;
        if (opt.PitchAdd.HasValue) pitch += opt.PitchAdd.Value;
        src.pitch = Mathf.Clamp(pitch, 0.1f, 3f);

        // loop
        src.loop = s.loop;

        // spatial
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

        // Steal oldest voice
        var victim = busyOrder.First.Value;
        // We must release it safely first
        ReleaseVoice(victim, null);
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
        // wait while playing
        while (src != null && src.isPlaying)
            yield return null;

        ReleaseVoice(src, s);
    }

    private void ReleaseVoice(AudioSource src, SoundRef s)
    {
        if (src == null) return;

        // Update per-sound count (if we know the sound)
        if (s != null && playingCount.TryGetValue(s, out var cnt))
            playingCount[s] = Mathf.Max(0, cnt - 1);

        src.Stop();
        src.clip = null;
        src.outputAudioMixerGroup = null;
        src.loop = false;
        src.spatialBlend = 0f;

        // Remove from busy order if present
        if (busyNode.TryGetValue(src, out var node))
        {
            busyOrder.Remove(node);
            busyNode.Remove(src);
        }

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
}
