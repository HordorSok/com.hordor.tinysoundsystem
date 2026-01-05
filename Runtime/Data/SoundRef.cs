using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "TinySoundSystem/SoundRef", fileName = "SND_")]
public class SoundRef : ScriptableObject
{
    [Header("Clip")]
    public AudioClip clip;

    [Header("Routing")]
    public AudioMixerGroup mixerGroup;
    public AudioBus bus = AudioBus.Sound;

    [Header("Defaults")]
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
    public bool loop = false;

    [Header("Randomization (optional)")]
    public bool randomPitch = false;
    [Range(0f, 0.5f)] public float pitchJitter = 0.05f;

    [Header("3D (optional)")]
    public bool spatial = false;
    [Range(0f, 1f)] public float spatialBlend = 1f;
    public float minDistance = 1f;
    public float maxDistance = 25f;

    [Header("Limits")]
    public int maxSimultaneous = 6;
    public float cooldown = 0f;
}
