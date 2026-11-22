# ThirdParty Source Assets

This directory stores the original downloaded packages and the files selected
for the current project. It is the audit trail for third-party art; it is not a
Unity import folder.

## Layout

```text
ThirdParty/
├─ ASSET_MANIFEST.csv
├─ Kenney/
│  └─ FantasyTownKit/
│     ├─ original.zip
│     ├─ LICENSE.txt
│     ├─ SOURCE.md
│     ├─ SHA256.txt
│     ├─ Models/
│     └─ Textures/
└─ PolyHaven/
   ├─ SOURCE.md
   ├─ grey_plaster_02/
   ├─ old_planks_02/
   └─ clay_roof_tiles_02/
```

Keep the original archive when it is small enough to version with Git LFS.
Keep the provider's license with the package even when the license is CC0.
Only the selected files are copied into `UnityProject/Assets/ThirdParty/`.

## Adding A Package

1. Confirm commercial use, modification, and redistribution on the provider's
   asset page and in the downloaded license.
2. Add the original archive, license, `SOURCE.md`, and checksum.
3. Add one row per runtime asset to `ASSET_MANIFEST.csv`.
4. Copy only the required files into Unity.
5. Run `../tools/validation/validate-art-assets.ps1`.
6. Open the Unity preview scene and confirm scale, orientation, materials, and
   UI use before replacing greybox content.

