# Solid Model Browser

This app is viewer for STL, 3MF, OBJ, PLY, GCODE files.
User can select files in left file panel and observe it's 3D content.

![cover01](https://github.com/user-attachments/assets/c996f67c-7100-4a8b-92c1-af75fd2d44bb)

## Requirements

.NET Framework 4.8, WPF (Windows 7 ... Windows 11)

## Installation

No installation needed, just copy executable to any empty folder. Settings file with defaults will be created after first startup.

## Build

Build project with Visual Studio 2022

## Features

- Open binary and ASCII STL files, load main 3Dmodel from 3MF files (CURA projects), partial support for OBJ files with triangle faces, PLY (ASCII and binary little-endian), experimental GCODE support with linear movements (Marlin compatible codes)

- Model slicing along model local Z axis (mostly to view GCODE layers)

- Fly camera around model

- Save current view from camera to PNG with selected DPI (in settings file)

- Open current file with local installed application like slicer or 3D editor

- Rotate model

- Set up materials for view

- Perspective and orthographic camera modes, fish eye FOV model

- Show XYZ axes

- Resolve normals problems, polygon vertex order

- Show basic model info

- Dark and Light app themes

![img01](https://github.com/user-attachments/assets/dd72c856-e867-4916-bc21-bff514890afe)

## Screenshots

![img02](https://github.com/user-attachments/assets/03dd9a9f-9a7e-477d-a349-92937fc5d4d3)

![img03](https://github.com/user-attachments/assets/dc0cea81-80e8-407a-b1f1-e9d900b76b83)
