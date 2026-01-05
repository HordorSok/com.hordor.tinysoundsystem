using UnityEngine;

namespace TinySoundSystem
{
    public class SoundEmmiter : MonoBehaviour
    {
        [SerializeField] private SoundRef soundRef;
        [SerializeField] private AudioManager audioManager;

        public void Play()
        {
            audioManager.Play(soundRef);
        }
    }
}
