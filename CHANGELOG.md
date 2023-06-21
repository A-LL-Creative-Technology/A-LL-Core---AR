@@ -0,0 +1,37 @@
# Changelog

## [1.2.1] - 2023-06-21

### Added
n/a

### Changed
n/a

### Fixed

- Tap to stop recording not working

## [1.2.0] - 2023-03-01

### Added

- Sample scene to test Core AR functionalities

### Changed

- Updated for ReplayKit to 2.0

### Fixed

- Revert NiceVibration from 3.9 to 4.1

### Update instructions
Before updating ReplayKit to 2.0, uninstall the following assets:
- Delete `Assets/Plugins/VoxelBusters/CoreLibrary` folder
- Delete `Assets/Plugins/VoxelBusters/CrossPlatformReplayKit` folder
- Delete `Assets/Plugins/Android/com.voxelbusters.replaykit.androidlib` folder
- Delete `Assets/Resources/CrossPlatformReplayKitSettings` (if any)

## [1.1.1] - 2023-02-06

### Added

- No new feature or functionality added

### Changed

- Updated README

### Fixed

- Fix NativeGallery permissions
