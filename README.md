# [Lemon]Loader Installer
Installs MelonLoader onto APKs

# Licensing/Credits
- [SAI](https://github.com/Aefyr/SAI), under the [GPL-3.0 license](https://github.com/Aefyr/SAI/blob/master/LICENSE)
- [SplitAPKInstall](https://github.com/nkalra0123/splitapkinstall), under the [Apache-2.0 license](https://github.com/nkalra0123/splitapkinstall/blob/master/LICENSE)
- [Apkifier](https://github.com/emulamer/Apkifier) under the [MIT license](https://github.com/emulamer/Apkifier/blob/master/LICENSE)
- [QuestPatcher](https://github.com/Lauriethefish/QuestPatcher) under the [zlib License](https://github.com/Lauriethefish/QuestPatcher/blob/main/LICENSE)
  - Changes (in accordance with the License; all used files are located in `Core/Utilities/Signing` and `Core/Utilities/Axml`)
    - Merged ApkSigner's V2 code into my APKSigner class
    - Changed namespace of helper classes to match other files and folder hierarchy position
    - QuestPatcher.Axml had no changes

## Building

- Install Xamarin and update path in `./App/MelonLoaderInstaller.csproj`
- Install Java 11
- Install Android Studio build tools and put them on your path
  - Mine were at `/Users/$USER/Library/Android/sdk/build-tools/33.0.1`
- Create a keystore using password from `./App/MelonLoaderInstaller.csproj`
  ```
  keytool -genkey -v -keystore ./App/keystore.jks -keyalg RSA -keysize 2048 -validity 10000 -alias lemonman
  ```
- Install dependencies
  ```
  msbuild /t:Restore
  ```
- Build
  ```
  msbuild ./App/MelonLoaderInstaller.csproj /p:Configuration=Release /t:PackageForAndroid
  ```
- Sign APK using password from `./App/MelonLoaderInstaller.csproj`
  ```
  jarsigner -verbose -sigalg SHA256withRSA -digestalg SHA-256 -keystore ./App/keystore.jks ./App/bin/Release/com.melonloader.installer.apk lemonman
  ```
- Zip-align APK
  ```
  "/Users/$USER/Library/Android/sdk/build-tools/33.0.1/zipalign" -v 4 ./App/bin/Release/com.melonloader.installer.apk ./App/bin/Release/com.melonloader.installer.aligned.apk
  ```
- Install `./App/bin/Release/com.melonloader.installer.aligned.apk` via SideQuest
