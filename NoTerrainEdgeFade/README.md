# No Terrain Edge Fade (Hard Floors) — RimWorld 1.6

## What this does

Removes the soft alpha-blended "fade" mesh RimWorld draws over man-made
floors (concrete, paved tile, wood, stone tile, flagstone, etc.) whenever
they border a terrain with a higher `renderPrecedence` — most commonly
water, but also things like lava or moving water. This is the effect you
see as a dark/grey halo bleeding from water onto a brick or tile floor.

Natural terrain (soil, sand, gravel, mud, ice, etc.) still fades into
other natural terrain as normal — only floors with `edgeType = Hard`
(the default for built floors) are made crisp.

## Why this needs Harmony instead of XML

The relevant logic lives entirely inside compiled game code
(`Verse.SectionLayer_Terrain.Regenerate`), which decides, per cell, per
neighbor, whether to draw a fade quad based on `edgeType` and
`renderPrecedence`. Some terrains' `renderPrecedence` can be raised via
XML to "outrank" water (this is what the Steam Workshop mod "No Edge
Fade" does), but it requires re-tuning the precedence of every floor def
— including any added by other mods — and can't fix a floor's value
without potentially fighting other mods that depend on the current
ordering. This mod instead patches the actual decision logic once, so it
applies uniformly to every `Hard`-edged terrain, vanilla or modded,
without renumbering anything.

## How the patch works

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

## Building

You'll need a C# compiler targeting .NET Framework 4.8 (Visual Studio
2022 Community, or `dotnet build` with the .NET Framework targeting
pack, or JetBrains Rider). This environment had no compiler available,
so the project is provided as source only — you'll need to build it
once on your machine.

1. Open `Source/NoTerrainEdgeFade/NoTerrainEdgeFade.csproj`.
2. Edit the `<RimWorldInstallDir>` property at the top to point at your
   actual RimWorld installation folder (the one containing
   `RimWorldWin64_Data`).
3. Edit `<HarmonyAssembliesDir>` to point at wherever the Harmony mod
   (`brrainz.harmony`, aka "Harmony" on the Steam Workshop) is installed
   in your `Mods` folder, so it can find `0Harmony.dll`. If you use
   Rimworld's built-in Harmony (bundled since some 1.5+ setups), point
   it at that copy instead.
4. Build (`dotnet build -c Release`, or build from your IDE). The
   project is set up to drop the compiled `NoTerrainEdgeFade.dll`
   directly into `1.6/Assemblies/` next to this README.
5. Copy (or symlink) the whole `NoTerrainEdgeFade` folder into your
   RimWorld `Mods` folder.
6. Enable "Harmony" and "No Terrain Edge Fade (Hard Floors)" in the mod
   list, with Harmony loaded first (the `About.xml` already declares
   this load order).

## If the game updates and this stops working

The transpiler logs an error to the RimWorld dev log (`Log.Error`,
visible with dev mode + log window enabled) if it can't find the IL
pattern it expects, rather than silently corrupting the method or
crashing — in that case it just leaves `Regenerate` unpatched and
terrain fading behaves like vanilla. If Ludeon changes
`SectionLayer_Terrain.Regenerate` in a future version, the fix is to
re-locate the renderPrecedence comparison in the updated method (the
comments in `Patch_SectionLayerTerrain.cs` document exactly what the
original game IL looks like at the time of writing).

## Compatibility

- Safe to add or remove mid-save.
- Should be compatible with mods that add new terrain types — they
  automatically get the fix as long as they leave `edgeType` at its
  default (`Hard`) for floors, which is the vanilla convention.
- Not expected to conflict with "No Edge Fade" / "No Edge Fade - Lite",
  but running both is redundant since this mod's fix is a superset for
  the Hard-floor case those mods target via renderPrecedence/edgeType
  XML edits.
