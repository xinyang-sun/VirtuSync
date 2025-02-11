
<div align="center">
  
English Version | [中文版本](./README.zh-cn.md)

</div>

# VirtuSync

This is a 3D motion capture software developed using Unity, Mediapipe, and Python, supporting real-time tracking and synchronization of the face, eyes, and hands.




## Authors

- [@Xinyang-sun](https://www.github.com/xinyang-sun)


## Built With

<div align="center">

![Unity2020.3.21f1c1](https://img.shields.io/badge/Unity-2020.3.21f1c1-blue)
![Python 3.10](https://img.shields.io/badge/Python-3.10-blue)
![Mediapipe](https://img.shields.io/badge/Mediapipe-blue
)
[![license](https://img.shields.io/badge/license-MIT-blue
)](https://github.com/xinyang-sun/VirtuSync?tab=MIT-1-ov-file)

</div>


## Demo

[Demo video](https://www.bilibili.com/video/BV1NZrtYAEaK/?share_source=copy_web&vd_source=9b95709580179b5fcb8562c82ecdfa35)


## How to use

[Download](https://github.com/xinyang-sun/VirtuSync/releases) the latest package in releases

**1. Activate the camera**: Simply extract the archive and run `Tracking.exe/VirtuSync_Camera.exe`. Enter the `Port value`. The default is `5056`. Select the `camera number`. Here you need to try from 0 one by one until you find the camera you want to use. Click `Running` to turn on the camera.

**2. Activate 3D motion capture software**: run `Model_test.exe/VirtuSync_Model.exe`. Enter the same `Port value`, the default is `5056`. Click `Start` and the software will stream with the camera. Click `Model` to enter the modeling interface, and start your vtuber experience. 😉

**3. Expression Management System**: 
- Click on the button `Edit Expressions` to open the model expression configuration page. Select the expression. The `slider` is to control the expression amplitude. The slider bar controls the range of the expression, from 0 to 100, which is convenient for displaying expression combinations.
- On the right hand side, the expression binding buttons are `Alpha0` for `keyboard 0`. `Alpha1` for `keyboard 1` and so on.
- To change the emoji bindings, click on the input box and enter letters or numbers. Just click on the input box and enter the alphanumeric keys directly.
- The default reset button for emoticons is 0.
- Once everything is set, click Save to save it.
- **Note**: The keypad numeric keys and the keyboard numeric keys do not interoperate when binding. If you use the keypad numeric keys for binding, press the keypad numeric keys when using them.

**4. Modify the background color of the model**: Click `Background Color` to open the color palette. Click `Close` to close the palette after modifying the color.
## License
[MIT](https://github.com/xinyang-sun/VirtuSync/tree/main?tab=MIT-1-ov-file)
## Reference
- [VTuber-Python-Unity](https://github.com/mmmmmm44/VTuber-Python-Unity/tree/main)

- [head-pose-estimation](https://github.com/yinguobing/head-pose-estimation)

- [VTuber_Unity](https://github.com/kwea123/VTuber_Unity?tab=readme-ov-file)

- [Face-Pose-Estimation](https://github.com/jerryhouuu/Face-Yaw-Roll-Pitch-from-Pose-Estimation-using-OpenCV)

- [kalidokit](https://github.com/yeemachine/kalidokit?tab=readme-ov-file)

- [UnityHandTrackingWithMediapipe](https://github.com/TesseraktZero/UnityHandTrackingWithMediapipe)

- [Demonstration of models used：Nemu Nemu_黒パーカーVer（2ndモデル） - VRoid Hub](https://hub.vroid.com/en/characters/9150908110176006593/models/2059315200011240750)
