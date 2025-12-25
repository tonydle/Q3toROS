# Q3toROS

Unity Project to send Passthrough (Colour) and Depth camera streams from Meta Quest 3 to ROS2.
<!-- Demo GIF -->
![Demo GIF](docs/images/StereoPassthroughToROS.gif)

Unity Version: **6000.2.14f1**

Uses Unity [ROS TCP Endpoint](https://github.com/Unity-Technologies/ROS-TCP-Endpoint) package to send images over TCP to ROS2.

Configurable settings menu during runtime
---
![Settings Menu](docs/images/SettingsMenu.gif)

## Installation
Available as a built apk. See [Releases](https://github.com/tonydle/Q3toROS/releases) for latest version. 

You can use [Meta Quest Developer Hub](https://developers.meta.com/horizon/documentation/unity/ts-mqdh/) or [SideQuest](https://sidequestvr.com/) to install the apk onto your Quest 3. Then launch it under "Unknown Sources" in your App Library.

### ROS2 Setup
Clone the `main-ros2` branch of [ROS-TCP-Endpoint](https://github.com/Unity-Technologies/ROS-TCP-Endpoint) into your ROS2 workspace `src` folder and colcon build it
```bash
git clone -b main-ros2 https://github.com/Unity-Technologies/ROS-TCP-Endpoint.git
```

## Usage
First launch the ROS TCP Endpoint node
```bash
ros2 launch ros_tcp_endpoint endpoint.py
```
Then launch the app on your Quest 3. Make sure your Quest 3 and ROS2 PC are on the same network.

Enter the ROS2 PC's IP address in the app settings menu, and select **Connect and Start**

## Published Topics
<!-- Topic Table -->
The following ROS2 topics are published by the app (if enabled in the settings menu):
| Topic Name|Message Type|Description|
|-----------------------|----------------|-------------------------------|
| `/quest3/left_eye/color/compressed` | `sensor_msgs/msg/CompressedImage` | Left eye colour (passthrough) camera stream |
| `/quest3/right_eye/color/compressed` | `sensor_msgs/msg/CompressedImage` | Right eye colour (passthrough) camera stream |
| `/quest3/left_eye/color/camera_info` | `sensor_msgs/msg/CameraInfo` | Left eye colour camera info |
| `/quest3/right_eye/color/camera_info` | `sensor_msgs/msg/CameraInfo` | Right eye colour camera info |
| `/quest3/left_eye/depth/compressed` | `sensor_msgs/msg/CompressedImage` | Left eye depth camera stream |
| `/quest3/right_eye/depth/compressed` | `sensor_msgs/msg/CompressedImage` | Right eye depth camera stream |


## License
The [Oculus License](unity/Q3toROS/OCULUS_LICENSE.txt) applies to the Meta SDK and related components.

Other scripts and assets in this project are licensed under the [MIT License](LICENSE).