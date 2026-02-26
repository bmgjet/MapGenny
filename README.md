
MapGenny – Rust Map Generation System
=====================================

Author:
bmgjet

Project Overview:
-----------------
MapGenny is a server-side map generation system designed for the game Rust.

It runs as a Harmony mod and provides a web-based interface for generating,
processing, Rust map files and optional custom prefabs.

[ScreenShot]: https://raw.githubusercontent.com/bmgjet/MapGenny/refs/heads/main/screenshot.jpg  "ScreenShot"

--------------------------------------------------
Tested Platforms
--------------------------------------------------
- Windows 10 / 11
- Ubuntu 22.04
- Debian 12

--------------------------------------------------
Minimum Requirements
--------------------------------------------------
CPU:
- 2 cores minimum
- More cores help slightly, but most logic is single-threaded

Memory:
- 4 GB RAM minimum
- 6 GB+ recommended

Storage:
- 20 GB SSD recommended

--------------------------------------------------
Prerequisites
--------------------------------------------------
- Vanilla Rust Dedicated Server
- Installed via SteamCMD

--------------------------------------------------
Installation
--------------------------------------------------
1. Install MapGenny
-------------------
Place:
MapGenny.dll

Into:
HarmonyMods/

It can be found in bin/Release

--------------------------------------------------
Running the Server
--------------------------------------------------

2. Local Machine
----------------
Run:
RustDedicated.exe

If the server binds to localhost, MapGenny will attempt to automatically
open a web browser to the interface.

--------------------------------------------------

3. Remote Machine
-----------------
To expose the server remotely, specify an IP and port:

Required parameters:
+server.ip
+server.port
+rcon.password

IMPORTANT:
Always set an RCON password or anyone could shut the server down.

-----------------------------------
Windows (start.bat example)
-----------------------------------

RustDedicated.exe ^
+server.ip * ^
+server.port 28016 ^
+rcon.password "YourPassWordHere" ^
-logfile "logs.log"

Notes:
- "*" means listen on all IP addresses
- Replace "YourPassWordHere" with your own password
- -logfile sets a custom log file location

-----------------------------------
Linux (start.sh)
-----------------------------------
If start.sh or start.bat exists, the server will reuse it on restart,
even if originally launched in local mode.

--------------------------------------------------
Server Notes
--------------------------------------------------
- If +server.ip is not specified, the server defaults to localhost
- Multiple interfaces can be bound using "/"
  Example:
  +server.ip 127.0.0.1/192.168.0.1
- On Linux, you must bind two addresses to open remotely
  Example:
  0.0.0.0/+
- Uploads can be disabled:
  +allowuploads false
- Job uploads can be disabled:
  +allowjobs false
- png2cubes can be disabled:
  +allowcubes false
- Only run png2cubes mode:
  +cubesonly true

Advertisement markup support:
<size=8></size>  (default scale is 8)
<br>             (line break)

--------------------------------------------------
Linux Requirements (Ubuntu 22.04 / Debian 12)
--------------------------------------------------
MapGenny requires GDI libraries on Linux.

Install dependencies:
sudo apt install -y libgdiplus libc6-dev

Startup script example:

#!/bin/sh
export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:$(pwd)
export SYSTEM_DRAWING_ALLOW_SYSTEM_GDI_UNIX=1
cd "$(dirname "$0")"
./RustDedicated \
+server.ip 0.0.0.0/+ \
+server.port 28016 \
+rcon.password "YourPassWordHere"

--------------------------------------------------
Custom Prefabs
--------------------------------------------------
Custom Prefab Template:
https://mega.nz/file/vGAV1IID#NE-zj3Fw7fTko5YBx-EN3Rgq_EJHaHFlbmi_VW-wgLE

The download includes its own instructions.

--------------------------------------------------
Jobs Zip Layout
--------------------------------------------------
Example Jobs (NZ / Australia maps):
https://mega.nz/file/6fY23QpS#bvx4yqBOXEIJW7W11J0XfNZpGRVtoqG4eIz2wO0uQOo

Zip Layout:

ZipFile
 └─ Folder
     ├─ job.json
     ├─ height.png            (optional)
     ├─ customprefabs.zip     (optional)

 └─ Folder2
     └─ job.json

--------------------------------------------------
job.json
--------------------------------------------------
- Generated using the "Export JSON" button on the main generation page
- Uses the active settings from the UI
- If using a custom heightmap or prefabs, they must be placed in the same folder
- Filenames must match exactly
- Folder names may be anything (no spaces recommended)

The server will continue processing each job until:
- The job completes
- The stop and delete button is pressed

Large zip files may take time to upload depending on your internet speed.

========================================
END USER LICENSE AGREEMENT (EULA)
========================================

Copyright (c) bmgjet
All rights reserved.

IMPORTANT:
----------
By using, copying, modifying, or distributing this software, you agree to
the terms of this End User License Agreement ("EULA").

If you do not agree, do not use this software.

1. DEFINITIONS
--------------
"Software" refers to all source code, binaries, scripts, assets, and any
associated materials contained in this repository.

"Output" refers to anything generated, compiled, rendered, or produced
using the Software.

"Commercial Use" means any use that generates revenue, monetary gain,
subscriptions, donations, paid access, or is part of a paid product or service.

2. GRANT OF LICENSE
-------------------
bmgjet grants you a limited, non-exclusive, non-transferable, revocable
license to use the Software for NON-COMMERCIAL purposes only.

3. PERMITTED USE
----------------
You MAY:
- Modify the Software to improve functionality
- Add features
- Fix bugs
- Refactor or optimize the code

Provided that:
- All original copyright notices remain intact
- Attribution to bmgjet is preserved in all source files
- Changes are submitted to bmgjet

4. PROHIBITED USE
-----------------
You MAY NOT:
- Use the Software in any paid or commercial project
- Sell, license, rent, or monetize the Software
- Sell, license, or monetize any Output generated by the Software
- Remove or alter copyright notices
- Claim ownership of the original Software

5. COMMERCIAL LICENSING
----------------------
Commercial use of the Software or its Output is ONLY permitted if:

1. Prior approval is obtained from bmgjet
2. A revenue share agreement is accepted
3. 20% of ALL gross profits derived from the Software or its Output
   are paid to bmgjet

Failure to meet these conditions voids any permission to use the Software.

6. OWNERSHIP
------------
The Software is owned by bmgjet.
All modifications remain subject to this EULA.

7. NO WARRANTY
--------------
The Software is provided "AS IS", without warranty of any kind, express or implied,
including but not limited to merchantability or fitness for a particular purpose.

8. LIMITATION OF LIABILITY
--------------------------
In no event shall bmgjet be liable for any damages arising from the use or
inability to use the Software or its Output.

9. TERMINATION
--------------
This license is automatically terminated if you violate any term of this EULA.
Upon termination, you must cease all use of the Software and destroy all copies.

10. GOVERNING LAW
-----------------
This EULA shall be governed by applicable laws as determined by the licensor.
