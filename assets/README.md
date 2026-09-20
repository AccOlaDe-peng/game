# Assets

Art and audio used by Project Catalyst. All third-party assets are free to
use; see `art/licenses/` for the exact license text of each pack.

## Kenney.nl packs (CC0)

Free assets by [Kenney Vleugels](https://kenney.nl/) (CC0 — free for
commercial use, no attribution required). Licenses copied to
`art/licenses/kenney/`.

| Location                 | Pack            | Contents                                   |
|--------------------------|-----------------|--------------------------------------------|
| `art/particles/`         | Particle Pack   | 80 PNG particle textures (fire, smoke, sparks, glows, circles) for spell/VFX |
| `art/icons/black/`       | Game Icons      | 105 black icons (1x) for spells/upgrades/cards |
| `art/icons/white/`       | Game Icons      | 105 white icons (1x), better on dark UI     |
| `audio/sfx/`             | Impact Sounds   | 130 impact/footstep/explosion OGG sounds    |
| `audio/ui/`              | Interface Sounds| 100 UI click/confirm/error OGG sounds       |
| `audio/rpg/`             | RPG Audio       | 51 RPG object/interaction OGG sounds        |
| `art/environment/nature/rocks/` | Nature Kit | 13 rock models (GLB) for Arena dressing   |

Particle textures are designed to be used with Godot's `GPUParticles2D/3D` or
`CPUParticles`; the transparent PNGs work with normal alpha blending.

## Quaternius (CC0)

3D characters, enemies, props and environments under `art/characters`,
`art/enemies`, `art/environment`. See `art/licenses/` for licenses.

| Location                        | Pack            | Contents                                        |
|---------------------------------|-----------------|-------------------------------------------------|
| `art/enemies/monsters/big/`     | Ultimate Monsters | 16 large monster models (GLTF)                 |
| `art/enemies/monsters/blob/`    | Ultimate Monsters | 17 blob-style monster models (GLTF)            |
| `art/enemies/monsters/flying/`  | Ultimate Monsters | 17 flying monster models (GLTF)                |
| `art/environment/ruins/`        | Ultimate Modular Ruins | 42 FBX ruins pieces (columns/walls/arches/floors/statues/props) + 2 textures |

The Ultimate Monsters pack (CC0) provides 50 total monster models across the
three styles for enemy variety.

The Ultimate Modular Ruins subset covers the "Arcane Research Ruins" Arena
needs from the art plan (§18): ruins / pillars / walls / vegetation / ground
pieces. Rocks come from the Kenney Nature Kit above. Crystals and machines
are still open — build via Blender or shader/VFX per plan.

## Fonts

| Location                        | Font            | License | Contents                              |
|---------------------------------|-----------------|---------|---------------------------------------|
| `art/ui/fonts/LXGWWenKai-Regular.ttf` | LXGW WenKai (霞鹜文楷) v1.522 | OFL-1.1 | 46,490 glyphs incl. 20,992 CJK — covers all Chinese UI text |
| `art/ui/fonts/who_asks_satan.ttf`     | Who Asks Satan | dafont license | Latin display font (no CJK glyphs) |

LXGW WenKai is required because UI text is Chinese; use it (or a font
fallback chain containing it) for any Control rendering Chinese strings.

## Music

| Location                        | Track           | Author         | License |
|---------------------------------|-----------------|----------------|---------|
| `audio/music/horde_war_drums.wav` | Horde War Drums Loop | William Hector | CC0 ([OpenGameArt](https://opengameart.org/content/horde-war-drums-loop)) |

## Mixamo animations (Adobe)

Free motion-capture animations under `art/animations/mixamo/`. See
`art/animations/mixamo/README.md`.
