# Flowery — a DELTARUNE survivor for Risk of Rain 2

Adds **Flowery**, leader of the Flowers and the antagonist of DELTARUNE Chapter 5, as a
playable survivor.

Flowery's own low-poly model, his animations, icons, HUD art and voice clips all ship with the
mod: the model and animations through an asset bundle, everything else as loose files.

---

## The character

Flowery is the Dark World manifestation of the golden flower from Asgore's wedding bouquet.
His kit includes the Jarona and his OMEGA form

## Kit

| Slot | Name | What it does |
| --- | --- | --- |
| Passive | **TENSION** | Standing near enemies (grazing) and landing hits build TP, up to 100. |
| Primary | **Flower Punches** | Alternating melee punches, 230% each, 0.6s per swing. |
| Secondary | **Jarona** | Instant dash through enemies, ~15m, 200%. Two charges, 2s each. Lunges, kicks or shrugs, at random. |
| Utility | **Here I Come San Frandisco!** | Hang in the air for a fixed 0.8s, then launch across the arena through everything for 300%. 6s. |
| Special | **OMEGA FLOWERY** | Needs **75 TP**. 400% + 8% per TP vine eruption, then OMEGA form. 10s. |
| Special *(while OMEGA)* | **LAST JARONA** | 0.8s wind-up, then a 600% impact dash that detonates for 1200% in a 14m blast. 3s. |

**OMEGA form** works like Void Fiend's corruption bar: activating it does not spend the TP up
front — the meter **drains** at 5 TP/s for as long as the form is up, and OMEGA ends when it
hits zero. A full 100 TP bar buys 20 seconds; the 75 TP minimum buys 15. TP does not build
while draining.

While OMEGA is up Flowery **flies freely in the direction he is looking** — forward input
follows the aim ray, so looking up and pushing forward climbs; strafing stays horizontal, and
with no input he simply hangs there. He also deals **+25% damage**, and his special slot
becomes LAST JARONA.

Transforming lifts him off the ground and makes him **invulnerable for 0.2s** while the
eruption winds up. LAST JARONA has a **0.8s wind-up** so the punch lands on "...Jarona!".

110 base health, 12 base damage, no armor. He is fragile on purpose: TP rewards exactly the
player who stays close to danger, and the two dashes are what get you back out.

Every number lives in `BepInEx/config/com.deltarune.FloweryMod.cfg` after the first launch.

## Repository layout

```
Ror2Mod/
├── FloweryMod/              the BepInEx plugin (C#)
│   ├── FloweryPlugin.cs     entry point
│   ├── Modules/             assets, effects, config, tokens, buffs, gravity, log, diagnostics
│   ├── Content/             body, skills, SurvivorDef
│   ├── Components/          TP meter, HUD bar, OMEGA form, the run lean
│   ├── SkillStates/         one entity state per skill
│   └── Assets/              icons, HUD art and voice clips, copied next to the DLL on build
├── Build/                   Thunderstore packaging, and the player-facing README
└── libs/                    reference assemblies (game, BepInEx, R2API) - not committed
```

The art pipeline is not in the repository either, because it only works against the purchased
model: `FloweryRig/` (Blender scripts that pose Flowery and export his animations) and
`HenryUnityProject/` (the Unity project that builds the `flowery` asset bundle). Packaging needs
that bundle at `HenryUnityProject/AssetBundles/flowery`.

## Building

Needs the .NET SDK (any recent version; the project targets `net472`).

`libs/` is gitignored — those assemblies belong to the game and to R2API. After a fresh clone,
or when the game updates, repopulate it:

```bash
bash Build/sync-libs.sh
```

Then:

```bash
dotnet build FloweryMod/FloweryMod.csproj
```

The build copies the DLL straight into `Risk of Rain 2/BepInEx/plugins/FloweryMod/`. To skip
that:

```bash
dotnet build FloweryMod/FloweryMod.csproj -p:DeployOnBuild=false
```

If the game lives somewhere else, edit `<RoR2Dir>` in the `.csproj`.

## Dependencies installed in the game

The dev install on this machine already has everything under
`C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2`:

- BepInExPack 5.4.2121 plus BepInEx_GUI, FixPluginTypesSerialization, SeekersPatcher, MiscFixes
- HookGenPatcher 1.2.9
- RoR2BepInExPack 1.43.0
- R2API: Core, ContentManagement, Prefab, Language, Networking, RecalculateStats, Sound,
  Skills, Unlockable

**Watch the folder layout:** BepInEx 5.4.2121 **deletes** `BepInEx/plugins/RoR2BepInExPack`
as legacy-path cleanup. Packages must sit in `BepInEx/plugins/<Author>-<Package>/`, which is
r2modman's layout. Installing by hand under the short name makes all of R2API fail with
`Newtonsoft.Json` not found.

To uninstall: delete `BepInEx/`, `winhttp.dll`, `doorstop_config.ini` and `.doorstop_version`
from the game folder.

