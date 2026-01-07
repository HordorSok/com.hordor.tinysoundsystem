using System.Collections.Generic;
using R3;

public sealed class SoundState
{
    readonly Dictionary<AudioBus, ReactiveProperty<bool>> mutedByBus = new();

    public ReadOnlyReactiveProperty<bool> IsMuted(AudioBus bus)
        => GetOrCreate(bus);

    public bool GetMuted(AudioBus bus)
        => GetOrCreate(bus).Value;

    public void SetMuted(AudioBus bus, bool isMuted)
        => GetOrCreate(bus).Value = isMuted;

    public void ToggleMute(AudioBus bus, bool isMuted)
        => SetMuted(bus, isMuted);

    ReactiveProperty<bool> GetOrCreate(AudioBus bus)
    {
        if (!mutedByBus.TryGetValue(bus, out var rp))
        {
            rp = new ReactiveProperty<bool>(false);
            mutedByBus.Add(bus, rp);
        }
        return rp;
    }
}
