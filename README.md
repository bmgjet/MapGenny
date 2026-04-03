# MapGenny – Rust Map Generation System

![Rust](https://img.shields.io/badge/Game-Rust-FF6B35?style=for-the-badge)<br>
![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux-2ED573?style=for-the-badge)<br>
A server-side map generation system for [Rust](https://store.steampowered.com/app/252490/Rust/) that runs as a Harmony mod with a web-based interface for generating, processing, and customizing maps.

![Screenshot](https://raw.githubusercontent.com/bmgjet/MapGenny/refs/heads/main/screenshot.jpg)

## Features

- **Web-Based Interface** – Intuitive UI for map generation and configuration
- **Custom Prefabs** – Import and integrate custom prefab templates
- **Heightmap Support** – Generate terrain from custom heightmaps
- **3D Preview** – Visual preview of generated maps
- **Prefab Breaker** – Advanced prefab manipulation tools
- **PNG to Cubes** – Convert images to prefab cubes
- **Batch Processing** – Queue multiple generation jobs via zip uploads

## Requirements

### Supported Platforms
| Operating System | Version |
|-----------------|---------|
| Windows | 10 / 11 |
| Ubuntu | 22.04 |
| Debian | 12 |

### Hardware Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| CPU | 2 cores | 4+ cores |
| RAM | 4 GB | 6 GB+ |
| Storage | 20 GB SSD | 50 GB SSD |

### Prerequisites
- Vanilla Rust Dedicated Server

## Demo Videos

| Installer | Prefab Breaker | PNG2Cube |
|:---------:|:--------------:|:--------:|
| [![Installer](https://img.youtube.com/vi/txoHktoal34/mqdefault.jpg)](https://www.youtube.com/watch?v=txoHktoal34) | [![Prefab Breaker](https://img.youtube.com/vi/o9HUak9pUcg/mqdefault.jpg)](https://www.youtube.com/watch?v=o9HUak9pUcg) | [![PNG2Cube](https://img.youtube.com/vi/4E5gPP9nND8/mqdefault.jpg)](https://www.youtube.com/watch?v=4E5gPP9nND8) |
| Custom Prefabs Static Map | Custom Prefabs Monuments | Jobs Mode |
| [![Static Map](https://img.youtube.com/vi/UKi5zy4JmnM/mqdefault.jpg)](https://www.youtube.com/watch?v=UKi5zy4JmnM) | [![Custom Monuments](https://img.youtube.com/vi/B_oPaRiBStE/mqdefault.jpg)](https://www.youtube.com/watch?v=B_oPaRiBStE) | [![Jobs](https://img.youtube.com/vi/-J2WMoGUE8s/mqdefault.jpg)](https://www.youtube.com/watch?v=-J2WMoGUE8s) |


## Installation

### Quick Start (Recommended)

1. Download the installer scripts from the [latest release](https://github.com/bmgjet/MapGenny/releases/tag/Installer)
2. Run the installer for your system:
   - `.bat` for Windows
   - `.sh` for Linux
3. Start the server using the run script for your platform

### Manual Installation

1. Download `MapGenny.dll` from [bin/Release](https://github.com/bmgjet/MapGenny/raw/refs/heads/main/bin/Release/MapGenny.dll)

2. Place `MapGenny.dll` in your server's `HarmonyMods` folder

3. Start your Rust Dedicated Server

### Linux Setup

Install required GDI libraries:

```bash
sudo apt install -y libgdiplus libc6-dev
```

Set environment variables before launching:

```bash
export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:$(pwd)
export SYSTEM_DRAWING_ALLOW_SYSTEM_GDI_UNIX=1
```

## Configuration

### Server Parameters

| Parameter | Description | Required |
|-----------|-------------|----------|
| `+server.ip` | Server bind address | Yes (remote) |
| `+server.port` | Server port | Yes |
| `+rcon.password` | RCON password | **Yes** (security) |

### Startup Examples

**Windows** (`start.bat`):
```batch
RustDedicated.exe ^
  +server.ip * ^
  +server.port 28016 ^
  +rcon.password "YourPassWordHere" ^
  -logfile "logs.log"
```

**Linux** (`start.sh`):
```bash
#!/bin/sh
export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:$(pwd)
export SYSTEM_DRAWING_ALLOW_SYSTEM_GDI_UNIX=1
cd "$(dirname "$0")"
./RustDedicated \
  +server.ip 0.0.0.0/+ \
  +server.port 28016 \
  +rcon.password "YourPassWordHere"
```

> **Security Note:** Always set an RCON password. Without it, anyone can shut down your server.

### Optional Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `+allowuploads` | `true` | Enable/disable file uploads |
| `+allowjobs` | `true` | Enable/disable job queue |
| `+allowcubes` | `true` | Enable/disable png2cubes |
| `+cubesonly` | `false` | Run in cubes-only mode |
| `+allow3d` | `true` | Enable/disable 3D preview |
| `+allowbreakprefab` | `true` | Enable/disable prefab breaker |

### Network Configuration

- **Local Mode:** If `+server.ip` is not specified, MapGenny auto-opens the web interface and enables all functions.
- **Remote Mode:** If `+server.ip and +server.port` are specified, MapGenny will run in remote mode. Some features disabled by default.
- **Multiple Interfaces:** Bind multiple IPs using `/` (e.g., `127.0.0.1/192.168.0.1`)
- **Linux Remote:** Bind two addresses to open remotely (e.g., `0.0.0.0/+`)

## Custom Prefabs

To create a custom prefab template:

1. Run the **debug version** of MapGenny.dll from [bin/Debug](https://github.com/bmgjet/MapGenny/raw/refs/heads/main/bin/Debug/MapGenny.dll)
2. Generate a map (instead of loading custom prefabs it will save all templates that were found on that map.)

## Batch Processing (Jobs Zip)

### Zip Structure

```
ZipFile.zip
└── FolderName
    ├── job.json           (required)
    ├── height.png         (optional)
    └── customprefabs.zip   (optional)
```

### Example

Download [example jobs (NZ/Australia maps)](https://mega.nz/file/6fY23QpS#bvx4yqBOXEIJW7W11J0XfNZpGRVtoqG4eIz2wO0uQOo) for reference.

### job.json

- Generated via the "Export JSON" button in the web interface
- Uses active settings from the UI
- Custom heightmaps/prefabs must be in the same folder
- Folder names should avoid spaces

The server processes jobs sequentially until stopped or deleted.

## Advertisement Markup

Support for custom text formatting:

| Tag | Description |
|-----|-------------|
| `<size=8></size>` | Text size (default: 8) |
| `<br>` | Line break |

## License

Copyright (c) bmgjet. All rights reserved.

### Grant of License
You are granted a limited, non-exclusive, non-transferable, revocable license to use this software for **non-commercial purposes only**.

### Permitted Use
- Modify and improve the software
- Add features or fix bugs
- Refactor or optimize code

**Conditions:**
- Original copyright notices must remain intact
- Attribution to bmgjet must be preserved
- Changes should be submitted back

### Prohibited Use
- Commercial use or monetization
- Selling, licensing, or renting the software
- Claiming ownership of the original work
- Removing copyright notices

### Commercial Licensing
For commercial use, contact bmgjet for:
1. Prior approval
2. Revenue share agreement
3. 20% of gross profits from map and/or resources created by this software.