## Art

Loose image files, same workflow as the voice clips. Drop a PNG or JPEG into
`FloweryMod/Assets/Icons/` (or `Hud/`) and it is copied to the plugin on build and picked up at
startup. The filename is the key:

| File | Used as |
| --- | --- |
| `CharSelection` | Survivor portrait / character select icon |
| `Primary` | Flower Punches icon |
| `Jarona` | Jarona (secondary) icon |
| `Passive` | TENSION icon |
| `HereICome` | Here I Come San Frandisco icon |
| `OmegaFlowery` | OMEGA FLOWERY icon (also the buff icon) |
| `LastJarona` | LAST JARONA icon |
| `Hud/TP_Bar_Empty` | The TP bar artwork |

Nothing stands in for a missing file: the icon is blank (or there is no TP bar) and the log names
the file it looked for.

The **TP bar** hangs down the left edge of the HUD. The artwork is an empty frame — black
outline, dark red interior, "TP" already lettered into it — and the plugin derives the fill
overlay from that same image by recolouring the red interior to rgb(255, 160, 64) and erasing
everything else. That way the fill follows the frame's slanted interior exactly instead of
being an approximate rectangle over it, and only the percentage is drawn in code.

The VFX are borrowed: Acrid's claw slash, Merc's impact spark and REX's plant explosion.

The **model** comes out of the asset bundle, as a GameObject named `mdlFlowery`. Without it
Flowery wears Loader's model, and the log says so. The bundle is built by
**Flowery ▸ Set Up Low-Poly Flowery** in `HenryUnityProject`, which also deploys it to the game;
`Build/package.sh` takes it from `HenryUnityProject/AssetBundles/flowery`.

## Voice clips

Loose `.wav` files, no Unity round-trip: drop a file in a folder and it is in the game next
build. They live in `FloweryMod/Assets/Sounds/<category>/` and are copied to
`BepInEx/plugins/FloweryMod/Assets/Sounds/` on build.

The folder name is the category, nesting included. A folder with several files picks one at
random each time.

| Folder | Plays when |
| --- | --- |
| `Primary/` | Thorn Lash swings |
| `Secondary/` | Jarona dashes |
| `Utility/` | San Frandisco launches |
| `Special/Transform/` | OMEGA FLOWERY activates |
| `Special/LastJarona/` | LAST JARONA fires |
| `Selected/` | Flowery is picked on the character select screen |

Files must be uncompressed PCM WAV (8/16/24/32-bit, or 32-bit float). The plugin decodes them
itself at startup, so a compressed WAV is skipped with a warning in the log. Setting the volume
config to 0 mutes them.

They also follow the game's audio settings: the config volume is multiplied by the **Master** and
**SFX** sliders (there is no voice slider), and they go silent while the game is muting itself for
having lost focus. The game applies those settings inside Wwise, which these clips never pass
through, so `Sounds.Volume` reads the console variables (`volume_master`, `volume_sfx`,
`audio_focused_only`) on every play and scales the samples itself.

### Why playback is odd

**Risk of Rain 2 ships with Unity's audio engine disabled** — it runs entirely on Wwise. Unity
says so itself in the log: *"Audio system is disabled, so AudioSettings.outputSampleRate cannot
be queried."* Every `AudioSource` is therefore silent, no matter how it is configured or how
many `AudioListener`s are in the scene.

So the plugin tries to wake Unity's audio system, and when that fails (it does), falls back to
the Windows waveform API (`winmm.dll` `waveOut`), which goes straight to the OS. That works,
with three consequences:

- no 3D positioning — clips are flat;
- one clip at a time, a new one cuts off the last (fine for voice lines);
- only for the character *you* control, so a lobby of Flowerys does not shout over each other.

It used to be `PlaySound`, which can only start and stop, so a line spoken just before opening
the pause menu carried on over it. The game pauses its own audio on `PauseManager.onPauseStartGlobal`
and resumes it on `onPauseEndGlobal`; the plugin listens to the same two events and calls
`waveOutPause` / `waveOutRestart`, so a line now stops mid-word and picks up where it left off.

The proper fix is a Wwise soundbank through `R2API.SoundAPI`, which needs the clips built into
a `.bnk` with Wwise Authoring. That would restore positioning, mixing and per-clip volume.

## Implementation notes

- **TP is authority-local.** The meter only changes on the machine that owns the body. Skills
  that spend TP write the spent amount into the entity state's `OnSerialize`, so the server and
  remote clients scale the effect identically without a synced variable.
- **Gravity is suspended through `gravityParameters`.** `CharacterMotor.useGravity` has an
  internal setter; granting a channeled anti-gravity is the public route and it is a counter,
  so the San Frandisco channel, both dashes and OMEGA flight compose instead of stomping
  each other.
- **LAST JARONA is a skill override**, not a fifth slot. `OmegaFormController` watches the
  OMEGA buff and pushes the skill onto the special slot at `Contextual` priority.
