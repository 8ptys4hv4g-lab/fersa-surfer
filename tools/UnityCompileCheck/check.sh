#!/usr/bin/env bash
# Kompiliert die Unity-Skripte (Runtime + Editor) ohne Unity – gegen öffentliche Referenz-Bibliotheken von NuGet.
# Findet Tipp- und API-Fehler, bevor jemand Unity öffnen muss. Ersetzt keinen echten Test in Unity.
# Hinweis: Die Referenzen sind Unity 2021. Fehler zu neueren APIs (NamedBuildTarget, insecureHttpOption) sind erwartet.
set -euo pipefail
cd "$(dirname "$0")"
REF=.ref
mkdir -p $REF
if [ ! -d $REF/mods ]; then
  curl -sL -o $REF/mods.nupkg https://api.nuget.org/v3-flatcontainer/unityengine.modules/2021.3.33/unityengine.modules.2021.3.33.nupkg
  (mkdir -p $REF/mods && cd $REF/mods && unzip -q -o ../mods.nupkg)
fi
if [ ! -f $REF/sdk/lib/UnityEditor.dll ]; then
  curl -sL -o $REF/sdk.nupkg https://api.nuget.org/v3-flatcontainer/unity3d.sdk/2021.1.14.1/unity3d.sdk.2021.1.14.1.nupkg
  (mkdir -p $REF/sdk && cd $REF/sdk && unzip -q -o ../sdk.nupkg)
fi
MODS=$(dirname "$(find $REF/mods -name UnityEngine.CoreModule.dll | head -1)")
{
  echo '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>netstandard2.1</TargetFramework><LangVersion>9.0</LangVersion><EnableDefaultCompileItems>false</EnableDefaultCompileItems><NoWarn>CS0649;CS0414;CS0169</NoWarn></PropertyGroup><ItemGroup>'
  echo '<Compile Include="../../Assets/_WorldRush/Scripts/Runtime/**/*.cs" /><Compile Include="../../Assets/_WorldRush/Scripts/Editor/**/*.cs" /><Compile Include="UrpStubs.cs" />'
  echo '<ProjectReference Include="../CoreLib/CoreLib.csproj" /></ItemGroup><ItemGroup>'
  for f in $MODS/UnityEngine*.dll; do echo "<Reference Include=\"$(basename "$f" .dll)\"><HintPath>$f</HintPath></Reference>"; done
  echo "<Reference Include=\"UnityEditor\"><HintPath>$REF/sdk/lib/UnityEditor.dll</HintPath></Reference>"
  echo '</ItemGroup></Project>'
} > UnityCompileCheck.csproj
dotnet build -nologo -v q 2>&1 | grep -E "error|Error\(s\)" | grep -vE "NamedBuildTarget|insecureHttpOption|InsecureHttpOption" | sed 's#.*/Scripts/##' | sort -u || true
