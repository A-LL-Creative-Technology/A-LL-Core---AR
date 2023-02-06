# Augmented Reality Core package of A-LL Creative Technology mobile Unity Framework

## Installation

To use this package in a unity project :

1. In Unity, open Window > Package Manager and "Add Package from git url ..." and insert this URL `https://github.com/A-LL-Creative-Technology/A-LL-Core---AR.git`
2. Add the following third-party packages from the Package Manager
    1. Add the free packages to your assets from the [Unity asset store](https://assetstore.unity.com/) and ask Laurent or Leo to give you access to the paid packages in Unity.
    2. Select "My Assets" in the Package Manager to display free and paid Packages from the Asset Store
    3. Select and import all these packages:
        - Nice Vibrations by Lofelt | HD Haptic Feedback for Mobile and Gamepads (Paid and deprecated)
        - [Native Gallery for Android & iOS](https://assetstore.unity.com/packages/tools/integration/native-gallery-for-android-ios-112630) (Free)
        - [Cross Platform Replay Kit](https://assetstore.unity.com/packages/tools/integration/easy-screen-recording-free-version-cross-platform-replay-kit-191899) (Free)
            - Add an Assembly Definition Reference, named `ReplayKit`, in Plugins/VoxelBusters and pointing to `all.core.ar.runtime`.