using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace MaskBreathVolume
{
    [BepInPlugin("ciallo.MaskBreathVolume", "Mask Faceshield Breath Volume", "2.0.0")]
    public class MainPlugin : BaseUnityPlugin
    {
        internal static ConfigEntry<int> Volume;
        internal static EPhraseTrigger LastTrigger;

        private void Awake()
        {
            Volume = Config.Bind(
                "General",
                "Volume Percent",
                40,
                new ConfigDescription("0 - 100", new AcceptableValueRange<int>(0, 100))
            );

            new PlayerOnPhraseToldPatch().Enable();
            new PlayerPlaySpeechPatch().Enable();
        }
    }

    // 捕获语音事件
    internal class PlayerOnPhraseToldPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.OnPhraseTold));
        }

        [PatchPrefix]
        private static void Prefix(Player __instance, EPhraseTrigger @event, TaggedClip clip)
        {
            if (__instance != Singleton<GameWorld>.Instance.MainPlayer)
                return;

            MainPlugin.LastTrigger = @event;
        }
    }

    // 调整呼吸音量
    internal class PlayerPlaySpeechPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "PlaySpeechFromTime");
        }

        [PatchPrefix]
        private static void Prefix(Player __instance, TaggedClip clip, ref float time)
        {
            if (__instance != Singleton<GameWorld>.Instance.MainPlayer)
                return;
            if (MainPlugin.LastTrigger != EPhraseTrigger.OnBreath)
                return;
            if (clip == null)
                return;

            float factor = MainPlugin.Volume.Value * 0.01f;
            clip.Volume = factor;
        }
    }
}
