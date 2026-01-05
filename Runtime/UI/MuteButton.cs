using UnityEngine;
using UnityEngine.UI;

namespace TinySoundSystem
{
    public class MuteToggle : MonoBehaviour
    {
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private Toggle muteToggle;
        [SerializeField] private AudioBus bus;
        [SerializeField] private Image muteIcon;

        private void OnEnable() => muteToggle.onValueChanged.AddListener(Mute);
        private void OnDisable() => muteToggle.onValueChanged.RemoveListener(Mute);

        private void Mute(bool isOn)
        {
            audioManager.SetMuted(bus, isOn);
            muteIcon.gameObject.SetActive(isOn);
        }
    }
}
