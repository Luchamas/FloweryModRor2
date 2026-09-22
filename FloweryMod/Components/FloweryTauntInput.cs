using BepInEx.Configuration;
using EntityStates;
using FloweryMod.Modules;
using FloweryMod.SkillStates;
using RoR2;
using UnityEngine;

namespace FloweryMod.Components
{
    /// <summary>
    /// Starts Flowery's taunts off the keyboard - Ctrl+1, Ctrl+2 and Ctrl+3 unless rebound.
    ///
    /// Taunts are not skills: there is no slot for them and no button in RoR2's input map, so
    /// nothing in the game will ever start one. This reads the keys itself and puts the taunt on
    /// the Body machine directly. It only ever acts on the body this machine controls, and the
    /// entity state machine networks the state from there like any other, so every client sees
    /// the taunt without a message of our own.
    /// </summary>
    public class FloweryTauntInput : MonoBehaviour
    {
        private CharacterBody body;
        private InputBankTest inputBank;
        private EntityStateMachine bodyMachine;
        private EntityStateMachine weaponMachine;

        private void Awake()
        {
            body = GetComponent<CharacterBody>();
            inputBank = GetComponent<InputBankTest>();
        }

        private void Start()
        {
            bodyMachine = EntityStateMachine.FindByCustomName(gameObject, "Body");
            weaponMachine = EntityStateMachine.FindByCustomName(gameObject, "Weapon");
        }

        // Update, not FixedUpdate: GetKeyDown is true for exactly one rendered frame, and a
        // physics tick can fall either side of it or not at all.
        private void Update()
        {
            if (body == null || !body.hasEffectiveAuthority || bodyMachine == null) return;
            if (!IsLocalPlayerFree()) return;

            EntityState taunt = PressedTaunt();
            if (taunt == null || !CanTaunt()) return;

            bodyMachine.SetNextState(taunt);
        }

        private static EntityState PressedTaunt()
        {
            if (Pressed(FloweryConfig.TauntHairFlipKey)) return new HairFlipTaunt();
            if (Pressed(FloweryConfig.TauntFrandiscoKey)) return new FrandiscoTaunt();
            if (Pressed(FloweryConfig.TauntFeintKey)) return new FeintTaunt();
            return null;
        }

        /// <summary>
        /// Whether a person at this keyboard is driving this body and nothing has their input.
        ///
        /// hasEffectiveAuthority alone is not it: the host has authority over every AI body, so
        /// an allied Flowery would taunt whenever the host pressed the key. And the UI test is the
        /// same one the game uses before it passes any input to a body at all - chat, the console
        /// and the pause menu all count - so Ctrl+1 typed into chat stays in chat.
        /// </summary>
        private bool IsLocalPlayerFree()
        {
            CharacterMaster master = body.master;
            PlayerCharacterMasterController player = master != null ? master.playerCharacterMasterController : null;
            NetworkUser user = player != null ? player.networkUser : null;
            LocalUser local = user != null ? user.localUser : null;
            return local != null && !local.isUIFocused;
        }

        /// <summary>
        /// Only from rest. Mid-dash or mid-swing a taunt would cut the skill off, which is not
        /// what anyone reaching for Ctrl+1 meant.
        ///
        /// Mid-taunt is not rest either. A taunt is not in the main state, so it already counts
        /// as busy here - which is the point: letting one taunt replace another meant mashing the
        /// key restarted the line and the pose on every press, a stutter rather than a taunt.
        /// </summary>
        private bool CanTaunt()
        {
            bool weaponFree = weaponMachine == null || weaponMachine.IsInMainState();
            return bodyMachine.IsInMainState() && weaponFree && !BaseFloweryTaunt.PlayerIsActing(inputBank);
        }

        /// <summary>
        /// The main key went down this frame with every modifier held.
        ///
        /// Not KeyboardShortcut.IsDown, which also refuses whenever any key outside the shortcut
        /// is held - so Ctrl+1 does nothing while Shift is down for sprint, Tab for the scoreboard,
        /// or the right-hand Ctrl instead of the left one the default names.
        /// </summary>
        private static bool Pressed(ConfigEntry<KeyboardShortcut> entry)
        {
            if (entry == null) return false;

            KeyboardShortcut shortcut = entry.Value;
            if (shortcut.MainKey == KeyCode.None || !Input.GetKeyDown(shortcut.MainKey)) return false;

            foreach (KeyCode modifier in shortcut.Modifiers)
            {
                if (!Held(modifier)) return false;
            }
            return true;
        }

        /// <summary>A key held down, with either side's Ctrl, Shift or Alt standing in for the other.</summary>
        private static bool Held(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.LeftControl:
                case KeyCode.RightControl:
                    return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                case KeyCode.LeftShift:
                case KeyCode.RightShift:
                    return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                case KeyCode.LeftAlt:
                case KeyCode.RightAlt:
                    return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
                default:
                    return Input.GetKey(key);
            }
        }
    }
}