- **The body is a Loader clone** (`RoR2/Base/Loader/LoaderBody.prefab`), so the motor,
  hurtboxes, ragdoll, drop pod and item displays are all correct out of the box (with a custom
  model, the hurtboxes and attachment points are then moved onto his bones — see below), and the
  animator already has the `SwingFistLeft` / `SwingFistRight` states the primary plays. The
  melee HitBoxGroup that Thorn Lash swings through is built at prefab time — Loader's own fist
  boxes are sized for punches, and a custom model is not expected to ship any.
- **A full-body override layer must be released by hand.** `"FullBody, Override"` masks the
  whole rig and nothing clears it automatically, so a state that grabs it and exits leaves the
  character frozen with no animation at all. Vanilla plays `"Empty"` on that layer in `OnExit`;
  Flowery only takes the layer when it can also resolve an empty state to give it back with,
  and falls back to a gesture-layer swing otherwise.
- **Attack states report `InterruptPriority.Skill`, not `Any`.** `SkillDef.CanExecute` never
  checks whether the machine is already in the skill's own state, so a held primary whose state
  reports `Any` re-executes every single frame: the animation crossfade restarts forever on
  frame one and the damage window is never reached.
- **Voice clips bypass Unity audio entirely.** See the section above: Unity's audio engine is
  switched off in this build, so the clips go out through `winmm.dll` instead.
- **The poses come out of the .blend, not out of code.** Flowery is posed by hand and each pose
  saved as a single-frame Action; `FloweryRig/posetool.py` maps animator state names onto those
  action names and `export_fbx.py` bakes them into one FBX. Three things in that path fail
  *silently* and cost a full debugging pass each:
  - a Blender 4.4+ Action applies nothing at all unless its **slot** is bound, and a slot only
    auto-binds when its identifier matches the object name - two of these poses were authored on
    an armature called `Armature`, so they need the slot assigned by hand;
  - a pose bone left in **euler mode** ignores the quaternion curves of every action baked after
    it, and the FBX exporter then writes one stale rotation into *all* the clips at once. The rig
    is quaternion throughout and has to stay that way, so the generated punch poses convert their
    degrees rather than switching the bone's mode;
  - an object **hidden in the .blend cannot be selected** — `select_set(True)` on one returns
    quietly having done nothing — and the export is `use_selection=True`. So a cape hidden in the
    outliner to get it out of the way while posing is a cape missing from the FBX, from the
    bundle and from the game, with every log line clean, this script's own "renderers for export"
    included: that list is built before the selection. `posetool.load` now un-hides the character
    and says so, and `export_fbx.py` refuses to write a file it could not select everything for.

  `export_fbx.py` deletes the source actions before writing the FBX, or Unity would see each pose
  twice under two names.

