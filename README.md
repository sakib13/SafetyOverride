# Safety Override

Safety Override is a collocated mixed reality training simulation built for Meta Quest headsets. Two users sit at the same table, each wearing a headset, and they see different virtual objects depending on their role. The project runs on Unity with Photon Fusion for networking and an Arduino for physical input and output.

## Introduction

The concept behind Safety Override is a nuclear power reactor training scenario. One person plays as the supervisor and the other as the technician. Both sit at the same physical table and see virtual objects through mixed reality passthrough, but each person sees different things based on their role.

The supervisor has a pressure gauge with a green zone that keeps moving, and they can point at it with a laser that comes from their hand. The technician has a physical knob (Arduino potentiometer) that controls a needle on the gauge. The technician needs to line up the needle with the green zone, then poke a virtual button to confirm. After that the supervisor checks if the needle is actually in the right spot and presses their own button. If the needle is inside the green zone the Arduino lights up green, otherwise it lights up red.

The project was built to explore how far the combination of mixed reality, networking, hand tracking, and physical hardware can be pushed into one shared experience. The training scenario adds time pressure because the green zone keeps moving and pausing randomly, so both players have to communicate and act fast.

## Design Process

### Goals

The project set out to achieve the following:

- Get two people into the same physical space with collocated VR
- Give each person a different role with their own set of virtual objects
- Use a real physical device (Arduino with a potentiometer and LED) instead of keeping everything virtual
- Make the interaction feel natural by using hand tracking instead of controllers

### Challenges and Solutions

**Colocation was the hardest part.**
The project originally attempted to use Meta's Colocation Discovery API to align both headsets automatically. But it kept throwing error code 1002 and no fix could be found despite extensive troubleshooting. So the project went with a manual approach instead. Each user presses a button on their controller and the game content gets placed in front of them relative to where their head is. It is simple but it works. The downside is both users need to roughly face the same direction when they calibrate, but since they are sitting at a table this is not really a problem.

**Why manual calibration and not shared spatial anchors?**
Because the API was broken and the project needed something that works reliably every time. Manual calibration is predictable. You press a button, content appears in front of you. No dependency on Meta's cloud services or anchor sharing. For a seated scenario at a table this is more than enough.

**Handling role based visibility.**
Instead of spawning separate objects for each player, all the game objects already exist in the scene. When a player connects, the code just turns on or off the relevant objects based on whether they are the host (supervisor) or client (technician). This keeps the networking simple because all the game state lives on one NetworkObject.

**Why hand tracking instead of controllers?**
For the supervisor, pointing at the gauge with your actual finger feels way more natural than using a controller joystick. For the technician, they already have a physical knob to turn, so using a controller on top of that would be awkward. Controllers are only used at the very beginning for the calibration step, and after that users put them down and switch to hand tracking.

**Why a physical potentiometer instead of a virtual slider?**
Turning a real knob just feels better and gives more precise control than dragging a virtual slider in the air. In a training simulation where accuracy matters, having something physical in your hands makes a big difference. Plus it demonstrates hardware integration which was one of the goals of the project.

**The button color problem.**
This one took a while to figure out. The Meta Interaction SDK has a component called `InteractableDebugVisual` that keeps overriding the button's material color whenever the button state changes. So no matter what color was set in code, it would get overwritten. The fix was to create separate materials (red, green, yellow) and swap the entire material at runtime using `renderer.sharedMaterial`. That way it does not matter what the SDK tries to do with the color because the whole material is different.

**Face to face mirroring.**
When two people sit across from each other, their left and right are flipped. So if the supervisor sees the green zone on their left, the technician should also see it on their left from their own perspective. To fix this the project negates the X position of the needle on the client side. The laser pointer also gets its X coordinate flipped so both users see it pointing at the same spot on the gauge.

## Features and Functionalities

### Collocated Mixed Reality
- Two users share the same physical table wearing Meta Quest headsets
- Virtual objects show up on top of the real environment through passthrough
- Each user calibrates by pressing a button and content appears at table height in front of them

### Role Based Gameplay
- **Supervisor (Host):** Sees the pressure gauge with a moving green zone, a confirm button, and a red laser that follows their right hand
- **Technician (Client):** Sees the same gauge but with a needle that they control with the Arduino potentiometer, plus a yellow confirmation button

### Moving Green Zone
- The green zone moves back and forth along the gauge in a sine wave pattern
- It randomly stops for a few seconds then starts moving again, which adds time pressure
- Both the speed and the pause duration can be changed in the Unity Inspector

### Supervisor Laser Pointer
- A red laser line extends from the supervisor's right hand
- It uses Meta Quest hand tracking so no controller is needed
- Both users can see the laser through Photon Fusion networking
- The X position gets flipped for the client so it looks correct from their side of the table

### Client Confirmation System
- The technician pokes a yellow button with their hand when they think the needle is in the right spot
- This sends a network event and the supervisor's button turns from red to green
- Then the supervisor can press their button to check the result

### Arduino Hardware Integration
- A physical potentiometer controls the needle position through serial communication
- The Arduino LED gives real feedback: green means success, red means failure
- The serial data goes through the Ardity library in Unity
- The potentiometer values get sent to all connected clients through Photon Fusion RPCs

### Instruction Canvas
- Before calibration, a floating canvas shows up in front of the user
- It explains the game story and gives step by step instructions for both roles
- It disappears automatically once the user calibrates

### Ambient Sound
- There is background audio that loops during gameplay for immersion

## Installation

### Prerequisites

