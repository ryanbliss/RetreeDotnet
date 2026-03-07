# Retree Unity Sample Project

A minimal Unity 6 project that references the `com.ryanbliss.retree` package locally.

## Setup

1. Open this project in Unity 6000.0.40f1.
2. The `com.ryanbliss.retree` package is referenced via a local path in `Packages/manifest.json`.
3. In the Package Manager window, find **Retree** and import the **Todo App** sample.
4. Open the imported sample scene or create a new scene and add a GameObject with the `TodoAppExample` component.
5. Enter Play mode to see Retree change events logged to the Console.

## Building the Retree DLL

If the `Retree.dll` in the package is out of date, rebuild it from the repository root:

```bash
dotnet build src/Retree/Retree.csproj -c Release
cp src/Retree/bin/Release/netstandard2.1/Retree.dll src/RetreeUnity/Runtime/Plugins/Retree.dll
```
