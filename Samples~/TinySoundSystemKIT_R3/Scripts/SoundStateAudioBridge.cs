using System;
using UnityEngine;
using R3;

public sealed class SoundStateAudioBridge : MonoBehaviour // It can be placed near AudioManager component
{
    // We can create a bridge for different managers if we dont want to change API of existing managers
    [SerializeField] R3StateProvider provider; // Get in service locator
    [SerializeField] AudioManager audioManager; // Get in service locator

    readonly CompositeDisposable disposables = new();

    void OnEnable()
    {
        foreach (AudioBus bus in Enum.GetValues(typeof(AudioBus)))
        {
            provider.SoundState
                .IsMuted(bus)
                .Subscribe(isMuted => audioManager.SetMuted(bus, isMuted))
                .AddTo(disposables);
        }
    }

    void OnDisable()
    {
        disposables.Dispose();
    }
}