| Requirement | Version |
|---|---|
| Unity | 6000.2.10f1 |
| Target Platform | Android (Meta Quest) |
| Meta XR SDK | 83.0.1 |
| Photon Fusion | 2 (imported via .unitypackage) |
| Ardity | Included in `Assets/Ardity/` |
| Arduino IDE | For uploading the sketch to the Arduino board |

### Hardware Required

- 2 Meta Quest headsets (Quest 2, Quest Pro, Quest 3, or Quest 3S)
- 1 Arduino board with a potentiometer and an LED
- 1 PC running Unity Editor (this acts as the Arduino serial bridge)
- All devices need to be on the same Wi-Fi network

### Setup Steps

1. **Clone the repository:**
   ```
   git clone <repository-url>
   ```

2. **Open in Unity:**
   - Open Unity Hub and add the project
   - Make sure you are using Unity version **6000.2.10f1**
   - Open `Assets/Scenes/SampleScene.unity`

3. **Photon Fusion App ID:**
   - Go to [Photon Dashboard](https://dashboard.photonengine.com/) and create a Fusion app
   - In Unity go to `Fusion > Fusion Hub > Setup` and paste your App ID

4. **Arduino Setup:**
   - Upload the Arduino sketch to your board (potentiometer on analog pin, LED on digital pin)
   - Connect the Arduino to the PC with USB
   - In Unity, set the correct COM port on the `SerialController` component in the scene

5. **Build for Quest:**
   - Go to `File > Build Settings`
   - Select **Android** platform
   - Connect your Quest headset via USB or wireless ADB
   - Click **Build and Run**

## Usage

### Starting a Session

1. Launch the app on the first Quest headset. This device becomes the Host and takes the Supervisor role.
2. Launch the app on the second Quest headset. This one joins as the Client and takes the Technician role.
3. Hit Play in the Unity Editor on the PC. This connects as a third client and handles the Arduino serial communication.

### Calibration

1. Both headset users sit at opposite sides of a table
2. Each user presses the **A button** or **right trigger** on their controller
3. The game content shows up at table height in front of each user
4. After that, hand tracking kicks in and users can put their controllers down

### Gameplay Flow

1. The green zone starts moving along the gauge
2. The supervisor points at the gauge with their right hand to show the technician where to aim
3. The technician turns the physical potentiometer to move the needle
4. When the technician thinks the needle is lined up, they poke the yellow button
5. The supervisor's button turns green, meaning the technician is ready
6. The supervisor pokes their confirm button
7. If the needle is inside the green zone the Arduino LED lights up green. If not, it lights up red.
8. The whole thing repeats

### Editor Testing (Single Headset)

If only one headset is available, most of the functionality can still be tested:
- The Quest headset runs as Host/Supervisor
- Unity Editor Play Mode runs as Client/Technician
- Press **Space** in the Editor to calibrate
- Press **C** in the Editor to simulate the client button press

### Configurable Parameters

| Parameter | Location | Default | Description |
|---|---|---|---|
| Green Zone Speed | DigitalTwin > SafetyGameManager | 0.3 | How fast the green zone moves |
| Pause Duration | DigitalTwin > SafetyGameManager | 5.0 | How long the green zone stops for (seconds) |
| Zone Width | DigitalTwin > SafetyGameManager | 0.15 | How wide the green zone is |
| Laser Length | DigitalTwin > SupervisorLaser | 3.0 | How far the laser extends |
| Laser Width | DigitalTwin > SupervisorLaser | 0.005 | How thick the laser line is |
| Content Distance | CalibrationManager | 0.5 | How far from the head the content appears (meters) |
| Height Offset | CalibrationManager | -0.3 | How far below eye level the content sits |

## Project Structure

```
Assets/
├── Scenes/
│   └── SampleScene.unity          # Main scene
├── Scripts/
│   ├── ConnectionManager.cs       # Photon Fusion networking setup
│   ├── ManualCalibrationManager.cs# Manual spatial calibration
│   ├── SafetyGameManager.cs       # Core game logic, role visibility, green zone
│   ├── SupervisorLaser.cs         # Networked hand tracked laser pointer
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
[BuildingBlock] Camera Rig         OVRCameraRig with Eye Level tracking
GameContent                        Parent for all game objects (moved by calibration)
  ├── LinearGauge                  The pressure gauge
  │   ├── Track                    Gauge background
  │   ├── GreenZone                Moving target zone (networked)
  │   └── Needle                   Arduino controlled indicator
  ├── BigRedButton                 Supervisor's confirm button
  └── ClientYellowButton           Technician's confirm button
CalibrationManager                 ManualCalibrationManager component
[BuildingBlock] Network Manager    ConnectionManager + Photon NetworkRunner
DigitalTwin                        SafetyGameManager + SupervisorLaser + NetworkObject
ArduinoManager                     TwinController + SerialController
```

## References

- [Photon Fusion 2 Documentation](https://doc.photonengine.com/fusion/current/getting-started/fusion-intro) for the networking framework
- [Meta XR SDK Documentation](https://developer.oculus.com/documentation/unity/unity-overview/) for the VR platform
- [Meta Interaction SDK](https://developer.oculus.com/documentation/unity/unity-isdk-interaction-sdk-overview/) for hand tracking and poke interactions
- [Ardity](https://github.com/DWilches/Ardity) for Arduino to Unity serial communication
- [Unity Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/index.html) for rendering

## Contributors

- **[Your Name]** Design, development, and implementation

  MSc in Design for Creative and Immersive Technology

  Contact: [Your Email]
