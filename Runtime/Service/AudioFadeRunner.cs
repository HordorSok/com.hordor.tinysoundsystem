using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public sealed class AudioFadeRunner : MonoBehaviour
{
    private readonly Dictionary<string, Coroutine> coroutines = new();
    private readonly Dictionary<string, int> tokens = new();

    public void FadeMixer(AudioMixer mixer, string key, string param, float from01, float to01, float duration, Action onComplete = null)
    {
        if (string.IsNullOrWhiteSpace(key)) key = param;

        StopFade(key);

        int token = 0;
        tokens.TryGetValue(key, out token);
        token++;
        tokens[key] = token;

        var c = StartCoroutine(FadeMixerRoutine(mixer, key, token, param, from01, to01, duration, onComplete));
        coroutines[key] = c;
    }

    public void StopFade(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        if (coroutines.TryGetValue(key, out var c) && c != null)
            StopCoroutine(c);

        coroutines.Remove(key);
    }

    private IEnumerator FadeMixerRoutine(AudioMixer mixer, string key, int token, string param, float from01, float to01, float duration, Action onComplete)
    {
        if (mixer == null || string.IsNullOrWhiteSpace(param)) yield break;

        float t = 0f;

        while (t < duration)
        {
            if (!tokens.TryGetValue(key, out var cur) || cur != token) yield break;

            t += Time.unscaledDeltaTime;
            float k = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
            float v = Mathf.Lerp(from01, to01, k);
            mixer.SetFloat(param, Linear01ToDb(v));
            yield return null;
        }

        if (!tokens.TryGetValue(key, out var cur2) || cur2 != token) yield break;

        mixer.SetFloat(param, Linear01ToDb(to01));
        coroutines.Remove(key);
        onComplete?.Invoke();
    }

    private static float Linear01ToDb(float v01)
    {
        v01 = Mathf.Clamp01(v01);
        if (v01 <= 0.0001f) return -80f;
        return Mathf.Log10(v01) * 20f;
    }
}
