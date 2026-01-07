using UnityEngine;
using UnityEngine.UI;
using R3;
using System;

namespace TinySoundSystem
{
    public class MuteToggleView : MonoBehaviour
    {
        [SerializeField] Toggle muteToggle;
        [SerializeField] AudioBus bus;
        [SerializeField] Image muteIcon;
        [SerializeField] R3StateProvider R3StateProvider; // Get in service locator

        IDisposable sub;
        bool isApplyingState;

        void Awake() => muteToggle.onValueChanged.AddListener(ChangeState);
        void OnDestroy() => muteToggle.onValueChanged.RemoveListener(ChangeState);

        void OnEnable()
        {
            sub = R3StateProvider.SoundState
                .IsMuted(bus)
                .Subscribe(UpdateState);

            UpdateState(R3StateProvider.SoundState.GetMuted(bus));
        }

        void OnDisable()
        {
            sub?.Dispose();
            sub = null;
        }

        void ChangeState(bool isOn)
        {
            if (isApplyingState) return;
            R3StateProvider.SoundState.ToggleMute(bus, isOn);
        }

        void UpdateState(bool isOn)
        {
            isApplyingState = true;
            muteToggle.SetIsOnWithoutNotify(isOn);
            muteIcon.gameObject.SetActive(isOn);
            isApplyingState = false;
        }
    }

}
