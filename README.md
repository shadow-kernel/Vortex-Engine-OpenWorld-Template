<div align="center">

<img src="preview.png" alt="Vortex Open World — the oasis" width="640"/>

# Vortex Engine — Open World Template

### A desert oasis under a real sky to walk, swim and explore — with the Tactical Shooter's gun range next door — for the **[Vortex Engine](https://github.com/shadow-kernel/Vortex-Engine)**.

<br/>

[![Vortex Engine](https://img.shields.io/badge/POWERED%20BY-Vortex%20Engine-6C5CE7?style=for-the-badge)](https://github.com/shadow-kernel/Vortex-Engine)
[![License](https://img.shields.io/badge/CODE-MIT-3DA639?style=for-the-badge&logo=opensourceinitiative&logoColor=white)](LICENSE)
[![Assets](https://img.shields.io/badge/ASSETS-CC--BY%20%2F%20CC0-00B894?style=for-the-badge)](ATTRIBUTIONS.md)

<br/>

**512 metres of dunes. Two turquoise lakes in a basin. Date palms along the shore, quiver trees on the gravel, a sandstone escarpment to the north — and the midday sun of a real Namaqualand sky.**

Press Play and you stand at the edge of the palm grove. Walk the shore under the palms, swim across the lakes, climb the dunes, look back at the oasis from the escarpment. The guns are in the **Range**, the project's second scene. Everything you see is engine data you can sculpt, paint and tune in the editor — or have Claude reshape from the editor chat.

</div>

---

## 🌍 The world

| Feature | What it is |
|---------|-----------|
| **Terrain** | a 512 m heightfield (`Assets/Terrain/Oasis_Terrain.vterrain`, 1 m cells): SW–NE dune ridges with slip faces, broad undulations, two flat-bottomed basins, a 20 m escarpment with a broken edge. Four splat layers — aerial sand, damp sand along the water line, sandstone on slopes, the escarpment and the gravel plains, grass on the oasis floor. Sculpt it with the Terrain tools in the Inspector (brush, raise / smooth / flatten / paint), import or export it as a 16-bit heightmap, or regenerate it from the noise presets; double-click the `.vterrain` in the Asset Browser to get there |
| **Cave** | a 48 m cave at the foot of the escarpment south of the oasis, its rock arch facing the water (`Assets/Models/Caves/cave_tunnel_a`, made with the engine's `tools/cave-gen.py`): ferns, grass and a curtain of lianas at the mouth, a high chamber where a shaft of sunlight falls through the ceiling onto a fern garden (a spot light, drifting dust and a warm haze), a campfire at the far end (`Assets/VFX/Campfire.vfx` and a flickering light, `FireFlicker.cs`) and faint glowing moss in between. It is dark inside: the world lighting cache treats the cave as a space you can walk into. The terrain has holes at the mouth and the shaft, the model carries a Mesh Collider, and three **Areas** linked to `Assets/Areas/Cave.varea` (tags cave, no-fly, dark; `flightAllowed=false`) tell scripts when someone is inside — `ZoneManager.cs` logs it |
| **Water** | two `Water` entities at −6 m: the surface follows the basin, darkens with depth, foams along the bank, reflects the HDRI's sky, ripples in the sun |
| **Vegetation** | one `Foliage` entity (`Assets/Foliage/Oasis_Vegetation.vfoliage`) with 28 types: six palm models (0.4–0.8 scale → 8–17 m), quiver trees, desert shrubs, succulents, dead wood, reeds and grass at the water, boulders, cliff rocks, stones, three rock formations on the dunes. Palms sway in the wind; trunks, boulders and cliffs collide. Paint more with the Foliage brush in the Inspector (double-click the `.vfoliage` in the Asset Browser to select the layer) |
| **Sky & sun** | `Assets/Skies/goegap_2k.hdr` as the sky; the directional light points where the picture's sun is (azimuth 219°, elevation 46°); the gradient colours are the environment the water and glossy surfaces reflect |
| **Air** | warm exp² haze that thins with height, SSAO, a touch of bloom (scene settings) |
| **Player** | the Tactical Shooter's movement (`CoDMovement.cs`: sprint, slide, mantle) and **swimming** — chest-deep water carries you, **Space** swims up, **Ctrl** dives — with footprints in the sand and footsteps. No weapons and no HUD here: the oasis is for walking. The armed rig — the animated UZI and CZ Scorpion, the HUD, health and med-kits, four combat bots holding the yard — is in the gun **RANGE**, the project's second scene; `Assets/Prefabs/TacticalPlayer.ventity` is its player (movement and health) as a prefab |
| **Weather** | `Assets/Scripts/World/Weather.cs` blends four presets over twelve seconds — **clear**, **haze**, **sandstorm** (brown sky, 80 m visibility, sand sheets racing past, a storm in the palms) and **dusk** (the sunset HDRI, a low orange sun): fog and height fog, the sun, the sky, the ambient light, the wind, the dust emitters under the player and the wind you hear. **O** next preset, **L** previous; `Set("sandstorm")` from any script |
| **The infected** | `Assets/Prefabs/Zombie.ventity` — operators who did not make it out: a hunched shamble between random points, a flat-out sprint once they see you (80 m, 160° view, they hear a shot from 70 m), a lunge and bite within reach, your bullets drop them into a ragdoll. `Assets/AI/Zombie.vbt` decides, `Assets/Scripts/AI/Zombie.cs` is the body. None roam the oasis today — the scene went without them; the prefab, the tree and the scripts stay: place `Zombie.ventity` anywhere, or put `ZombieDirector.cs` on an entity — it keeps up to ten on the navmesh 40–130 m around you and tops them up, **Z** calls a wave of four. A bite lands on the player's `PlayerHealth` (the Range's player has one; the oasis player has none, so add it there first) |

## 🎮 Controls

| Input | Action |
|-------|--------|
| **WASD** + mouse | move + look · **Shift** sprint (double-tap **W** tactical sprint) · **Ctrl / C** crouch / slide · **Space** jump / mantle · in deep water **Space** swims up, **Ctrl** dives |
| **LMB** (Range) | fire · **RMB** aim · **R** reload · **B** fire mode · **1 / 2** switch weapon · **I** inspect · **H** med-kit · **F1** the feature tour's keys |
| **O / L** (oasis) | next / previous weather · **Z** a wave of infected (with a `ZombieDirector` in the scene) |
| **ESC** | settings · **P** (editor) free camera |

## 🛠 Rebuilding the oasis

The scene was authored by code, not by hand: the engine's `oasis setup` authoring helper
(`Managed/Vortex.Editor/Shell/OasisSetupSmoke.cs`) sculpts the terrain from noise, places the lakes, scatters every type
by biome rules (height above the water, slope, distance to the oasis, grove noise) and sets the sky, sun and fog — through
the same services the editor's tools and the MCP server use. From the editor chat, Claude's tools cover all of it:
`create_terrain` / `sculpt_terrain` / `paint_terrain_layer`, `create_water`, `create_foliage_layer` / `add_foliage_type` /
`paint_foliage`, `set_sky` (with `align_sun` for an HDRI), `set_environment`, `set_weather`; `place_asset` puts a `Zombie.ventity` wherever you point.

Heavy photoscanned models were reduced for instancing with `tools/gltf-decimate.py` (Poly Haven's 0.1–2 M-triangle
plants and rocks to 1–40 k); leaf cut-outs were merged with `tools/polyhaven-leaf-alpha.py`. The tools live in the engine
repository.

## 📜 Credits

Code: MIT. Assets: CC0 (Poly Haven, Yughues / Nobiax) and the Tactical Shooter's CC-BY / CC0 sets — see
[ATTRIBUTIONS.md](ATTRIBUTIONS.md).
