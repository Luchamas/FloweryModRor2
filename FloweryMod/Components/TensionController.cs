using FloweryMod.Modules;
using RoR2;
using UnityEngine;

namespace FloweryMod.Components
{
    /// <summary>
    /// Flowery's TP meter, modelled on DELTARUNE's Tension Points.
    ///
    /// It fills from grazing and hits, and once OMEGA FLOWERY is active it drains instead -
    /// the same shape as Void Fiend's corruption bar. The drain rate is a constant and the
    /// OMEGA buff's duration is computed from the same number, so the bar and the buff empty
    /// together without the meter itself having to be networked.
    ///
    /// Deliberately authority-local: only the client that owns the body ever mutates the value.
    /// </summary>
    public class TensionController : MonoBehaviour
    {
        private const float GrazeCheckInterval = 0.25f;

        private CharacterBody body;
        private float grazeCheckTimer;
        private bool grazing;
        private bool omegaActive;

        /// <summary>Current TP. Only meaningful on the authority.</summary>
        public float Tension { get; private set; }

        public float MaxTension => Mathf.Max(1f, FloweryConfig.TensionMax.Value);

        public float Fraction => Mathf.Clamp01(Tension / MaxTension);

        private void Awake()
        {
            body = GetComponent<CharacterBody>();
        }

        private void FixedUpdate()
        {
            if (body == null || !body.hasEffectiveAuthority) return;

            bool omegaNow = Buffs.Omega != null && body.HasBuff(Buffs.Omega);

            if (omegaNow)
            {
                omegaActive = true;
                Tension = Mathf.Max(0f, Tension - FloweryConfig.OmegaDrainPerSecond.Value * Time.fixedDeltaTime);
                return;
            }

            if (omegaActive)
            {
                // OMEGA just ended. Whatever rounding is left over goes with it.
                omegaActive = false;
                Tension = 0f;
            }

            grazeCheckTimer -= Time.fixedDeltaTime;
            if (grazeCheckTimer <= 0f)
            {
                grazeCheckTimer = GrazeCheckInterval;
                grazing = AnyEnemyWithinGrazeRadius();
            }

            if (grazing)
            {
                Add(FloweryConfig.TensionGrazePerSecond.Value * Time.fixedDeltaTime);
            }
        }

        /// <summary>
        /// Grants TP. Silently ignored off-authority, and while OMEGA is draining, so callers do
        /// not need to branch.
        /// </summary>
        public void Add(float amount)
        {
            if (amount <= 0f || omegaActive) return;
            if (body != null && !body.hasEffectiveAuthority) return;
            Tension = Mathf.Min(MaxTension, Tension + amount);
        }

        /// <summary>Grants the per-hit reward for <paramref name="hitCount"/> enemies.</summary>
        public void AddForHits(int hitCount)
        {
            if (hitCount <= 0) return;
            Add(FloweryConfig.TensionPerHit.Value * hitCount);
        }

        public bool CanSpend(float amount) => Tension + 0.001f >= amount;

        private bool AnyEnemyWithinGrazeRadius()
        {
            float radiusSqr = FloweryConfig.TensionGrazeRadius.Value * FloweryConfig.TensionGrazeRadius.Value;
            Vector3 origin = body.corePosition;
            TeamIndex myTeam = body.teamComponent != null ? body.teamComponent.teamIndex : TeamIndex.Player;

            var instances = CharacterBody.readOnlyInstancesList;
            for (int i = 0; i < instances.Count; i++)
            {
                CharacterBody other = instances[i];
                if (other == null || other == body) continue;
                if (other.teamComponent == null || other.teamComponent.teamIndex == myTeam) continue;
                if (other.healthComponent == null || !other.healthComponent.alive) continue;
                if ((other.corePosition - origin).sqrMagnitude > radiusSqr) continue;
                return true;
            }

            return false;
        }
    }
}
