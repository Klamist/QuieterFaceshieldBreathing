using BepInEx;
using BepInEx.Configuration;
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
        internal static ConfigEntry<int> V_hurt;
        internal static EPhraseTrigger LastTrigger;

        private void Awake()
        {
            Volume = Config.Bind(
                "General",
                "Breath Volume Percent",
                30,
                new ConfigDescription("Only affect yourself", new AcceptableValueRange<int>(0, 100))
            );
            V_hurt = Config.Bind(
                "General",
                "Hurt Volume Percent",
                50,
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

            float factor = 1f;

            switch (MainPlugin.LastTrigger)
            {
                // 呼吸类
                case EPhraseTrigger.OnBreath:
                case EPhraseTrigger.Dehydrated:
                case EPhraseTrigger.Exhausted:
                    factor = MainPlugin.Volume.Value * 0.01f;
                    break;

                // 受伤类
                case EPhraseTrigger.Bleeding:
                case EPhraseTrigger.Hit:
                case EPhraseTrigger.HurtLight:
                case EPhraseTrigger.HurtMedium:
                case EPhraseTrigger.HurtHeavy:
                case EPhraseTrigger.HurtNearDeath:
                case EPhraseTrigger.LegBroken:
                case EPhraseTrigger.OnAgony:
                case EPhraseTrigger.OnBeingHurt:
                case EPhraseTrigger.OnBeingHurtDissapoinment:
                    factor = MainPlugin.V_hurt.Value * 0.01f;
                    break;

                default:
                    return; // 不处理其他语音
            }

            clip.Volume = factor;
        }
    }
}