- **The flight idle is the author's pose, breathed rather than re-posed.** `Fly` is the animator
  state Flowery is in whenever no skill is running, so it is the pose the player looks at most,
  and it used to ship as a single held frame. `posetool.py` now samples it across a
  2.4s loop (144 frames at the .blend's 60fps): the pelvis rises and sinks 11cm, the head and neck
  trail that lift, and a wave travels
  down all four cape chains three times per hover, scaled by a slow gust that also leans the whole
  cape downwind. His hair and collar ride the same ripple.

  All of it is **additive** — a third dict, `ADDS`, composing with whatever the base pose left on a
  bone about that bone's own posed axes, next to the `POSES` that replace and the `MOVES` that
  translate. The silhouette stays the author's; only its second-order motion is generated. Four
  things here are load-bearing:
  - **Every term is a sine at a whole number of cycles per loop**, and the closing key is the
    opening pose again rather than a near-miss, so the wrap is seamless to five decimal places.
  - **The hover's sine starts at its zero, not at the top.** Every other pose is authored at the
    un-hovered height and `PlayPose` restarts the idle at frame 1 whenever a skill lets go of it,
    so a cycle that begins at neutral costs nothing to enter or leave. Starting it at the peak
    would drop him 5cm at the end of every punch and every dash.
  - **The wind direction is stated once, on `coatholder`.** That is the single bone all four cape
    panels hang from, so a lean applied there swings them together; the panels themselves only get
    a zero-mean ripple, because their four local frames point four different ways and a steady
    bias applied per-chain would splay the cape open instead of leaning it.
  - **Per-segment amplitude grows toward the hem**, because the leverage shrinks the other way: a
    degree at the top of a chain swings its end 21mm, a degree at the bottom swings it 4mm. Equal
    degrees is a cape that only moves at the shoulders.

  A clip out of an FBX imports with **loop time off**, which is invisible on a held pose and fatal
  here — Flowery would hover once, 2.4 seconds after spawning, and hang frozen at the bottom of the
  cycle for the rest of the run. `FloweryLPSetup.SetLoopingClips` turns it on for `A_Fly` and
  `A_Run`, so re-running **Flowery ▸ Set Up Low-Poly Flowery** after the export is not optional
  for those two.
  **Flowery ▸ Inspect Clips** prints what each clip actually imported as — length, loop flag, and
  the widest travel any curve in it kept — which is the only way to see that Unity's keyframe
  reduction has not quietly flattened the hover.

- **The run is the idle's twin, not a second animation.** Standing still and crossing the map at a
  sprint used to look identical, because `Fly` was the whole of Flowery's locomotion. `Run` is that
  same pose leaned into the wind: the character pitched forward, the torso bent a little further
  and the head lifted back out of it so he looks downfield rather than at his own feet, the limbs
  swept back and held there, and the cape and fringe streaming. He does not
  switch to it — `Fly` is now a **blend tree** between the two, and `FloweryLocomotion` writes how
  far along it he is from his own horizontal speed over his walk-speed stat: nothing below 0.15 of
  it, everything above the sprint multiplier, smoothed over 0.14s. A walk therefore leans him about
  two thirds of the way, which is the walk pose he does not otherwise have.

  **Nothing above the pelvis moves except the head — and that is the third time this list has
  been cut.** Flowery hovers.
  Nothing about him is a gait: his legs are not carrying him, they are hanging, and his hands are
  folded against his chest. Both clips originally disagreed — the idle lagged the arms 1.5° and the
  legs 3° behind the pelvis on the theory that anything hanging off a moving body should trail it,
  and the run scissored the thigh a further 9° and the shin 5°. That theory is for a body with
  weight swinging on joints. What it produced was feet paddling 40cm over the loop and arms bobbing
  gently inside a body that was already rising and falling — two motions competing to say the same
  thing.

  The chest was the subtle one. Its drag was only 3.8°, but an arm is a rigid lever hanging off it
  and the hands are at the far end, so those 3.8° — which moved the chest 23mm — wandered the hands
  25 and 34mm. **Countering it in the arm is not the answer**, and that is worth knowing before
  reaching for it: the hands are *resting* on the chest, so measured in the chest's own frame they
  already sit dead still, and a shoulder counter-rotation that holds them still in the world instead
  slides the left hand 54mm across his own body. The chest itself had to go quiet.

  Measured with the body's own hover subtracted, both hands and both feet now wander **0.0mm** in
  both clips. They still travel 11cm, because the whole body does; they no longer travel any
  distance of their own. `RUN_SURGE`, the body bob at twice the stride, went the same way — a 3.3Hz
  bob reads as a footfall, and it was a quarter of the run's vertical travel.

  What separates the run from the idle is now entirely a shape held rather than a cycle performed:
  the lean, the sweep, and the wind. Everything still moving in either clip is either the body
  itself, the head nodding 17mm on its own account, or something light enough to be blown — cape,
  fringe, collar. That is enough: on a hovering figure the cape does the living.

  **One state rather than two is the load-bearing decision.** Flowery's controller is a single layer
  of whole-body poses where the last writer wins, and his skills run on two entity state machines
  that do not know about each other — which is why `ReleasePose` exists at all. Anything that
  crossfaded to a separate `Run` state would be a second owner of the pose, having to work out every
  frame whether a punch or a dash currently holds it. A blend inside `Fly` owns nothing: it only
  changes what `Fly` looks like, so every skill takes and returns the pose exactly as it did before
  the run existed. It costs no networking either — every client already has the velocity.

  A blend tree lines its clips up by **normalised** time, so the run is authored as the idle's twin
  rather than as its own animation. It is the same 144 frames, and `_run_beat` calls `_fly_beat` and
  adds to what comes back, so every term the idle has keeps the idle's rate and phase and blending
  can only ever scale it. Clips of different lengths would make the blended clip's length move with
  speed; a cape rippling six times per loop against one rippling three would beat against itself at
  every intermediate lean. The one new rate, the stride, sits on bones whose idle contribution at
  that rate is zero. What comes out is a blend parameter that means exactly one thing — how far over
  he is — and a clip that needs sampling twice as densely as the idle, which is the one thing here
  that does *not* have to match it.

  The lean itself cannot be an `ADD`, and that is a fourth dict: `TILTS`, a rotation about the
  **world's** fore-aft axis applied outside everything the pose did, exactly as `SPINS` is about the
  vertical. An `ADD` turns a bone about its own posed axes, and not one of the bones this is asked
  of rests with its X along the world's — the pelvis rests 13° off vertical, and a degree of local X
  sends one bang forward and the next one backward, so there is no per-bone axis a wind direction
  could be stated on. A world pitch is one instruction all of them understand: whatever sits above
  the bone swings toward his face, whatever hangs below it swings behind him, which is the body's
  lean and the hair's sweep in the same number. It is exact on the parentless pelvis and, on the
  hair and collar, exact to the extent that the chain above them deviates from rest only in pitch —
  which it does, because everything the run adds above a bang *is* a pitch, and pitches about a
  common axis commute.

- **The character-select mannequin performs an entrance, and it is two clips.** The select screen
  used to show one held frame of the author's standing pose. It now plays his introduction from
  the Chapter 5 opening, cut to what a menu can show: he is already standing when the panel opens,
  so the bow the clip opens on is dropped, and what is left is the flourish — the arm thrown
  straight up, swept down, and parked on his hair with two fingers out, where it stays.

  **Both ends are the author's:** it starts on `flowery standing` and lands on `CharSelectEnd`,
  posed by hand in the .blend, and only the travel between them is generated.

  `SelectIntro` is a one-shot, 64 frames of it, and `Select` is the 3s breath it lands in. The
  split is the whole design. **The mannequin's animator is started exactly once** — `Animator.Play`
  in `FloweryModelEnforcer.Start`, at the moment the select screen instantiates the display prefab
  — so the clip begins when Flowery is actually picked, alongside the "Selected" voice line. One
  looping clip would have been less machinery and would have had him re-introduce himself every few
  seconds for as long as the player reads his skills. Nothing in the mod watches the mannequin to
  notice the entrance finished, so the animator does it: `LinkSelectIntro` wires the controller's
  **only** transition, exit-time only, no parameter and no condition.

  Its duration is **zero**, and that is load-bearing rather than lazy. The intro's last key *is* the
  loop's first sample — `select_intro_frames` ends on `SelectHold_000` rather than on a beat of its
  own, the same trick the punches use to chain — so there is nothing between the two to blend, and
  the wrap measures 0.000mm. Break that identity on the `posetool` side and a crossfade has to go
  back in here.

  **Only his left arm travels, and the cape is why.** It hangs off `coatholder`, which is
  parented to `hand.R`, so the right hand is the one thing that cannot move without dragging 21
  cape bones up to his ear. Both of the author's poses leave that hand where standing has it, and
  in the source it *is* his left hand that goes up and his right that stays at his chest. The
  wind-up and the throw are `POSES` over standing; the arrival and its recoil are a few degrees of
  `ADDS` over `CharSelectEnd`, so the overshoot is *about* his pose rather than a second opinion on
  where the hand goes.

  **Do not solve the end pose in code.** It was, first: temple position, elbow height and palm
  direction as a cost function, iterated until every number was right — and it still was not the
  pose, which the author then posed by hand in a minute. Two things that solve found are still
  true and still used by the throw: a solve that checks only positions **cannot see the palm**
  (its facing is `hand.L`'s world +Z, calibrated with a curl test), and the palm turns with the
  **forearm's** roll, not the wrist's — rolling `hand.L` about its own Y swings the hand 395mm
  away, while the forearm's Y turns the palm and moves the wrist 0mm.

  **`CharSelectEnd` does not key the cape, and the rig could not reset it.** The 21 cape bones sit
  in the `Coat` bone collection, which the .blend has **hidden** — and a bone in a hidden
  collection cannot be selected, so `select_all` skipped them and `transforms_clear` never touched
  the cape. `_clear_pose` claimed to reset every bone and never did. Every action that leaves the
  cape unkeyed therefore wore whatever cape the *previous* pose left, which nobody could see while
  those were only the dashes, all of which hide it. `CharSelectEnd` shows it: each breath sample
  inherited the last one's ripple, so the ripple integrated instead of oscillating — the hem
  travelled 75mm instead of 31, sat 45mm off his pose on average, and the loop stopped closing.
  The reset is now done on the pose data, which has no notion of visibility, and verified against
  every pose the export bakes: across all 193 of them, **no bone outside the cape changed**, and
  the cape changed only in the dashes and OMEGA (hidden in game) and in the breath (the bug).

  With the cape now genuinely at rest when unkeyed — the rigid slab — `CharSelectEnd` needs a cape
  from somewhere. `posetool.FILL_FROM` gives it standing's, for the bones it does not key only: the
  cape the author was looking at while posing it, and the one the flourish starts with, so it does
  not jump. Key the cape in `CharSelectEnd` and the author's keys win with nothing else changed.

  The breath **translates nothing**. The flight idle lifts the pelvis 11cm and gets away with it
  because Flowery is in the air; the same move on the ground, with no IK on this rig, pushes his
  feet through the floor. What is left — a chest that rises, a head trailing it, the cape stirring
  at 30% of the idle's ripple — moves the head 20mm and the cape hem 44mm, which is enough to keep
  a portrait alive.

  `A_Select` had to join `A_Fly` and `A_Run` in `SetLoopingClips`. It is the **last** thing that
  ever plays on that mannequin, so without loop time he takes exactly one breath, three seconds
  after being picked, and is a statue from then on.

  **`Animator.Play` on a state the controller does not have does nothing, silently**, leaving the
  animator in its *default* state — which for Flowery is `Fly`. So an asset bundle built before
  this existed does not fall back to the old standing pose, it falls back to a survivor hovering
  in his own select screen. `FloweryModelEnforcer.ResolvePose` checks `HasState` first and falls
  back to `Select`, saying so in the log.

- **The pocket hand is pinned against the pelvis, not the chest.** `hand.L` rests in his pocket,
  and a pocket is on the **hip** — so the frame it has to hold still in is the pelvis's, and that
  is not the frame it hangs from. The run broke it twice over: the arm's own 13° sweep took the
  hand back, and the 17° the spine bends forward swung the whole arm back with it. The hand ended
  up **145mm** behind where the idle puts it, which is inside his backside.

  Dropping the sweep gets that to 58mm, and no single give-back at the shoulder does better — −17°
  there overshoots the other way to 70mm — because the bending chest **translated** the shoulder as
  well as turning it, and only the elbow can absorb a change in reach. So all three joints are used
  (`RUN_POCKET_SHOULDER`, `_ARM`, `_ELBOW`), solved together against the idle's hand position. They
  hold it to **0.9mm**.

  Every number in that correction is static, so it costs the clip no motion — it just puts the arm
  somewhere else. Which is the point: `Fly` is a blend tree, so accelerating from a standstill walks
  the pose through every mixture of the two clips, and each mixture is a pose in its own right that
  the hand has to survive. It is checked at the quarter points, not just the ends.

  Nothing recomputes those three, so changing `RUN_BEND`, `RUN_LEAN` or the author's Fly arm pose
  silently unpins the hand. `export_fbx` therefore re-measures it after every bake
  (`posetool.check_pocket`) and warns past 5mm — the failure is obvious once seen and perfectly
  invisible until someone looks, which is this repo's whole category of bug.

- **The cape is mounted on his wrist, and the run is where that stops being survivable.** This
  shipped broken once and is worth writing down. `coatholder` hangs off `hand.R`, so anything moving
  above it carries all four cape panels rigidly. Measured against the idle at the same phase, the
  run's pelvis lean and spine bend put **29°** into the cape and the right arm's sweep another
  **13°**, while the five bones of a chain contribute under half a degree each of their own. 42° is
  the whole difference between a garment hanging down his back and a plank sticking out sideways —
  and a plank is what it was. It is the same rigid slab the cape shows in the poses that never key
  it, arrived at from the other direction: the idle escapes it only because the author keyed the
  cape to counter the *idle's* hand, and the run moves that hand.

  The run therefore states where the cape is anchored, in two shares, because the 42° is two
  different things:
  - the **arm's** share is pure rig artifact — a cape on your shoulders does not move when you
    swing your arm — so it is given back in full, which is also what stops the cape pumping in time
    with the stride;
  - the **torso's** share is real. A back that leans forward does carry its cape, just nowhere near
    as far as a wrist would, so most of it is given back and what is left reads as the cape riding
    up as he tips into the run.

  The correction goes on **each chain's own `.001`, not on `coatholder`**, and that is the
  load-bearing half. A bone turns about its own head, and a chain's `.001` head *is* the pin at his
  shoulder — so turning it there swings the panel about the point it hangs from and moves nothing
  else. `coatholder` sits 21cm from those pins, so the same rotation applied there would drag all
  four of them 12cm across his back and unstitch the cape from his shoulders. That is the same
  limit the idle's own `CAPE_PUSH` is held down to 1.5° by, and why the idle states its wind
  direction at `coatholder` but the run states its anchor at the roots.

  It takes the hem's divergence from the idle from 48° to a steady 21°, at every phase of the loop
  rather than pumping — the residue being the lean the cape is meant to keep.

- **Flower Punches is the only animation written from scratch.** The
  author posed everything except the punches, so the two swings are built in `posetool.py` as six
  keyed poses — ready, chamber, contact, settle, hold, retract — over 37 frames, which at 60fps
  is exactly the 0.6s the skill gives a swing. The beats are placed off what `FlowerPunches`
  reads: the damage window opens at 22% and shuts at 55%, so the fist is out by frame 11 and
  stays out through 22. Each punch's last key is the *other* hand's opening pose, so holding the
  primary chains swing into swing with nothing to blend across. `PunchL` is `PunchR` mirrored, not
  a second hand-written swing. Three things here are easy to get wrong and were:
  - **The poses compose Z, then Y, then X — not Blender's `XYZ`.** An upper arm rests pointing
    straight out sideways, so its local X is the arm's azimuth and its local Z is its elevation.
    Compose X first and a punch — azimuth ~90 — drags the arm onto its own local Z, where
    elevation degenerates into a wrist twist. Every degree of elevation the old punch asked for
    was spent silently as twist, which is why it rendered as a T-pose with a lean.
  - **The torso's yaw, the shoulder's azimuth and the arm's azimuth all turn about the same
    vertical axis and add up.** Winding the torso 36° and then swinging the arm 92° puts the fist
    38° off the crosshair. `posetool._aim` takes the *world* angle wanted and subtracts what the
    torso and shoulder have already spent, so the fist lands where the hitbox is.
  - **A punch is upper body only.** Each beat is layered on top of the author's `Fly` pose, so the
    floating pelvis, trailing legs and cape stay exactly as posed. The previous punches keyed the
    whole rig from rest, which dropped him 33cm onto the floor for the length of every swing.

  The clip is authored at the base 0.6s and the generated controller drives both punch states off
  a `PunchSpeed` float, which `FlowerPunches` sets to `attackSpeedStat` — otherwise attack-speed
  items cut the swing off mid-extension. Re-run **Flowery ▸ Set Up Low-Poly Flowery** after any
  export; the parameter defaults to 1, so an older bundle just plays at normal speed.

- **Jarona has three poses, and one of them spins.** The secondary is the skill you press most, so
  it rolls between three of the author's poses every time it fires: a head-first lunge, a flying
  kick, or an upright shrug — palms out, entirely unbothered by the fifteen metres — that turns
  once on the spot on the way through. Three things carry that:
  - **The roll happens on the authority and is networked**, in the entity state's `OnSerialize`
    next to OMEGA's stored TP. Every client renders this model, so rolling it locally would have
    Flowery lunging on one screen and shrugging on another for the same dash. The voice clips get
    away with a local roll only because the owner is the only listener they ever have.
  - **The spin is baked into the clip, not driven from C#.** A dash already takes the facing away
    from `CharacterDirection` and writes the model base's rotation itself every frame, precisely
    so there is only one writer; a second one turning that same transform would be the flicker
    that lock exists to end. Turning the *pelvis*, inside the clip, sits underneath all of it and
    cannot race anything. That is `posetool.SPINS`: world up expressed in the bone's **rest**
    frame and **pre**-multiplied, which is the conjugation that puts the turn outside the pose
    rather than inside it. Composed in, it would ride the pelvis — which rests 13° off vertical
    and is posed 11° from there — and he would precess like a dropped coin instead of spinning
    like a top.
  - **Every one of the 27 frames is keyed**, because a quaternion has no notion of "the whole
    way round": keys at 0 and at 360 hold the *same* rotation, so he would not turn at all.
    Frame to frame the sweep runs 3° to 25°, and the clip is authored at the dash's own 0.44s,
    so the turn lands as the state ends and the crossfade back to `Fly` has no yaw left to undo.
    Move `JaronaDash.duration` and `posetool.JARONA_FRAMES` has to move with it.

    How *fast* he spins is `posetool.JARONA_TURNS`, and revolutions are the only dial there is:
    the clip cannot be given more time without running past the dash, so a slower spin is fewer
    turns in the same 0.433s. It has to stay a whole number — land on a half and he finishes the
    dash facing backwards, with 180° of yaw left for the crossfade to unwind.

  None of the three keys the cape chain, so all hide the cape, which `WearsCape` already does for
  anything it does not name. A DLL built before the bundle has `Jarona2` or `Jarona3` wears the
  lunge in their place — `PlayPose` takes a fallback — so the two halves can be deployed in either
  order.

- **One material per object, always.** `CharacterModel` owns the materials of every renderer in
  its `baseRendererInfos`: it rebuilds that renderer's whole material *array*, sized to one entry
  plus overlays. A renderer with several submeshes keeps only the first and the rest stop
  rendering — which is how Flowery first appeared in game with no face and no shoes while his body
  was fine, his eyes, mouth and shoes being submeshes 1-3 of the body renderer. `export_fbx.py`
  splits each mesh by material so this cannot happen, and both places that build `RendererInfo`s
  log a warning if a multi-material renderer ever shows up again.

- **Flowery's body is cloned from Loader's, but nothing of Loader's rig survives on it**
  (`FloweryRig`, `FlowerySkin`). Loader's model object is kept only as a holder, renamed
  `mdlFlowery`, because the game looks for `CharacterModel`, `HurtBoxGroup`, `ChildLocator` and
  `RagdollController` on it. At prefab time, in order:
  1. Flowery's model goes in as a child and `CharacterModel` is given his renderers.
  2. A ragdoll is built on his bones.
  3. **Hurtboxes move onto his bones.** Read out of the game, Loader's main hurtbox is a fixed
     capsule spanning body height −0.89 to 1.63, while Flowery hovers with his toes near 0 and his
     head near 2: it stopped at his chest and his head could not be hit. It becomes one capsule from
     his feet to the top of his head hung off his pelvis, and the Railgunner weak point moves to
     his head.
  4. **The body's *core* moves to the middle of him.** The core is what every shield, burn and slow
     effect is centred on. Loader's body pins it to his chest bone through
     `CharacterBody.overrideCoreTransform`, so it sat on Loader's frozen chest, around Flowery's
     thighs — which is what left Rose Buckler's sprint shield trailing behind him. Pointing it at
     Flowery's chest instead put the shield bubble visibly high in game (Loader is mostly torso;
     Flowery is mostly legs), so the override is cleared and the core is the centre of the body
     capsule above.
  5. **Every `ChildLocator` name is pointed at his matching bone**, plus a built `HeadCenter`.
  6. **Loader's rig is deleted**: skeleton, meshes, `Animator`, `ModelSkinController` and skins,
     `FootstepHandler`, fist hitboxes, `AimAnimator`, foot IK, `SprintEffectController`. First,
     every serialized reference anywhere on the prefab that points into it is fixed and logged: a
     bone the `ChildLocator` mapped becomes Flowery's matching bone, anything else is cleared. That
     sweep caught Loader's lights in `CharacterModel.baseLightInfos`, which otherwise threw from
     `UpdateLights` every frame.
  7. **He gets a default skin of his own**, built in code and found by `SkinCatalog` on its own.
     Loader's skins named Loader's renderers and switched them back on whenever they were applied.

  The holder deliberately has **no `Animator`** — Flowery's is on his model, a level down — so
  vanilla states that play Loader's animations (hurt, stun, freeze, pod exit, death) find nothing
  to play them on, instead of throwing Loader's state and parameter names at Flowery's controller.
  That is also why `BaseFloweryState.GetFloweryAnimator` looks one level down.

  **One vanilla state used that Animator for something else, and it froze him.** Every stage after
  the first starts the player in `SpawnTeleporterState`, which finds the `CharacterModel` to hide
  and print in through `GetModelAnimator().gameObject`. On Flowery that is null: it threw on its
  first tick, and the throw skipped the line that shortens the state to its brief delay, so he stood
  locked for the full 4-second fallback with no teleport effect. `FlowerySpawnState` is the same
  state reading the `CharacterModel` off the model transform, and `FloweryBody.ReplaceSpawnState`
  swaps it in. **It has to go on the Body state machine's `initialStateType`**: a body starts in that
  the moment it spawns. `CharacterBody.preferredInitialStateType` looks like the obvious field and is
  only read for a body without a drop pod, which Loader is not; setting only that changed nothing.
  `FloweryModelEnforcer` also leaves his renderers off while `invisibilityCount` is up, or its sweep
  would show him through the hidden delay.

  Still Loader's: the item display rule set. Its rules now attach to Flowery's bones through the
  `ChildLocator`, but every offset in them was authored for Loader's, so items land on the right
  bone in the wrong place. `Show Item Displays` stays off by default until he has rules of his own.

- **The character-select mannequin is built from Loader's display prefab** and converted exactly the
  same way. The select screen needs a `CharacterModel`, `ModelPanelParameters`, and above all a
  `ModelSkinController`: it applies the loadout's skin to the mannequin without checking one
  exists. Handing it the raw model left an empty slot on the select screen.

- **The logbook camera is Flowery's, not Loader's.** A survivor's logbook entry is framed entirely
  by `ModelPanelParameters` on the model object: the camera sits on `LogbookCamera` looking at
  `LogbookTarget`. Those were Loader's points, placed for Loader's height and build, and on Flowery
  they gave a close-up of his hips. `FloweryRig.FrameLogbook` now puts the target on the middle of
  him (the same feet-to-head measurement as his hurtbox) and backs the camera off to fit his whole
  height 1.25 times over, keeping Loader's viewing angle and scaling Loader's zoom range to match.
  **The field of view it fits into is measured, not read** - it lives in serialized prefab values,
  not code. ModelPanel defaults to 60°, and framing for that left him small; Loader's camera at
  0.90m showing about 1.7m of him puts the real figure near 85°. The log line
  `Rig: logbook camera ...` prints both distances, which is what to recalibrate `LogbookFov` from.

- **No MonoMod hooks.** The mod uses no `On.` or `IL.` hooks, which greatly reduces the odds of
  a game update breaking it.
- **The OMEGA bar and the OMEGA buff never disagree.** The TP at activation is networked in
  the entity state, and both the local meter's drain and the server's buff duration come from
  the same number divided by the same rate.
- **`FloweryDiagnostics`** logs, once the catalogs load, whether the body, the survivor entry,
  the skills, LAST JARONA, the melee hitbox, the borrowed VFX and the named state machines all
  came out right — registration failures in RoR2 are otherwise silent.
- **`FloweryEffects` gates every VFX the mod spawns.** `EffectManager.SpawnEffect` only accepts
  prefabs that are in the `EffectCatalog`; hand it anything else and it draws nothing and logs
  an error on every call. Plenty of borrowable vanilla VFX are not catalogued — the melee swing
  prefabs least of all, since vanilla instantiates those onto a muzzle by hand — so uncatalogued
  prefabs are instantiated locally instead of being dropped. The skill states run on every
  client, so a local copy is still seen by everyone watching.
