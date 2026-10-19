#!/usr/bin/env bash
# Builds the mod project and prints each distinct compiler error once (developer convenience).
cd "$(dirname "$0")/.."
powershell -NoProfile -Command ". .\tools\env.ps1; dotnet build src\DriveTheCityII.Mod\DriveTheCityII.Mod.csproj -c Release -nologo -v:minimal 2>&1" \
  | grep -E "error|Build succeeded" | sed -E 's#\[C:.*$##' | sed -E 's#^.*DriveTheCityII.Mod.##' | sort -u | head -${1:-60}
