
<p align="center">
[English Version](./README.md) | 中文版本
</p>

# VirtuSync

这是一款使用 Unity、Mediapipe 和 Python 开发的 3D 动作捕捉软件，支持面部、眼睛和手部的实时跟踪和同步。




## 作者

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


## 如何使用

[下载](https://github.com/xinyang-sun/VirtuSync/releases)最新的压缩包在releases中。

**1. 启动相机**: 解压后点击运行`Tracking.exe`。输入`Port值`。默认是`5056`。选择`摄像头number`。在这需要从0开始一个个试出你想调用的摄像头。点击`Running`就能启动摄像头了。

**2. 启动3D动捕软件**: 运行`Model_test.exe`。输入同样的`Port值`，默认是`5056`。点击`Start`后软件会和摄像头进行串流连接。点击`Model`就能开始使用模型来直播啦。😉
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
