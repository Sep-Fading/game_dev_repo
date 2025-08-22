# RetargetPro-HumanoidBaker Tool Documentation

## Important Notice
This tool is built upon and requires [Retarget Pro](https://assetstore.unity.com/packages/package/240061) from the Unity Asset Store. You must purchase and import Retarget Pro before using this tool.

## Overview
RetargetPro-HumanoidBaker is an advanced Unity Editor extension that enhances the original Retarget Pro's capabilities by introducing specialized Any-to-Humanoid animation retargeting. 

While the original plugin focuses on Humanoid-to-Generic conversion, this tool maintains the full advantages of Unity's Humanoid animation system while solving complex retargeting scenarios.

### Key Advantages
- **Preserves Humanoid Animation Features**: 
	- Fully compatible with Unity's animation layers, masks, and other Humanoid-specific features
- **Transfers Weapon Chain & Socket Animation**: 
	- Automatically transfers weapon attachments and socket retargetd-animations between different humanoid models
    - Save your life from manual re-rigging ( due to mismatch bone orientation)
- **Streamlined Workflow**: 
	- Eliminates the need for external 3D modeling software. All happens in Unity!
- **One-Step Solution**: 
	- Direct transfer of both humanoid animations and auxiliary bone animations in a single process

### Technical Considerations
- **Humanoid Bone Mapping**: 
	- Core humanoid animations benefit from Unity's automatic Avatar mapping system
- **Auxiliary Bone Handling**: 
	- Non-humanoid bones (weapons, props, etc.) maintain their animation data with fixed bone paths in the target model
- **Optimal Use Case**: 
	- Ideal for projects requiring frequent animation retargeting while preserving complex prop animations and maintaining Humanoid system benefits

## Features
- Single animation retargeting
- Batch animation processing
- Real-time (multiple) animation preview
- Root motion support
- Progress tracking and error reporting
- Playback controls (play, pause, loop)
- Time slider for precise playback frame selection

## Requirements
- Unity 2020.3 or later
- [Retarget Pro](https://assetstore.unity.com/packages/package/240061) from Unity Asset Store
- Source character's AnimationType must match source animation's type.
- Target character must be a humanoid model

## Installation
1. Purchase and import [Retarget Pro](https://assetstore.unity.com/packages/package/240061) (v3.4.0) from Unity Asset Store
2. Import the RetargetProHumanoidBake folder into your Unity project's Assets folder
3. The tool will automatically be integrated into the Unity Editor

## Usage

### Accessing the Tool
1. Open the tool window by navigating to Window > KINEMATION > Retarget Humanoid Animation
2. The window can be docked anywhere in your Unity Editor layout

### Basic Setup
1. Assign the source character (the character with the original animation)
2. Assign the target character (the character to receive the retargeted animation)
3. Ensure both characters have the same pose ( T-POSE/A-POSE )
3. Configure the retarget profile with your desired settings

### Single Animation Mode
1. Select "Single Mode" (default)
2. Drag and drop your animation clip into the Source Animation field
3. Use the preview controls to verify the animation
4. Click "Retarget Animation" to process
5. The retargeted animation will be saved in your project

### Batch Processing Mode
1. Toggle "Batch Mode"
2. Drag and drop multiple animation clips into the designated area
3. Preview individual animations as needed
4. Click "Retarget X Animations" to process all clips
5. Monitor progress in the progress bar
6. Results will be saved in the same output directory

### Preview Controls
- Play/Pause: Toggle animation playback
- Loop: Toggle animation looping
- Time Slider: Scrub through the animation
- Time Display: Shows current time / total duration

### Additional Features
- Root motion detection and handling
- Error validation and reporting
- Progress tracking for batch operations
- Preview state persistence

## Best Practices
1. Ensure both characters are properly configured as humanoids
2. Test with a single animation before batch processing
3. Keep the tool window visible during batch processing
4. Check for error messages in the help box
5. Verify retargeted animations before using in production

## Troubleshooting
If you encounter issues:
1. Verify both characters are properly configured as humanoids
2. Check that all required fields are assigned
3. Ensure animations are compatible with humanoid rigs
4. Review error messages in the help box
5. Try restarting Unity if issues persist

## Output
Retargeted animations are saved in your project with the following naming convention:
- Single mode: [TargetCharacteName]_[OriginalName]
- Batch mode: [TargetCharacteName]_[OriginalName] for each processed animation
