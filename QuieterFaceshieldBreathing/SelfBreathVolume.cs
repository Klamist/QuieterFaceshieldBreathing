using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace SelfBreathVolume
{
    [BepInPlugin("ciallo.selfbreathvolume", "Self Breath Volume", "1.1")]
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
                new ConfigDescription("Only affect yourself", new AcceptableValueRange<int>(0, 100))
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
            if (clip == null)
                return;

            switch (MainPlugin.LastTrigger)
            {
                case EPhraseTrigger.OnBreath:
                case EPhraseTrigger.LegBroken:
                case EPhraseTrigger.Bleeding:
                case EPhraseTrigger.Dehydrated:
                case EPhraseTrigger.Exhausted:
                case EPhraseTrigger.HurtLight:
                case EPhraseTrigger.HurtMedium:
                case EPhraseTrigger.HurtHeavy:
                case EPhraseTrigger.HurtNearDeath:
                    break; // 允许继续执行
                default:
                    return; // 其他情况直接退出
            }

            float factor = MainPlugin.Volume.Value * 0.01f;
            clip.Volume = factor;
        }
    }
}
