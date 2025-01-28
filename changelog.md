# Solid Model Browser, change log

## v0.5 (28.01.2025)

- Settings Menu added (instead of opening settings.ini file)
- GCODE format import added (Marlin linear moves compatible, experimental)
- Model Z-axis slicing (with slider)
- Camera positioning at geometric/points_avg center buttons
- Camera default positioning DefaultLookAtModelPointsAvgCenter setting added
- Sort files by extensions setting added
- Sort folders and files for all supported filesystems (fixed)
- Open folder in filepanel and select file from command line argument (fixed)

## v0.4 (23.12.2024)

- PLY format import added (ASCII and Binary little-endian)
- Wireframe mode implemented
- WireframeEdgeScale setting added
- IgnoreOriginalNormals setting added, to skip normals if there are a lot files with broken or incorrect normals in user library
- UnsmoothAfterLoading setting added, to load all models in flat mode

## v0.3.1 (08.12.2024)

- 3MF import compatibility improvements
- Different colors for files of different types in file panel 

## v0.3 (25.10.2024)

- 3MF with components import added (extended 3MF support)
- Tool to reset meshes smooth structure to flat (button on tool panel)
- Camera front, back, top, bottom, left, right view positions added (hotkeys)
- Camera initial shift from model setting added
- Model rotation angle 45/90 deg setting added
- Ground plane added (tool panel)
- [F1] help page added
- 3MF import, no transform matrix for object bugfix
- interface updates

## v0.2 (21.10.2024)

- 3MF import class was remastered
- model file name may be passed like command line argument to open with app
- window max size calculation method was changed
- window max size can be manually selected in settings (to solve problems with non-primary displays)
- header double click expands window
- camera FOV and FishEyeFOV angles settings were added
- interface updates

## v0.1 initial release (17.10.2024)
