using UnityEngine;
using R3;

public class R3StateProvider : MonoBehaviour // Add this to service locator
{
    public SoundState SoundState = new ();
    // public WalletState WalletState;
    // public QuestState QuestState;
    // public InputSettingsState InputSettingsState;
    // public GameProgressState GameProgressState;

    // private void Awake()
    // {
    //     Init();
    // }

    // public void Init()
    // {
    //     SoundState = new SoundState();
    //     // WalletState = new WalletState();
    //     // QuestState = new QuestState();
    //     // InputSettingsState = new InputSettingsState();
    //     // GameProgressState = new GameProgressState();
    // }
}
