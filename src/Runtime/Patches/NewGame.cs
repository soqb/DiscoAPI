using DiscoAPI.Runtime.Utils;
using FortressOccident;
using HarmonyLib;
using Sunshine;
using Voidforge;
using Il2CppInterop.Runtime;
using PixelCrushers.DialogueSystem;
using Sunshine.Feld;
using Sunshine.Views;

namespace DiscoAPI.Runtime.Patches;

public static class NewGamePatches
{
    [HarmonyPatch(typeof(WhirlingNewGameManager), nameof(WhirlingNewGameManager.RedButtonPressed))]
    [HarmonyPrefix]
    private static bool OnStartIntro()
    {
        RedButtonSwitch.DisableFakeButton();
        DialogueManager.StartConversation(DiscoRunner.globalConfig.newGameConversation);
        WhirlingNewGameManager.OnLoadingWhirlingLvl2();
        SingletonComponent<GameController>.Singleton.AddInputLock(Il2CppType.From(typeof(WhirlingNewGameManager)));
        WhirlingNewGameManager.TapeEnableBlackBackground();
        return false;
    }

    [HarmonyPatch(typeof(WhirlingNewGameManager), nameof(WhirlingNewGameManager.NewGameMode))]
    [HarmonyPrefix]
    private static bool OnNewGameProcessBegin(bool showIntro, bool isActivity)
    {
        CameraSelector.AddLock(Il2CppType.From(typeof(WhirlingNewGameManager)));
        SingletonComponent<GameController>.Singleton.AddInputLock(Il2CppType.From(typeof(WhirlingNewGameManager)));
        WhirlingNewGameManager.DisableOrbs();
        WhirlingNewGameManager.DisableOrbits();
        ContainerSource.ClearRegistry();
        SingletonClass<SunshineClock>.Singleton.SetDay(1);
        SingletonClass<SunshineClock>.Singleton.SetDayReal(1);
        SingletonClass<SunshineClock>.Singleton.SetTime(SunshineClock.hangoverStartTime);
        WhirlingNewGameManager.ResetStates();
        WhirlingNewGameManager.ResetVisualTequilaStates();
        WhirlingNewGameManager.SuppressHud();
        if (showIntro)
        {
            WhirlingNewGameManager.IsIntroInProgress = true;
            WhirlingNewGameManager.EnableBlack();
            SingletonComponent<DialogueImage>.Singleton.Show("darkness");
            ViewController.ToggleView(ViewType.SPECIAL);
        }
        PartyManager.RemoveAllPartyMembersFromParty();
        var ngl = DiscoRunner.globalConfig.newGameLocation;
        AreaUtils.ChangeArea(ngl.area, ngl.destinationId, true, false, true);
        if (showIntro)
        {
            WhirlingNewGameManager.IsIntroRunning = true;
            WhirlingNewGameManager.EnableDialogueFuries();
            WhirlingNewGameManager.RegisterEscapeKeyListener();
            SingletonComponent<FeldViewController>.Singleton.SetState(FeldView.NO_FELD);
        }
        else
        {
            WhirlingNewGameManager.SkipIntro(isActivity);
        }
        return false;
    }

    [HarmonyPatch(typeof(WhirlingNewGameManager), nameof(WhirlingNewGameManager.FinalizeLoadingScene))]
    [HarmonyPostfix]
    public static void OnFinalizeLoadingScene()
    {
        SingletonComponent<GameController>.Singleton.ClearAllInputLocks();
        ContinueResponseToggle.SequencerLock = false;
    }
}