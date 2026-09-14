using FloweryMod.Content;
using FloweryMod.Modules;
using RoR2;
using UnityEngine;

namespace FloweryMod.Components
{
    /// <summary>
    /// Plays a "Selected" voice clip when the local player picks Flowery on the character
    /// select screen.
    ///
    /// Polls the local user's body preference instead of hooking the UI: the preference is the
    /// value the select screen actually writes, and polling keeps the mod hook-free.
    /// </summary>
    public class FloweryMenuSounds : MonoBehaviour
    {
        private const float PollInterval = 0.15f;

        private BodyIndex floweryBodyIndex = BodyIndex.None;
        private BodyIndex lastPreference = BodyIndex.None;
        private float pollTimer;

        internal static void Spawn()
        {
            var host = new GameObject("FloweryMenuSounds");
            host.AddComponent<FloweryMenuSounds>();
            DontDestroyOnLoad(host);
        }

        private void Update()
        {
            pollTimer -= Time.unscaledDeltaTime;
            if (pollTimer > 0f) return;
            pollTimer = PollInterval;

            // Only in menus. During a run the preference is still set, and we do not want the
            // clip firing again on every stage transition.
            if (Run.instance != null)
            {
                lastPreference = BodyIndex.None;
                return;
            }

            if (floweryBodyIndex == BodyIndex.None)
            {
                floweryBodyIndex = BodyCatalog.FindBodyIndex(FloweryBody.BodyName);
                if (floweryBodyIndex == BodyIndex.None) return;
            }

            LocalUser localUser = LocalUserManager.GetFirstLocalUser();
            NetworkUser networkUser = localUser != null ? localUser.currentNetworkUser : null;
            if (networkUser == null) return;

            BodyIndex preference = networkUser.bodyIndexPreference;
            if (preference == lastPreference) return;

            // Picking someone else mid-line cuts him off, the way the mannequin on screen is
            // swapped out: his introduction should not carry on over another survivor's.
            if (lastPreference == floweryBodyIndex) Sounds.StopUI();

            lastPreference = preference;
            if (preference == floweryBodyIndex) Sounds.PlayUI(Sounds.Selected);
        }
    }
}
