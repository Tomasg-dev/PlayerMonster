#!/usr/bin/env bash
# Compila el mod y arma el zip para Thunderstore en dist/.
# Uso:  scripts/package.sh
set -euo pipefail

raiz="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$raiz"

version="$(grep -oP '"version_number":\s*"\K[^"]+' thunderstore/manifest.json)"
nombre="$(grep -oP '"name":\s*"\K[^"]+' thunderstore/manifest.json)"

# La versión del manifiesto y la del código tienen que coincidir.
if ! grep -q "public const string Version = \"$version\"" src/PlayerMonster/Plugin.cs; then
  echo "La versión de thunderstore/manifest.json ($version) no coincide con la de Plugin.cs" >&2
  exit 1
fi

dotnet build src/PlayerMonster -c Release

temporal="$(mktemp -d)"
trap 'rm -rf "$temporal"' EXIT
cp thunderstore/manifest.json thunderstore/icon.png thunderstore/README.md thunderstore/CHANGELOG.md "$temporal/"
cp src/PlayerMonster/bin/Release/netstandard2.1/PlayerMonster.dll "$temporal/"

mkdir -p dist
salida="dist/$nombre-$version.zip"
rm -f "$salida"
(cd "$temporal" && zip -q -r "$raiz/$salida" .)
echo "Paquete creado: $salida"
