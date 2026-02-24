# Safety Override

A collocated mixed reality training simulation where two users share the same physical space, each with role-specific responsibilities. Built for Meta Quest headsets using Unity, Photon Fusion for real-time networking, and Arduino for physical hardware integration.

## Introduction

Safety Override is a two-player cooperative VR training experience set in a nuclear power reactor scenario. A supervisor and a technician sit at the same table wearing Meta Quest headsets, seeing different virtual objects overlaid on the shared physical environment through mixed reality passthrough.

The supervisor monitors a pressure gauge and guides the technician using a hand-tracked laser pointer. The technician adjusts a needle on the gauge using a physical potentiometer (Arduino) and confirms their reading. The supervisor then validates whether the needle is correctly positioned within a moving green zone, with the result communicated back to the Arduino as a physical LED response (green for success, red for failure).

This project explores how mixed reality, real-time networking, hand tracking, and physical computing can combine to create a shared, hands-on training experience with educational value in safety-critical decision making under time pressure.

## Design Process

### Goals

- Create a collocated VR experience where two users share the same physical space
- Assign distinct roles (supervisor/technician) with role-specific virtual objects
- Integrate physical hardware (Arduino potentiometer and LED) as tangible input/output
- Use hand tracking for natural interaction (laser pointing, button poking)

### Challenges and Solutions

**Colocation without Shared Spatial Anchors:**
Meta's Colocation Discovery API returned persistent -1002 errors during development. Instead of relying on the API, a manual calibration system was implemented. Each user presses a controller button to place the game content relative to their head position, achieving spatial alignment without shared anchors.

**Design Decision — Manual Calibration over Shared Spatial Anchors:**
Manual calibration was chosen because it provides reliable, predictable results regardless of Meta API availability. The trade-off is that both users must face the same general direction during calibration. For a seated table scenario, this constraint is acceptable and keeps the system simple.

**Role-Based Visibility:**
Rather than spawning separate objects per player, all game objects exist in the scene and are toggled on/off based on the user's role (host = supervisor, client = technician). This simplifies networking since all state lives on a single `NetworkObject`.

**Design Decision — Hand Tracking over Controllers:**
Hand tracking was chosen for the supervisor's laser pointer because it provides a more natural pointing gesture. The technician interacts through a physical potentiometer (more intuitive for "turning a dial") and pokes a virtual button using hand tracking. Controllers are only used for the initial calibration step.

**Design Decision — Physical Potentiometer over Virtual Slider:**
A physical Arduino potentiometer was chosen over a virtual UI slider because tangible input provides better tactile feedback for precise adjustments, which is important in a training simulation where accuracy matters.

**Button Color System:**
The Meta Interaction SDK's `InteractableDebugVisual` component overrides material colors on state changes. To achieve reliable button color changes (red/green/yellow), pre-created materials are swapped at runtime via `renderer.sharedMaterial`, bypassing the SDK's color system entirely.

**Face-to-Face Mirroring:**
Since both users sit opposite each other, the X axis appears mirrored. The needle position is negated on the client (`-targetX`), and the supervisor's laser uses a networked coordinate conversion with X negation so both users see consistent positions relative to their own perspective.

## Features and Functionalities

### Collocated Mixed Reality
- Two users share the same physical table with Meta Quest passthrough
- Virtual objects appear anchored to the real environment
- Manual calibration positions content at arm's reach, at table height

### Role-Based Gameplay
- **Supervisor (Host):** Sees the pressure gauge with a moving green zone, a confirm button, and a hand-tracked laser pointer
- **Technician (Client):** Sees the same gauge with a needle (controlled by Arduino potentiometer) and a yellow confirmation button

### Moving Green Zone
- The green zone oscillates along the gauge using a sine wave
- Randomly pauses for a configurable duration (default 5 seconds), creating time pressure
- Speed and pause duration are adjustable in the Unity Inspector

### Supervisor Laser Pointer
- Red laser ray extends from the supervisor's right hand
- Hand-tracked using Meta Quest hand tracking
- Visible to both users through Photon Fusion networking
- Mirrored for the client's face-to-face perspective

### Client Confirmation System
- Technician pokes a yellow button to signal readiness
- Supervisor's button changes from red to green via material swap
- Supervisor then presses their button to validate the needle position

### Arduino Hardware Integration
- Physical potentiometer controls the needle position via serial communication
- Arduino LED provides tangible feedback: green (success) or red (failure)
- Communication bridged through Unity using the Ardity serial library
- Potentiometer values are networked to all clients via Photon Fusion RPCs

### Instruction Canvas
- A floating world-space canvas appears before calibration
- Displays the game narrative and step-by-step instructions for both roles
- Automatically hidden when the user triggers calibration

### Ambient Sound
- Background audio with looping playback for immersion

## Installation

### Prerequisites

| Requirement | Version |
|---|---|
| Unity | 6000.2.10f1 |
| Target Platform | Android (Meta Quest) |
| Meta XR SDK | 83.0.1 |
| Photon Fusion | 2 (imported via .unitypackage) |
| Ardity | Included in `Assets/Ardity/` |
| Arduino IDE | For uploading sketch to Arduino board |

### Hardware Required

- 2x Meta Quest headsets (Quest 2, Quest Pro, Quest 3, or Quest 3S)
- 1x Arduino board with potentiometer and LED
- 1x PC running Unity Editor (serves as Arduino serial bridge)
- All devices on the same Wi-Fi network

### Setup Steps

