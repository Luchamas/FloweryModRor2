#!/usr/bin/env bash
# Builds the mod and assembles a Thunderstore-ready zip in Build/dist.
#
#   bash Build/package.sh
#
# The zip layout is what Thunderstore expects: manifest.json, icon.png and README.md at the
# root, plugin files under plugins/FloweryMod/.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BUILD="$ROOT/Build"
STAGE="$BUILD/stage"
DIST="$BUILD/dist"

VERSION="$(python - "$BUILD/manifest.json" <<'PY'
import json, sys
print(json.load(open(sys.argv[1], encoding="utf-8"))["version_number"])
PY
)"

echo "==> building FloweryMod $VERSION"
dotnet build "$ROOT/FloweryMod/FloweryMod.csproj" -c Release -p:DeployOnBuild=false --nologo -v q

DLL="$ROOT/FloweryMod/bin/Release/FloweryMod.dll"
[ -f "$DLL" ] || { echo "!! $DLL not found"; exit 1; }

echo "==> staging"
rm -rf "$STAGE"
mkdir -p "$STAGE/plugins/FloweryMod"
cp "$DLL" "$STAGE/plugins/FloweryMod/"
cp "$BUILD/manifest.json" "$STAGE/"
cp "$BUILD/icon.png" "$STAGE/"
# The player-facing page, not the repository README.
cp "$BUILD/README.md" "$STAGE/"

# Voice clips and images ship as loose files the plugin reads at startup.
SOUNDS="$ROOT/FloweryMod/Assets/Sounds"
if [ -d "$SOUNDS" ]; then
  mkdir -p "$STAGE/plugins/FloweryMod/Assets/Sounds"
  cp -r "$SOUNDS/." "$STAGE/plugins/FloweryMod/Assets/Sounds/"
  echo "    included $(find "$SOUNDS" -name '*.wav' | wc -l) voice clips"
fi

for folder in Icons Hud; do
  SRC="$ROOT/FloweryMod/Assets/$folder"
  if [ -d "$SRC" ]; then
    mkdir -p "$STAGE/plugins/FloweryMod/Assets/$folder"
    cp -r "$SRC/." "$STAGE/plugins/FloweryMod/Assets/$folder/"
    echo "    included $(find "$SRC" -type f | wc -l) file(s) from $folder"
  fi
done

# The model and its animations. Without it Flowery is just Loader, so there is nothing to ship.
BUNDLE="$ROOT/HenryUnityProject/AssetBundles/flowery"
[ -f "$BUNDLE" ] || { echo "!! $BUNDLE not found - run Flowery > Set Up Low-Poly Flowery first"; exit 1; }
mkdir -p "$STAGE/plugins/FloweryMod/Assets"
cp "$BUNDLE" "$STAGE/plugins/FloweryMod/Assets/"
echo "    included asset bundle"

mkdir -p "$DIST"
ZIP="$DIST/FloweryMod-$VERSION.zip"
rm -f "$ZIP"

# Built with Python rather than zip(1): Git Bash on Windows ships no zip, and PowerShell's
# Compress-Archive writes backslash separators, which Thunderstore rejects.
python - "$STAGE" "$ZIP" <<'PY'
import os, sys, zipfile

stage, out = sys.argv[1], sys.argv[2]
with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as zf:
    for root, _, files in os.walk(stage):
        for name in files:
            full = os.path.join(root, name)
            arc = os.path.relpath(full, stage).replace(os.sep, "/")
            zf.write(full, arc)
PY

echo "==> $ZIP"
