# Xenium

A tool that generates the initialization scripts for a Garry's Mod addon/gamemode, using a module-based approach.

## Features

Xenium was built out of the necessity to replace my module loader, which provided the same features, except it broke [auto-refresh](https://gmodwiki.com/Auto_Refresh).
As such, I have built a tool that would keep my benefits of using a module loader, but also allow for auto-refreshing of scripts.

- Module-based approach to addon and gamemode development.
- Configuration on a project and per-module basis.
  - Configurable load order of modules in the project configuration.
  - Configurable load order of files and folders in a module, in the module's configuration.
- Generates the init scripts for your addon and gamemode.
  - `lua/autorun/client` and `lua/autorun/server` for addons.
  - `gamemode/cl_init.lua` and `gamemode/init.lua` for gamemodes.
- CI friendly, can be used as a build step when deploying your addons & gamemodes to servers.

## Documentation

WIP
