using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "TinySoundSystem/SoundRef", fileName = "SND_")]
public class SoundRef : ScriptableObject
{
    [Header("Clip")]
    public AudioClip clip;

    [Header("Routing")]
    public AudioMixerGroup mixerGroup;

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
    [Tooltip("Max simultaneous instances of this sound (0 = unlimited).")]
    public int maxSimultaneous = 6;

    [Tooltip("Min time (sec) between triggers of this sound (0 = no cooldown).")]
    public float cooldown = 0f;
}
