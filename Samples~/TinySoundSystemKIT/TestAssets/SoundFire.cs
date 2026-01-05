using UnityEngine;

public class SoundFire : MonoBehaviour
{
    [Header("FireSoundOnSpacePress")]
    [SerializeField] private SoundRef soundRef;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            AudioManager.PlaySound(soundRef);
    }
}
