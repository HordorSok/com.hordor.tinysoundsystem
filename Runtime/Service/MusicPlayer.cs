using UnityEngine;
using UnityEngine.Audio;

public sealed class MusicPlayer : MonoBehaviour
{
    [SerializeField] private AudioMixerGroup musicGroup;

    private AudioSource src;
    private SoundRef current;
    private bool paused;
    private IAudioMuteQuery muteQuery;

    public void Bind(IAudioMuteQuery query) => muteQuery = query;

    private void Awake()
    {
        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = true;
        src.spatialBlend = 0f;
        if (musicGroup != null) src.outputAudioMixerGroup = musicGroup;
    }

    public void Play(SoundRef music, float volume01 = 1f, bool restartIfSame = false)
    {
        if (music == null || music.clip == null) return;
        if (muteQuery != null && (muteQuery.IsMuted(AudioBus.All) || muteQuery.IsMuted(AudioBus.Music))) return;

        bool sameClip = current != null && current.clip == music.clip;

        if (paused && sameClip && !restartIfSame)
        {
            src.volume = Mathf.Clamp01(music.volume * volume01);
            src.UnPause();
            paused = false;
            return;
        }

        current = music;
        paused = false;

        src.Stop();
        src.clip = music.clip;
        src.loop = music.loop;
        src.pitch = Mathf.Clamp(music.pitch, 0.1f, 3f);
        src.volume = Mathf.Clamp01(music.volume * volume01);

        if (music.mixerGroup != null) src.outputAudioMixerGroup = music.mixerGroup;

        src.Play();
    }

    public void Pause()
    {
        if (src.clip == null) return;
        if (!src.isPlaying) return;
        src.Pause();
        paused = true;
    }

    public void Stop()
    {
        src.Stop();
        paused = false;
    }

    public bool IsPlaying => src.isPlaying;
    public bool IsPaused => paused;
    public SoundRef Current => current;
}
