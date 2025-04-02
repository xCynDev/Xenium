# Xenium

A tool that generates the initialization scripts for a Garry's Mod addon/gamemode, using a module-based approach.

## Features

Xenium was built out of the necessity to replace my module loader, which provided the same features, except it broke [auto-refresh](https://gmodwiki.com/Auto_Refresh).

As such, I have built a tool that would keep my benefits of using a module loader, but also allow for auto-refreshing of scripts.

- Module-based approach to addon and gamemode development.
- Configuration on a project and per-module basis.
  - Configurable load order of modules in the project configuration.
  - Configurable load order of files and folders in a module, in the module's configuration.
- Automatically generates `AddCSLuaFile()` and `include()` statements based on the file's prefixes.
  - These are configurable if you wish to add custom prefixes. The defaults are `sv_`, `sh_` and `cl_` for `server`, `shared` and `client` files respectively.
- Writes them to the init scripts for your addon and gamemode.
  - `lua/autorun/client` and `lua/autorun/server` for addons.
  - `gamemode/cl_init.lua` and `gamemode/init.lua` for gamemodes.
- CI friendly, can be used as a build step when deploying your addons & gamemodes to servers.
- Tiny! Using .NET's Native AOT, the binary is self-contained and does not require a .NET runtime to function.

## Download

Head over to the latest [releases](https://github.com/xCynDev/Xenium/releases) and download the Xenium binaries for your OS.
Releases include Windows & Linux. Head over to the [Building](https://github.com/xCynDev/Xenium?tab=readme-ov-file#building)
section of the README for instructions on how to build for different operating systems.

## Documentation

Check the [wiki](https://github.com/xCynDev/Xenium/wiki) out to learn how to use this tool.

## Building

Install the [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) for your platform of choice.

Then run the following in the root directory of the repository.
```sh
# Build for Windows
dotnet publish -c Release --os win

# Build for Linux
dotnet publish -c Release --os linux

# Build for MacOS
dotnet publish -c Release --os osx

```

Xenium has been tested on Windows & Linux. MacOS should work, but hasn't been tested. You can find .NET runtime identifiers (RIDs) [here](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog).