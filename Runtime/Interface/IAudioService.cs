public interface IAudioService
{
    AudioHandle Play(SoundRef s);
    AudioHandle Play(SoundRef s, PlayOptions opt);
    void Stop(AudioHandle h);

    void SetMuted(AudioBus bus, bool muted);
    bool IsMuted(AudioBus bus);

    void SetVolume01(AudioBus bus, float v01);
    float GetVolume01(AudioBus bus);

    void MusicPlay(SoundRef music, float volume01 = 1f, bool restartIfSame = false);
    void MusicPause();
    void MusicStop();

    void UnlockWebGLAudio();
}
