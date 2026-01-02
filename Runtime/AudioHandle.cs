using UnityEngine;

public readonly struct AudioHandle
{
    public readonly AudioSource Source;
    public readonly SoundRef Sound;

    public bool IsValid => Source != null;

    public AudioHandle(AudioSource source, SoundRef sound)
    {
        Source = source;
        Sound = sound;
    }
}
