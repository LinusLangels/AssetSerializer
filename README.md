# AssetSerializer
Alternative to Unity's AssetBundle system.

## Overview
- Any top level monobehaviour attached to a prefab is a valid target for serialisation.
- The Asset Serializer is built around node/tree structures where everything built from a top down perspective.
- This file format and the tools it has are built with flexibility in mind.
- Currently it serialises from within the Unity editor, but plans are in the making of porting it to Maya and 3DS Max.

## Usage 
1. Apply a top level control script/monobehaviour to any prefab you like.
2. Add either the builtin class attribute: "GenericSerializerAttribute" to that monobehaviour. 
If more control is needed derive your own attribute from this.
3. Any audio or texture asset should be compressed beforehand, you will find these tools if you right-click any valid asset in the Unity Editor.
This will generate a cache file alongside the original asset. When the serializer goes through all the assets it will try to find the previously compressed files.
Otherwise it will generate new ones on the fly while serialising (Makes the process slower)
4. The output will be put in a folder with the same name as the prefab that was selected for export.
5. To load files at runtime you can look in the ImportExportMain.cs as reference.


## Notes
Much of the source is currently in C/C++ and implemented as plugins.
For the future i plan to release all of this source and link them to this repository.
