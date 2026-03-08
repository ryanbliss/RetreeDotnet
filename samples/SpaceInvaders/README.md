# RetreeCore Unity Sample — Space Invaders

A Space Invaders game built with Unity 6 and the `com.ryanbliss.retreecore` package, demonstrating reactive state management with RetreeCore.

## Setup

1. Open this project in Unity 6000.0.40f1.
2. The `com.ryanbliss.retreecore` package is referenced via a local path in `Packages/manifest.json`.
3. Open the `Assets/Scenes/SpaceInvaders.unity` scene.
4. Enter Play mode and press **Space** to start.

## Building the RetreeCore DLL

If the `RetreeCore.dll` in the package is out of date, rebuild it from the repository root:

```bash
dotnet build src/RetreeCore/RetreeCore.csproj -c Release
cp src/RetreeCore/bin/Release/netstandard2.1/RetreeCore.dll src/RetreeUnity/Plugins/RetreeCore.dll
```
