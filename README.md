# Lazy Light Generator for Unity

The **Lazy Light Generator** is a Unity Editor tool that automates the placement of lights in your scene using smart sampling, geometry awareness, and scene analysis.

> Whether you're prototyping a level or polishing a cinematic scene, this tool helps you generate high-quality lighting setups in seconds.

---

## Features

- **Auto Light Placement** with Poisson Disk Sampling
- **Scene-Aware Placement**: Places lights intelligently based on scene geometry
- Supports `Point`, `Spot`, and `Area` lights (HDRP)
- **HDRP/URP Compatibility**
- **Automatic Reflection Probe Generation**
- Global Illumination toggle (Realtime/Mixed)
- Save and Load Lighting Setups via JSON
- Groups lights under a root object for clean hierarchy management
- Scene View preview of light placement

---

## How to Use

## Installation

You can install the Lazy Light Generator in two ways:

### Method 1: Manual Script Import

1. Download the `LazyLightGenerator.cs` script.
2. Place it inside your Unity project’s `Assets/Editor` folder.
3. Open Unity. The tool will now be accessible from the menu:  
   `Tools > Lazy Light Generator`



### Method 2: Using Git URL (Recommended for updates)

1. Open Unity.
2. Go to `Window > Package Manager`
3. Click the `+` button ➜ **Add package from Git URL**
4. Paste this URL: https://github.com/Jay-A-Kad/LazyLightPackage.git
5. Click **Add**. Unity will fetch and import the tool automatically.
6. You can now access it from `Tools > Lazy Light Generator`



### 2. **Open the Tool**
Go to `Tools > Lazy Light Generator` in the Unity menu bar.

### 3. **Configure Lighting Settings**
- Select:
  - Light type: `Point`, `Spot`, or `Area`
  - Light color, intensity, range
  - Spot angle
- Toggle options:
  - Area lights (HDRP only)
  - Global Illumination
  - Scene View preview

### 4. **Generate Lights**
Click **"Generate Lights"**. This will:
- Calculate bounds based on scene geometry
- Use Poisson Disk Sampling to spread lights
- Auto-place each light near ceiling or visible space
- Attach a Reflection Probe per light
- Group all under `LightingRig_<Tag>`

### 5. **Manage Lights**
- Click **"Delete All Tagged Lights"** to remove all generated lights with the selected tag.
- Use **Export** and **Import** to save/load lighting setups as `.json`.

---

## File Structure
Assets/
├── Editor/
    └── LazyLightGenerator.cs


---

## Technical Documentation
### `GenerateAdvancedLights()`
- Calculates scene bounds via `Renderer[]`
- Samples points via `PoissonDiskSample()`
- Uses `Raycast` to position lights near ceilings
- Creates and configures Unity `Light` component:
  - HDRP: uses `HDAdditionalLightData`
  - URP: uses `UniversalAdditionalLightData`

### `PoissonDiskSample(center, radius, count)`
- Evenly distributes light positions with distance control

### `ExportLightingToJson() / ImportLightingFromJson()`
- Serializes light position, rotation, and settings for portability

### `ReflectionProbe`
- One probe per light
- Set to `Realtime` mode, refreshing `EveryFrame`

## Test Photos



## ToDo List

- NavMesh-aware placement for corridor lights
- Auto-classification of `"HeroObjects"` and focused spotlighting
- Light baking button (coming soon)
- AI-guided placement with semantic scene parsing (experimental)


## Requirements

- Unity 2021.3+
- Compatible with:
  - Built-in RP
  - URP
  - HDRP (Area lights + GI support)

---

## Author

**Jay Kadam**  

Feel free to contribute or fork the tool if you find it helpful!

---
