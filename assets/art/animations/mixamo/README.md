# Mixamo Animations

Free motion-capture animations downloaded from [Mixamo](https://www.mixamo.com/)
(Adobe), for use with Project Catalyst. Mixamo assets are free to use in
commercial and non-commercial projects per Adobe's terms of service; no
attribution is required, but keeping this note is good practice.

## Animations

| File            | Mixamo name                          | Usage                                  |
|-----------------|--------------------------------------|----------------------------------------|
| `idle_magic`    | Standing Idle 03 (Playing With Magic) | Casting-ready idle                     |
| `run`           | Run (forward)                        | Locomotion                             |
| `walk`          | Walking (careful)                    | Locomotion                             |
| `cast_2h`       | Standing 2H Magic Attack 01          | Spell cast (two-handed forward)        |
| `cast_1h`       | Standing 1H Magic Attack 03          | Spell cast (one-handed upward sweep)   |
| `cast_area`     | Standing 2H Magic Area Attack 01     | Spell cast (AoE to ground)             |
| `melee_jab`     | Punching (jab)                       | Melee attack                           |
| `melee_combo`   | Punch Combo (four punches)           | Melee combo                            |
| `hurt`          | Hit Reaction                         | Damage reaction                        |
| `death`         | Dying                                | Defeat                                 |
| `dodge_roll`    | Dive Roll                            | Dodge / roll                           |
| `jump`          | Jumping Up                           | Jump                                   |

All animations were exported as FBX (30 fps, no keyframe reduction, without
skin) on a standard Mixamo humanoid (`mixamorig`) skeleton with 68 bones.

## Import notes

- Files are tracked with Git LFS (`*.fbx` filter in `.gitattributes`).
- Godot 4.7 imports each `.fbx` as a `PackedScene` containing a
  `Skeleton3D` (bones prefixed `mixamorig_`) and one `AnimationPlayer`
  animation named `mixamo_com`.
- The Quaternius player model (`UAL1_Standard.glb`) uses its own skeleton
  (`root` / `pelvis` / `upperarm_l` …), so these Mixamo animations need
  **animation retargeting** before they can play on it. In the Godot editor:
  1. Open the FBX in the Import dock → Advanced settings.
  2. Under "Retarget", assign a `SkeletonProfile` (e.g. humanoid) and a
     `BoneMap`, then re-import.
  3. Do the same mapping on the character so both skeletons share bone names
     and bone rests, then the animations can play on the character.

See `docs/` for game design details. Re-download/refresh with the mixamo
download script if needed (requires an Adobe account).
