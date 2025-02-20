
<div align="center">
  
[English Version](./README.md) | 中文版本

</div>

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
**视频演示**
[Demo video](https://www.bilibili.com/video/BV1NZrtYAEaK/?share_source=copy_web&vd_source=9b95709580179b5fcb8562c82ecdfa35)
**GIF展示**
![相机控制](./Unity/face%20and%20hand%20track/GIF/camera_cn.gif)
![面捕功能](./Unity/face%20and%20hand%20track/GIF/face_cn.gif)
![手捕功能](./Unity/face%20and%20hand%20track/GIF/hand_cn.gif)


## 如何使用

[下载](https://github.com/xinyang-sun/VirtuSync/releases)最新的压缩包在releases中。

**1. 启动相机**: 解压后点击运行`Tracking.exe/VirtuSync_Camera.exe`。输入`Port值`。默认是`5056`。选择`摄像头number`。在这需要从0开始一个个试出你想调用的摄像头。点击`Running`就能启动摄像头了。

**2. 启动3D动捕软件**: 运行`Model_test.exe/VirtuSync_Model.exe`。输入同样的`Port值`，默认是`5056`。点击`Start`后软件会和摄像头进行串流连接。点击`Model`就能开始使用模型来直播啦。😉

**3. 模型表情控制**: 
- 单击按钮`Edit Expressions`打开模型表情配置页面。下拉框选择表情。`滑动条`是控制表情幅度。从0到100.方便进行表情组合展示。
- 右边表情绑定按键中`Alpha0`代表键盘0按键控制。`Alpha1`代表键盘1按键控制.以此类推。
- 更改表情绑定按键。点击输入框直接输入字母或数字按键即可。
- 默认重置表情按键为`键盘0`按键。可更改为其他按键。
- 一切设置好后，点击`Save`保存。
- 使用时按下对应按键即可，可叠加表情来进行组合。最后按重置表情按键(默认键盘0按键)来清空表情。
- **注意**：小键盘数字键和键盘数字键在绑定时不互通。如果使用小键盘数字键进行绑定，使用时请按小键盘数字键。

**4. 模型背景颜色修改**: 点击`Background Color`打开调色盘。修改完颜色后点击`Close`关闭。
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
