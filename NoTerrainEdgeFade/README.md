# No Terrain Edge Fade (Hard Floors) — RimWorld 1.6

## What it does

Removes the soft alpha-blended "fade" mesh RimWorld draws over man-made
floors (concrete, paved tile, wood, stone tile, flagstone, etc.) whenever
they border a terrain with a higher `renderPrecedence` — most commonly
water, but also things like lava or moving water. This is the effect you
see as a dark/grey halo bleeding from water onto a brick or tile floor.

Natural terrain (soil, sand, gravel, mud, ice, etc.) still fades into
other natural terrain as normal — only floors with `edgeType = Hard`
(the default for built floors) are made crisp.

## How it works

A Harmony **transpiler** patches `SectionLayer_Terrain.Regenerate`. It
finds the IL sequence that compares the neighbor cell's
`TerrainDef.renderPrecedence` against the current cell's
`TerrainDef.renderPrecedence`, and inserts one extra check immediately
before it: if the *current* cell's terrain has
`edgeType == TerrainEdgeType.Hard` (the enum's default/zero value), skip
drawing the fade entirely, regardless of precedence. Everything else
about the method — natural terrain blending, foundations, bridges,
pollution overlays — is untouched.

See `Source/NoTerrainEdgeFade/Patch_SectionLayerTerrain.cs` for the full
implementation and comments explaining the exact IL pattern matched.

## Compatibility

- Safe to add or remove mid-save.
- Should be compatible with mods that add new terrain types — they
  automatically get the fix as long as they leave `edgeType` at its
  default (`Hard`) for floors, which is the vanilla convention.
- Not expected to conflict with "No Edge Fade" / "No Edge Fade - Lite",
  but running both is redundant since this mod's fix is a superset for
  the Hard-floor case those mods target via renderPrecedence/edgeType
  XML edits.
