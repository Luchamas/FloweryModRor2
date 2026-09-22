using System;
using System.Reflection;
using EntityStates;
using FloweryMod.Modules;
using FloweryMod.SkillStates;
using R2API;
using RoR2;
using RoR2.Skills;
using UnityEngine;

namespace FloweryMod.Content
{
    /// <summary>Builds Flowery's four skills plus his passive, and wires them onto the body.</summary>
    internal static class FlowerySkills
    {
        private static readonly FieldInfo SkillFamilyField =
            typeof(GenericSkill).GetField("_skillFamily", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// LAST JARONA. Never sits in a skill family: it is pushed onto the special slot as an
        /// override while the OMEGA buff is up, and pulled back off when it expires.
        /// </summary>
        internal static SkillDef LastJaronaSkill;

        internal static void RegisterEntityStates()
        {
            bool added;
            ContentAddition.AddEntityState<FlowerPunches>(out added);
            ContentAddition.AddEntityState<JaronaDash>(out added);
            ContentAddition.AddEntityState<SanFranciscoChannel>(out added);
            ContentAddition.AddEntityState<SanFranciscoDash>(out added);
            ContentAddition.AddEntityState<OmegaFloweryState>(out added);
            ContentAddition.AddEntityState<LastJarona>(out added);
            ContentAddition.AddEntityState<FlowerySpawnState>(out added);
            // Not skills, but networked states all the same: a taunt started on one client is
            // sent to the others by its index in the state catalog, which this is what gives it.
            ContentAddition.AddEntityState<HairFlipTaunt>(out added);
            ContentAddition.AddEntityState<FrandiscoTaunt>(out added);
            ContentAddition.AddEntityState<FeintTaunt>(out added);
        }

        internal static void Build(GameObject bodyPrefab)
        {
            var skillLocator = bodyPrefab.GetComponent<SkillLocator>();
            if (skillLocator == null)
            {
                Log.Error("Flowery body has no SkillLocator - skills were not applied.");
                return;
            }

            BuildPassive(skillLocator);
            LastJaronaSkill = BuildLastJarona();

            AssignFamily(skillLocator.primary, "FloweryPrimaryFamily", BuildPrimary());
            AssignFamily(skillLocator.secondary, "FlowerySecondaryFamily", BuildSecondary());
            AssignFamily(skillLocator.utility, "FloweryUtilityFamily", BuildUtility());
            AssignFamily(skillLocator.special, "FlowerySpecialFamily", BuildSpecial());
        }

        private static void BuildPassive(SkillLocator skillLocator)
        {
            skillLocator.passiveSkill.enabled = true;
            skillLocator.passiveSkill.skillNameToken = Tokens.PassiveName;
            skillLocator.passiveSkill.skillDescriptionToken = Tokens.PassiveDescription;
            skillLocator.passiveSkill.icon = FloweryAssets.IconPassive;
        }

        private static SkillDef BuildPrimary()
        {
            // SteppedSkillDef feeds a step index into the state, which is how vanilla melee
            // survivors alternate left and right swings.
            var skill = Create<SteppedSkillDef>("FloweryFlowerPunches", Tokens.PrimaryName,
                                                Tokens.PrimaryDescription, FloweryAssets.IconFlowerPunches,
                                                typeof(FlowerPunches), "Weapon");
            skill.stepCount = 2;
            skill.stepGraceDuration = 0.5f;
            skill.baseRechargeInterval = 0f;
            skill.baseMaxStock = 1;
            skill.interruptPriority = InterruptPriority.Any;
            skill.isCombatSkill = true;
            skill.mustKeyPress = false;
            skill.cancelSprintingOnActivation = true;
            skill.attackSpeedBuffsRestockSpeed = false;
            return skill;
        }

        private static SkillDef BuildSecondary()
        {
            // "Body", like every dash: it drives the motor, and would fight
            // GenericCharacterMain for velocity if the main state kept running.
            var skill = Create<SkillDef>("FloweryJarona", Tokens.SecondaryName, Tokens.SecondaryDescription,
                                         FloweryAssets.IconJarona, typeof(JaronaDash), "Body");
            skill.baseRechargeInterval = FloweryConfig.JaronaCooldown.Value;
            skill.baseMaxStock = Mathf.Max(1, FloweryConfig.JaronaCharges.Value);
            skill.rechargeStock = 1;
            skill.interruptPriority = InterruptPriority.PrioritySkill;
            skill.isCombatSkill = true;
            skill.mustKeyPress = true;
            skill.cancelSprintingOnActivation = false;
            skill.beginSkillCooldownOnSkillEnd = true;
            return skill;
        }

        private static SkillDef BuildUtility()
        {
            var skill = Create<SkillDef>("FlowerySanFrancisco", Tokens.UtilityName, Tokens.UtilityDescription,
                                         FloweryAssets.IconSanFrancisco, typeof(SanFranciscoChannel), "Body");
            skill.baseRechargeInterval = FloweryConfig.SanFranciscoCooldown.Value;
            skill.baseMaxStock = 1;
            skill.interruptPriority = InterruptPriority.PrioritySkill;
            skill.isCombatSkill = true;
            skill.mustKeyPress = true;
            skill.cancelSprintingOnActivation = false;
            skill.beginSkillCooldownOnSkillEnd = true;
            return skill;
        }

        private static SkillDef BuildSpecial()
        {
            var skill = Create<TensionSkillDef>("FloweryOmega", Tokens.SpecialName, Tokens.SpecialDescription,
                                                FloweryAssets.IconOmega, typeof(OmegaFloweryState), "Weapon");
            skill.requiredTension = FloweryConfig.OmegaMinTension.Value;
            skill.baseRechargeInterval = FloweryConfig.OmegaCooldown.Value;
            skill.baseMaxStock = 1;
            skill.interruptPriority = InterruptPriority.PrioritySkill;
            skill.isCombatSkill = true;
            skill.mustKeyPress = true;
            skill.cancelSprintingOnActivation = true;
            return skill;
        }

        private static SkillDef BuildLastJarona()
        {
            // "Body", not "Weapon": the dash drives the motor, and it would fight
            // GenericCharacterMain for control of velocity if the main state kept running.
            var skill = Create<SkillDef>("FloweryLastJarona", Tokens.LastJaronaName,
                                         Tokens.LastJaronaDescription, FloweryAssets.IconLastJarona,
                                         typeof(LastJarona), "Body");
            skill.baseRechargeInterval = FloweryConfig.LastJaronaCooldown.Value;
            skill.baseMaxStock = 1;
            skill.interruptPriority = InterruptPriority.PrioritySkill;
            skill.isCombatSkill = true;
            skill.mustKeyPress = true;
            skill.cancelSprintingOnActivation = true;
            return skill;
        }

        private static T Create<T>(string name, string nameToken, string descriptionToken, Sprite icon,
                                   Type activationState, string stateMachine) where T : SkillDef
        {
            var skill = ScriptableObject.CreateInstance<T>();
            // SkillDef shadows ScriptableObject.name with a read-only property.
            ((ScriptableObject)skill).name = name;
            skill.skillName = name;
            skill.skillNameToken = nameToken;
            skill.skillDescriptionToken = descriptionToken;
            skill.icon = icon;
            skill.activationState = new SerializableEntityStateType(activationState);
            skill.activationStateMachineName = stateMachine;
            skill.rechargeStock = 1;
            skill.requiredStock = 1;
            skill.stockToConsume = 1;
            skill.resetCooldownTimerOnUse = false;
            skill.fullRestockOnAssign = true;
            skill.dontAllowPastMaxStocks = false;
            skill.canceledFromSprinting = false;
            skill.forceSprintDuringState = false;

            ContentAddition.AddSkillDef(skill);
            return skill;
        }

        private static void AssignFamily(GenericSkill slot, string familyName, params SkillDef[] skills)
        {
            if (slot == null)
            {
                Log.Warning("Missing skill slot for " + familyName);
                return;
            }

            var family = ScriptableObject.CreateInstance<SkillFamily>();
            ((ScriptableObject)family).name = familyName;
            family.variants = new SkillFamily.Variant[skills.Length];

            for (int i = 0; i < skills.Length; i++)
            {
                family.variants[i] = new SkillFamily.Variant
                {
                    skillDef = skills[i],
                    unlockableDef = null,
                    viewableNode = new ViewablesCatalog.Node(skills[i].skillNameToken, false, null),
                };
            }

            ContentAddition.AddSkillFamily(family);

            slot.skillName = skills[0].skillName;
            SetSkillFamily(slot, family);
            slot.SetBaseSkill(skills[0]);
        }

        /// <summary>
        /// GenericSkill only exposes its family read-only; assigning it is standard practice for
        /// custom survivors, so the reflection is isolated here.
        /// </summary>
        private static void SetSkillFamily(GenericSkill slot, SkillFamily family)
        {
            if (SkillFamilyField == null)
            {
                Log.Error("GenericSkill._skillFamily is missing - the game's layout changed.");
                return;
            }
            SkillFamilyField.SetValue(slot, family);
        }
    }
}
