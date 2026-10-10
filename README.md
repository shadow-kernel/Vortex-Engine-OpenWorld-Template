<div align="center">

<img src="preview.png" alt="Vortex Open World — the oasis" width="640"/>

# Vortex Engine — Open World Template

### A desert oasis under a real sky, playable with the Tactical Shooter's weapons — for the **[Vortex Engine](https://github.com/shadow-kernel/Vortex-Engine)**.

<br/>

[![Vortex Engine](https://img.shields.io/badge/POWERED%20BY-Vortex%20Engine-6C5CE7?style=for-the-badge)](https://github.com/shadow-kernel/Vortex-Engine)
[![License](https://img.shields.io/badge/CODE-MIT-3DA639?style=for-the-badge&logo=opensourceinitiative&logoColor=white)](LICENSE)
[![Assets](https://img.shields.io/badge/ASSETS-CC--BY%20%2F%20CC0-00B894?style=for-the-badge)](ATTRIBUTIONS.md)

<br/>

**512 metres of dunes. Two turquoise lakes in a basin. Date palms along the shore, quiver trees on the gravel, a sandstone escarpment to the north — and the midday sun of a real Namaqualand sky.**

Press Play and you stand on the sand strip between the lakes with the UZI in your hands. Walk the shore under the palms, climb the dunes, look back at the oasis from the escarpment. Everything you see is engine data you can sculpt, paint and tune in the editor — or have Claude reshape from the editor chat.

</div>

---

## 🌍 The world

| Feature | What it is |
|---------|-----------|
| **Terrain** | a 512 m heightfield (`Assets/Terrain/Oasis_Terrain.vterrain`, 1 m cells): SW–NE dune ridges with slip faces, broad undulations, two flat-bottomed basins, a 20 m escarpment with a broken edge. Four splat layers — aerial sand, damp sand along the water line, sandstone on slopes, the escarpment and the gravel plains, grass on the oasis floor. Sculpt it with the Terrain tools (brush, raise / smooth / flatten / paint) |
| **Water** | two `Water` entities at −6 m: the surface follows the basin, darkens with depth, foams along the bank, reflects the HDRI's sky, ripples in the sun |
| **Vegetation** | one `Foliage` entity (`Assets/Foliage/Oasis_Vegetation.vfoliage`) with 28 types: six palm models (0.4–0.8 scale → 8–17 m), quiver trees, desert shrubs, succulents, dead wood, reeds and grass at the water, boulders, cliff rocks, stones, three rock formations on the dunes. Palms sway in the wind; trunks, boulders and cliffs collide. Paint more with the Foliage brush |
| **Sky & sun** | `Assets/Skies/goegap_2k.hdr` as the sky; the directional light points where the picture's sun is (azimuth 219°, elevation 46°); the gradient colours are the environment the water and glossy surfaces reflect |
| **Air** | warm exp² haze that thins with height, SSAO, a touch of bloom (scene settings) |
| **Player** | the Tactical Shooter rig (`Assets/Prefabs/TacticalPlayer.ventity`): animated UZI and CZ Scorpion, CoD movement (sprint, slide, mantle), HUD. The gun **RANGE** is the project's second scene |

## 🎮 Controls

| Input | Action |
|-------|--------|
| **WASD** + mouse | move + look · **Shift** sprint (double-tap **W** tactical sprint) · **Ctrl / C** crouch / slide · **Space** jump / mantle |
| **LMB** | fire · **RMB** aim · **R** reload · **B** fire mode · **1 / 2** switch weapon · **I** inspect |
| **ESC** | settings · **P** (editor) free camera |

## 🛠 Rebuilding the oasis

The scene was authored by code, not by hand: the engine's `oasis setup` authoring helper
(`Managed/Vortex.Editor/Shell/OasisSetupSmoke.cs`) sculpts the terrain from noise, places the lakes, scatters every type
by biome rules (height above the water, slope, distance to the oasis, grove noise) and sets the sky, sun and fog — through
the same services the editor's tools and the MCP server use. From the editor chat, Claude's tools cover all of it:
`create_terrain` / `sculpt_terrain` / `paint_terrain_layer`, `create_water`, `create_foliage_layer` / `add_foliage_type` /
`paint_foliage`, `set_sky` (with `align_sun` for an HDRI), `set_environment`.

Heavy photoscanned models were reduced for instancing with `tools/gltf-decimate.py` (Poly Haven's 0.1–2 M-triangle
plants and rocks to 1–40 k); leaf cut-outs were merged with `tools/polyhaven-leaf-alpha.py`. The tools live in the engine
repository.

## 📜 Credits

Code: MIT. Assets: CC0 (Poly Haven, Yughues / Nobiax) and the Tactical Shooter's CC-BY / CC0 sets — see
[ATTRIBUTIONS.md](ATTRIBUTIONS.md).
