# HolsterResizer

A simple mod for Bonelab which resizes the gun holsters and their contents when using a small avatar so they don't get in the way of things.

If BoneLib is installed there's also a submenu where you can configure:
- SizeMultiplier: Adjust the relative size of the holsters
- Scale Up: Determines if the holsters scale up with big avatars
- Scale Down: Determines if the holsters scale down with small avatars
- Scale Bodylog: Determines if the bodylog also gets resized with avatars

Compatible with Fusion

## Compilation

To compile this mod, create a file named `HolsterResizer.csproj.user` in the "HolsterResizer" folder of this
project. Copy this text into the file, replacing the path in `BONELAB_PATH` with
the path to your BONELAB installation.
```
<?xml version="1.0" encoding="utf-8"?>
<Project>
    <PropertyGroup>
        <BONELAB_PATH>C:\Program Files (x86)\Steam\steamapps\common\BONELAB</BONELAB_PATH>
    </PropertyGroup>
</Project>
```
<br/>
<br/>
