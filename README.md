# Tiny Sound System

Tiny Sound System is a tiny, opinionated audio manager that fits in a single runtime folder. It is designed to be cheap and WebGL-friendly while still offering the basics most Unity projects need: a voice pool, playback limits, volume/pitch overrides, and SoundRef assets that record configuration per clip.

## Installation

1. Import the `Runtime` folder into any Unity package, project, or submodule that targets Unity 2022.3 or later.
2. Add an `AudioManager` component to a persistent `GameObject` in your first scene (no prefab is provided, so just drop the script onto a fresh GameObject).
3. Optionally increase `initialVoices`/`maxVoices` or disable `stealOldestWhenFull` depending on your workload.

## Creating SoundRefs

1. Right click in the Project window → **Create > TinySoundSystem > SoundRef**.
2. Assign an `AudioClip`. The runtime will refuse to play null clips.
3. Tune routing, defaults, and optional random pitch or 3D settings directly on the asset:
   - Use the `Randomization` section to introduce slight pitch jitter per playback.
   - Enable `Spatial` plus `min/max distance` to turn the source into a 3D sound.
   - `Max Simultaneous` and `Cooldown` gate how often this clip can run, which avoids spamming in WebGL where voice budgets are tight.

SoundRefs are just `ScriptableObject`s. You can keep them organized like any other data asset and reuse them across scenes or prefabs.

## API

- `AudioHandle AudioManager.PlaySound(SoundRef sound)`  
  Plays the configured clip with the SoundRef defaults.

- `AudioHandle AudioManager.PlaySound(SoundRef sound, PlayOptions options)`  
  Apply optional modifiers such as positional overrides, pitch/volume multipliers, transforms to follow, or forced spatialization.

- `void AudioManager.Stop(AudioHandle handle)`  
  Immediately stops and returns the voice to the pool; safe to call after a sound finishes.

- `void AudioManager.UnlockWebGLAudio()`  
  Call this once (e.g., in a button `onClick` or pointer-down handler) before attempting any playback on WebGL builds. Web browsers block audio until the user interacts, so this flags the manager to start playing.

`PlayOptions` lets you tweak playback per-call:

```
new PlayOptions(
    volumeMul: 0.8f,
    pitchAdd: 0.1f,
    position: someTransform.position,
    follow: someTransform,
    spatialOverride: true
);
```

Use `PlayOptions.At(position)` or `PlayOptions.FollowTarget(transform)` for common scenarios.

## WebGL Notes

Because browsers ignore automatic playback, wrap any UI flow in a gesture and call `AudioManager.UnlockWebGLAudio()` as soon as that gesture happens. Subsequent `PlaySound` calls will no longer be blocked. The manager also tracks per-SoundRef cooldowns and simultaneous counts to keep voice usage under control.

