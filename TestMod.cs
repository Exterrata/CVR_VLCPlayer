using UnityEngine;
using MelonLoader;
using HarmonyLib;
using BTKUILib;
using ABI.CCK.Components;
using ABI_RC.Core.InteractionSystem;
using BTKUILib.UIObjects;

[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonInfo(typeof(VLCTest.VLCMod), "VLCPlayer", "1.0.0", "Exterrata")]

namespace VLCTest
{
    public class VLCMod : MelonMod
    {
        private bool _initialized;

        private BTKUILib.UIObjects.Components.Button _button;

        public static bool useVlc = true;

        public override void OnApplicationStart()
        {
            MelonLogger.Msg("Start");
            QuickMenuAPI.OnMenuRegenerate += Setup;
        }
        void Setup(CVR_MenuManager unused)
        {
            if (_initialized) return;
            _initialized = true;

            var page = QuickMenuAPI.MiscTabPage;

            var Category = page.AddCategory("VLCPlayer");

            var button = Category.AddButton("Switch To AVPro", "idk", "Swap Players");
            button.OnPress += swap;
            _button = button;
        }

        private void swap() { useVlc = !useVlc; _button.ButtonText = useVlc ? "Switch To AVPro" : "Switch To VLC"; }
    }

    [HarmonyPatch]
    internal class Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CVRVideoPlayer), "SwitchVideoPlayer")]
        private static void Prefix_CVRVideoPlayer_SwitchVideoPlayer(ref System.Type type)
        {
            if (!VLCMod.useVlc)
                return;

            type = typeof(VLCPlayer);
            MelonLogger.Msg("Replaced Video Player");
        }
    }
}