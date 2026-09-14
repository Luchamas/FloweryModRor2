using FloweryMod.Modules;
using RoR2;
using UnityEngine;

namespace FloweryMod.SkillStates
{
    /// <summary>
    /// The launch half of "Here I come San Frandisco!" - a long, committed dash that crosses
    /// most of an arena. Always at full power, since the channel is a fixed length.
    /// </summary>
    public class SanFranciscoDash : BaseFloweryDash
    {
        public static float speed = 52f;
        public static float duration = 0.5f;
        public static float tensionPerEnemy = 7f;

        protected override float DashSpeed => speed;
        protected override float DashDuration => duration;
        protected override float DamageCoefficient => FloweryConfig.SanFranciscoDamage.Value;
        protected override float TensionPerEnemy => tensionPerEnemy;

        // Silent: SanFranciscoChannel already shouted on the button press.
        protected override string VoiceCategory => null;

        protected override void SpawnDashEffect()
        {
            if (FloweryAssets.JaronaChargeEffect == null) return;

            FloweryEffects.Spawn(FloweryAssets.JaronaChargeEffect, new EffectData
            {
                origin = transform.position + Vector3.up,
                scale = 3.5f,
                rotation = Quaternion.LookRotation(dashDirection),
                color = FloweryAssets.SoulBlue,
            }, false);
        }
    }
}