1. **Clone the repository:**
   ```
   git clone <repository-url>
   ```

2. **Open in Unity:**
   - Open Unity Hub and add the project
   - Use Unity version **6000.2.10f1**
   - Open `Assets/Scenes/SampleScene.unity`

3. **Photon Fusion App ID:**
   - Go to [Photon Dashboard](https://dashboard.photonengine.com/) and create a Fusion app
   - In Unity, go to `Fusion > Fusion Hub > Setup` and paste your App ID
   - The current App ID is configured for this project's Photon account

4. **Arduino Setup:**
   - Upload the Arduino sketch to your board (potentiometer on analog pin, LED on digital pin)
   - Connect the Arduino to the PC via USB
   - In Unity, set the correct COM port on the `SerialController` component in the scene

5. **Build for Quest:**
   - Go to `File > Build Settings`
   - Select **Android** platform
   - Connect Quest headset via USB or use wireless ADB
   - Click **Build and Run**

## Usage

### Starting a Session

1. **Launch on first Quest headset** — this device becomes the Host (Supervisor)
2. **Launch on second Quest headset** — this device joins as Client (Technician)
3. **Start Unity Editor Play Mode** on the PC — this connects as a third client and acts as the Arduino serial bridge

### Calibration

1. Both headset users sit at opposite sides of a table
2. Both users press the **A button** or **right trigger** on their controller
3. Game content appears at table height in front of each user
4. After calibration, hand tracking activates — users can put down controllers

### Gameplay Flow

1. The green zone starts moving along the gauge
2. The **supervisor** uses their right hand as a laser pointer to guide the technician toward the green zone
3. The **technician** turns the physical potentiometer to move the needle
4. When the technician is satisfied with the needle position, they **poke the yellow button** with their hand
5. The supervisor's button turns **green** (from red), indicating the technician is ready
6. The supervisor **pokes the confirm button**
7. The system checks if the needle is inside the green zone:
   - **Success:** Arduino LED turns green
   - **Failure:** Arduino LED turns red
8. The process repeats

### Editor Testing (Single Headset)

For testing with only one headset:
- Quest headset runs as **Host/Supervisor**
- Unity Editor Play Mode runs as **Client/Technician**
- Press **Space** in the Editor to calibrate
- Press **C** in the Editor to simulate the client button press

### Configurable Parameters

| Parameter | Location | Default | Description |
|---|---|---|---|
| Green Zone Speed | DigitalTwin > SafetyGameManager | 0.3 | Speed of green zone oscillation |
| Pause Duration | DigitalTwin > SafetyGameManager | 5.0 | How long the green zone pauses (seconds) |
| Zone Width | DigitalTwin > SafetyGameManager | 0.15 | Width of the green zone |
| Laser Length | DigitalTwin > SupervisorLaser | 3.0 | Length of the supervisor's laser ray |
| Laser Width | DigitalTwin > SupervisorLaser | 0.005 | Thickness of the laser ray |
| Content Distance | CalibrationManager | 0.5 | Distance from head to game content (meters) |
| Height Offset | CalibrationManager | -0.3 | Height below eye level for table placement |

## Project Structure

```
Assets/
├── Scenes/
│   └── SampleScene.unity          # Main scene
├── Scripts/
│   ├── ConnectionManager.cs       # Photon Fusion networking setup
│   ├── ManualCalibrationManager.cs# Manual spatial calibration
│   ├── SafetyGameManager.cs       # Core game logic, role visibility, green zone
│   ├── SupervisorLaser.cs         # Networked hand-tracked laser pointer
│   └── TwinController.cs          # Arduino serial communication bridge
├── Materials/
│   ├── ButtonRed.mat              # Supervisor button default
│   ├── ButtonGreen.mat            # Supervisor button when client confirmed
│   └── ButtonYellow.mat           # Client button
├── Ardity/                        # Serial communication library
│   ├── Scripts/
│   └── Prefabs/
└── Photon/
    └── Fusion/                    # Photon Fusion networking SDK
```

### Scene Hierarchy

```
[BuildingBlock] Camera Rig         — OVRCameraRig with Eye Level tracking
GameContent                        — Parent for all game objects (moved by calibration)
  ├── LinearGauge                  — The pressure gauge
  │   ├── Track                    — Gauge background
  │   ├── GreenZone                — Moving target zone (networked)
  │   └── Needle                   — Arduino-controlled indicator
  ├── BigRedButton                 — Supervisor's confirm button
  └── ClientYellowButton           — Technician's confirm button
CalibrationManager                 — ManualCalibrationManager component
[BuildingBlock] Network Manager    — ConnectionManager + Photon NetworkRunner
DigitalTwin                        — SafetyGameManager + SupervisorLaser + NetworkObject
ArduinoManager                     — TwinController + SerialController
```

## References

- [Photon Fusion 2 Documentation](https://doc.photonengine.com/fusion/current/getting-started/fusion-intro) — Networking framework
- [Meta XR SDK Documentation](https://developer.oculus.com/documentation/unity/unity-overview/) — VR platform SDK
- [Meta Interaction SDK](https://developer.oculus.com/documentation/unity/unity-isdk-interaction-sdk-overview/) — Hand tracking and poke interactions
- [Ardity - Arduino Unity Communication](https://github.com/DWilches/Ardity) — Serial communication library
- [Unity Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/index.html) — Rendering pipeline

## Contributors

- **[Your Name]** — Design, development, and implementation

  MSc in Design for Creative and Immersive Technology

  Contact: [Your Email]
