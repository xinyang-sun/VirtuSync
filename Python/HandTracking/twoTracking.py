import math
import cv2
import numpy as np
from cvzone.HandTrackingModule import HandDetector
#from cvzone.FaceMeshModule import FaceMeshDetector
import socket
import mediapipe as mp
import tkinter as tk
from tkinter import ttk
#import time
#from Kalman import Stabilizer

switch = False
port = 5056
videoNumber = 2
camera_list = []
index = 0
# Camera Availability Detection
while True:
    cap = cv2.VideoCapture(index, cv2.CAP_DSHOW)
    if not cap.read()[0]:
        break
    else:
        camera_name = f"Camera {index}"
        camera_list.append(camera_name)
    cap.release()
    index += 1

# Initialize the global model points variable
model_points = None

# 初始化眼睛存储坐标的列表
rEyePositions = []
lEyePositions = []
eyeFlag = 0

# Parameters
width, height = 1280, 720

# Hand and Face Detector
detector = HandDetector(maxHands=2, detectionCon=0.95)
#detectorFace = FaceMeshDetector(staticMode=False, maxFaces=1, minDetectionCon=0.5, minTrackCon=0.5)
# 初始化mediapipe面部网格检测器
mp_face_mesh = mp.solutions.face_mesh
face_mesh = mp_face_mesh.FaceMesh(refine_landmarks=True)

# 初始化用于绘制面部网格的绘图助手
mp_drawing = mp.solutions.drawing_utils
drawing_spec = mp_drawing.DrawingSpec(thickness=1, circle_radius=1)

# Communication
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# Calculate the real distance
x = [300, 245, 200, 170, 145, 130, 112, 103, 93, 87, 80, 75, 70, 67, 62, 59, 57]
y = [20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100]
coff = np.polyfit(x, y, 2)

# GUI
window = tk.Tk()
window.title('Tracking Hands')
window.geometry('400x200')

#PortUI
labelP = tk.Label(window, text='Port: ').place(x=150, y=0)
var = tk.IntVar()
var.set(port)
label = tk.Label(window, textvariable=var)
label.pack()
entery = tk.Entry(window, show=None)
entery.pack()


def insertPort():
    global port
    port = entery.get()
    port = int(port)
    var.set(port)

def on_camera_select(event):
    global videoNumber
    videoNumber = camera_dropdown.current()  # 获取当前选中的索引
    videoNumber = int(videoNumber)
    varVideo.set(videoNumber)  # 将选中的索引赋值到变量中

DebugNum = False
def on_check():
    global DebugNum
    if check_var.get():
        DebugNum = True
    else:
        DebugNum = False


# PortGUI
button = tk.Button(window,text='EnterPort', width=15, height=1, command=insertPort)
button.pack()

#调试信息UI
check_var = tk.BooleanVar()
checkbox = tk.Checkbutton(window, text="Debug", variable=check_var, command=on_check).place(x=325, y=74)

# VideoUI
labelVideo = tk.Label(window, text='VideoNumber: ').place(x=105, y=74)
varVideo = tk.IntVar()
varVideo.set(videoNumber)
labelV = tk.Label(window, textvariable=varVideo)
labelV.pack()
camera_dropdown = ttk.Combobox(window, values=camera_list, state="readonly")
camera_dropdown.bind("<<ComboboxSelected>>", on_camera_select)
camera_dropdown.pack(padx=10, pady=10)

class Stabilizer:
    """Using Kalman filter as a point stabilizer."""

    def __init__(self,
                 state_num=4,
                 measure_num=2,
                 cov_process=0.0001,
                 cov_measure=0.1):
        """Initialization"""
        # Currently we only support scalar and point, so check user input first.
        assert state_num == 4 or state_num == 2, "Only scalar and point supported, Check state_num please."

        # Store the parameters.
        self.state_num = state_num
        self.measure_num = measure_num

        # The filter itself.
        self.filter = cv2.KalmanFilter(state_num, measure_num, 0)

        # Store the state.
        self.state = np.zeros((state_num, 1), dtype=np.float32)

        # Store the measurement result.
        self.measurement = np.array((measure_num, 1), np.float32)

        # Store the prediction.
        self.prediction = np.zeros((state_num, 1), np.float32)

        # Kalman parameters setup for scalar.
        if self.measure_num == 1:
            self.filter.transitionMatrix = np.array([[1, 1],
                                                     [0, 1]], np.float32)

            self.filter.measurementMatrix = np.array([[1, 1]], np.float32)

            self.filter.processNoiseCov = np.array([[1, 0],
                                                    [0, 1]], np.float32) * cov_process

            self.filter.measurementNoiseCov = np.array(
                [[1]], np.float32) * cov_measure

        # Kalman parameters setup for point.
        if self.measure_num == 2:
            self.filter.transitionMatrix = np.array([[1, 0, 1, 0],
                                                     [0, 1, 0, 1],
                                                     [0, 0, 1, 0],
                                                     [0, 0, 0, 1]], np.float32)

            self.filter.measurementMatrix = np.array([[1, 0, 0, 0],
                                                      [0, 1, 0, 0]], np.float32)

            self.filter.processNoiseCov = np.array([[1, 0, 0, 0],
                                                    [0, 1, 0, 0],
                                                    [0, 0, 1, 0],
                                                    [0, 0, 0, 1]], np.float32) * cov_process

            self.filter.measurementNoiseCov = np.array([[1, 0],
                                                        [0, 1]], np.float32) * cov_measure

    def update(self, measurement):
        """Update the filter"""
        # Make kalman prediction
        self.prediction = self.filter.predict()

        # Get new measurement
        if self.measure_num == 1:
            self.measurement = np.array([[np.float32(measurement[0])]])
        else:
            self.measurement = np.array([[np.float32(measurement[0])],
                                         [np.float32(measurement[1])]])

        # Correct according to measurement
        self.filter.correct(self.measurement)

        # Update state value.
        self.state = self.filter.statePost

    def set_q_r(self, cov_process=0.1, cov_measure=0.001):
        """Set new value for processNoiseCov and measurementNoiseCov."""
        if self.measure_num == 1:
            self.filter.processNoiseCov = np.array([[1, 0],
                                                    [0, 1]], np.float32) * cov_process
            self.filter.measurementNoiseCov = np.array(
                [[1]], np.float32) * cov_measure
        else:
            self.filter.processNoiseCov = np.array([[1, 0, 0, 0],
                                                    [0, 1, 0, 0],
                                                    [0, 0, 1, 0],
                                                    [0, 0, 0, 1]], np.float32) * cov_process
            self.filter.measurementNoiseCov = np.array([[1, 0],
                                                        [0, 1]], np.float32) * cov_measure




def face_orientation(frame, landmarks):
    size = frame.shape  # (height, width, color_channel)

    # Use all 468 landmark points for image_points
    image_points = np.array([(landmark[0], landmark[1]) for landmark in landmarks[:468]], dtype="double")

    # Set the model points once if they haven't been initialized yet
    global model_points
    if model_points is None:
        model_points = np.array([(landmark[0], landmark[1], landmark[2]) for landmark in landmarks[:468]], dtype="double")
    '''
    image_points = np.array([
        (landmarks[4][0], landmarks[4][1]),  # Nose tip
        (landmarks[152][0], landmarks[152][1]),  # Chin
        (landmarks[33][0], landmarks[33][1]),  # Left eye left corner
        (landmarks[263][0], landmarks[263][1]),  # Right eye right corne
        (landmarks[61][0], landmarks[61][1]),  # Left Mouth corner
        (landmarks[291][0], landmarks[291][1]),  # Right mouth corner
        (landmarks[52][0], landmarks[52][1]),  # 左眉毛中心，示例点位，需要根据实际情况调整
        (landmarks[282][0], landmarks[282][1]),  # 右眉毛中心，示例点位，需要根据实际情况调整
        (landmarks[151][0], landmarks[151][1]),  # 额头中心，示例点位，需要根据实际情况调整
        (landmarks[50][0], landmarks[50][1]),  # 左脸颊，示例点位，需要根据实际情况调整
        (landmarks[280][0], landmarks[280][1])  # 右脸颊，示例点位，需要根据实际情况调整
    ], dtype="double")

    model_points = np.array([
        (0.0, 0.0, 0.0),  # Nose tip
        (0.0, -330.0, -65.0),  # Chin
        (-225.0, 170.0, -135.0),  # Left eye left corner
        (255.0, 170.0, -135.0),  # Right eye right corne
        (-150.0, -150.0, -125.0),  # Left Mouth corner
        (150.0, -150.0, -125.0),  # Right mouth corner
        (-130.0, 200.0, -125.0),  # 左眉毛中心
        (130.0, 200.0, -125.0),  # 右眉毛中心
        (0.0, 300.0, -125.0),  # 额头中心
        (-150.0, 0.0, -125.0),  # 左脸颊
        (150.0, 0.0, -125.0)  # 右脸颊
    ])
    '''
    '''    (0.0, -0.463170, 7.586580),  # Nose tip
    (0.0, -9.403378, 4.264492),  # Chin
    (-4.445859, 2.663991, 3.173422),  # Left eye left corner
    (4.445859, 2.663991, 3.173422),  # Right eye right corne
    (-2.456206, -4.342621, 4.283884),  # Left Mouth corner
    (2.456206, -4.342621, 4.283884),  # Right mouth corner
    '''

    # Camera internals
    #print(size[1])
    center = (size[1] / 2, size[0] / 2)
    focal_length = size[1]
    camera_matrix = np.array(
        [[focal_length, 0, center[0]],
         [0, focal_length, center[1]],
         [0, 0, 1]], dtype="double"
    )

    dist_coeffs = np.zeros((4, 1))  # Assuming no lens distortion
    (success, rotation_vector, translation_vector) = cv2.solvePnP(model_points, image_points, camera_matrix,
                                                                  dist_coeffs, flags=cv2.SOLVEPNP_ITERATIVE)

    axis = np.float32([[500, 0, 0],
                       [0, 500, 0],
                       [0, 0, 500]])

    imgpts, jac = cv2.projectPoints(np.array([(0.0, 0.0, 1000.0)]), rotation_vector, translation_vector, camera_matrix, dist_coeffs)
    modelpts, jac2 = cv2.projectPoints(model_points, rotation_vector, translation_vector, camera_matrix, dist_coeffs)
    rvec_matrix = cv2.Rodrigues(rotation_vector)[0]

    proj_matrix = np.hstack((rvec_matrix, translation_vector))
    eulerAngles = cv2.decomposeProjectionMatrix(proj_matrix)[6]

    pitch, yaw, roll = [math.radians(_) for _ in eulerAngles]

    pitch = math.degrees(math.asin(math.sin(pitch)))
    roll = -math.degrees(math.asin(math.sin(roll)))
    yaw = math.degrees(math.asin(math.sin(yaw)))
    #print((str(int(roll)), str(int(pitch)), str(int(yaw))))

    return imgpts, modelpts, ((yaw), (pitch), (roll)), (landmarks[4][0], landmarks[4][1])

# Eye Aspect Ratio
def calculate_ear(eye):
    # 计算垂直方向的两个距离
    A = np.linalg.norm(eye[1] - eye[3])
    B = np.linalg.norm(eye[2] - eye[4])

    # 计算水平方向的距离
    C = np.linalg.norm(eye[0] - eye[5])

    # 计算EAR
    ear = (A + B) / (2.0 * C)
    return ear

def calculate_eye_point_x(eyePoint):
    #计算水平x的距离
    A = np.linalg.norm(eyePoint[0] - eyePoint[1])
    B = np.linalg.norm(eyePoint[0] - eyePoint[2])
    C = B - A/2

    return C


def calculate_eye_point_y(eyePoint):
    # 计算水平x的距离
    A = np.linalg.norm(eyePoint[0] - eyePoint[1])
    B = np.linalg.norm(eyePoint[0] - eyePoint[2])
    C = B - A / 2

    return C

# 定义辅助函数
def clamp(value, min_value, max_value):
    return max(min_value, min(value, max_value))

def remap(value, in_min, in_max, out_min=0, out_max=1):
    return out_min + (out_max - out_min) * ((value - in_min) / (in_max - in_min))

# Mouth Aspect Ratio
def calculate_m_ratio(mouth):
    # 嘴唇垂直距离
    A = np.linalg.norm(mouth[0] - mouth[1])

    # 嘴唇水平距离
    B = np.linalg.norm(mouth[2] - mouth[3])

    #两眼睛内和外水平距离
    C = np.linalg.norm(mouth[4] - mouth[5])
    D = np.linalg.norm(mouth[6] - mouth[7])

    # 计算嘴巴比率
    ratio_y = A / C
    ratio_x = B / D

    ratio_y = remap(ratio_y, 0.15, 0.7)
    ratio_x = remap(ratio_x, 0.45, 0.9)
    ratio_x = (ratio_x - 0.3) * 2

    # 计算元音比例
    ratio_i = clamp(remap(ratio_x, 0, 1) * 2 * remap(ratio_y, 0.2, 0.7), 0, 1)
    ratio_a = ratio_y * 0.4 + ratio_y * (1 - ratio_i) * 0.6
    ratio_u = ratio_y * remap(1 - ratio_i, 0, 0.3) * 0.1
    ratio_e = remap(ratio_u, 0.2, 1) * (1 - ratio_i) * 0.3
    ratio_o = (1 - ratio_i) * remap(ratio_y, 0.3, 1) * 0.4

    return (
        ratio_x or 0,
        ratio_y or 0,
        ratio_a or 0,
        ratio_e or 0,
        ratio_i or 0,
        ratio_o or 0,
        ratio_u or 0,
    )

# Main program
def changeSwitch():
    # Webcam
    cap = cv2.VideoCapture(videoNumber, cv2.CAP_DSHOW)
    cap.set(3, width)
    cap.set(4, height)

    serverAddressPort = ("127.0.0.1", port)
    switch = True
    #每次启动后校准一次脸部坐标
    global model_points
    model_points = None

    while switch:
        success, img = cap.read()

        #Face

        leftEyePot=rightEyePot=leftEAR=rightEAR=mar=roll=pitch=yaw=0.0
        targetOffset = None
        face = []
        #img, faces = detectorFace.findFaceMesh(img, draw=True)# Draw
        # 将BGR图像转换为RGB图像
        rgb_frame = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)

        # 使用Face Mesh处理图像
        faces = face_mesh.process(rgb_frame)
        if faces.multi_face_landmarks:

            # Introduce scalar stabilizers for pose.
            pose_stabilizers = [Stabilizer(
                state_num=2,
                measure_num=1,
                cov_process=0.1,
                cov_measure=0.1) for _ in range(6)]

            # for eyes
            eyes_stabilizers = [Stabilizer(
                state_num=2,
                measure_num=1,
                cov_process=0.01,
                cov_measure=0.1) for _ in range(6)]

            # Loop through each detected face
            for face_landmarks in faces.multi_face_landmarks:
                for idx, landmarks in enumerate(face_landmarks.landmark):
                    x = int(landmarks.x * img.shape[1])
                    y = int(landmarks.y * img.shape[0])
                    z = landmarks.z
                    face.append([x, y, z])
                #print(face)

                # Get specific points for the eye
                leftEye = np.array([face[point][:2] for point in [33, 160, 158, 144, 153, 155]])
                rightEye = np.array([face[point][:2] for point in [362, 385, 387, 380, 373, 263]])

                # Get Eyes Point 以眼框作为基准。脸部转动会失效
                lEyePotx = np.array([face[point][:2] for point in [33, 155, 468]])
                leftEyePotx = calculate_eye_point_x(lEyePotx)
                lEyePoty = np.array([face[point][:2] for point in [159, 145, 468]])
                leftEyePoty = calculate_eye_point_y(lEyePoty)
                leftEyePot = [leftEyePotx, leftEyePoty]

                rEyePotx = np.array([face[point][:2] for point in [362, 263, 473]])
                rightEyePotx = calculate_eye_point_x(rEyePotx)
                rEyePoty = np.array([face[point][:2] for point in [386, 374, 473]])
                rightEyePoty = calculate_eye_point_y(rEyePoty)
                rightEyePot = [rightEyePotx, rightEyePoty]

                #获取5次眼睛坐标平均后作为标准 然后计算中间值再计算前一刻和下一刻的差值，配合Unity中vrm自带脚本控制眼睛变化 受脸部转动影响小
                global eyeFlag, avgLEyePos, avgREyePos, oldLEyePot, oldREyePot
                if eyeFlag < 5:
                    lEyePot = np.array(face[468][:2])
                    rEyePot = np.array(face[473][:2])
                    lEyePositions.append(lEyePot)
                    rEyePositions.append(rEyePot)
                    avgLEyePos = np.mean(lEyePositions, axis=0)
                    avgREyePos = np.mean(rEyePositions, axis=0)
                    oldLEyePot = avgLEyePos
                    oldREyePot = avgREyePos
                    eyeFlag += 1
                newLEyePot  = np.array(face[468][:2])
                newREyePot = np.array(face[473][:2])
                lEyeOffset = newLEyePot - oldLEyePot
                rEyeOffset = newREyePot - oldREyePot
                oldLEyePot = newLEyePot
                oldREyePot = newREyePot
                targetOffset = (lEyeOffset + rEyeOffset) / 2
                #print("Right Eye Offset:", rEyeOffset, avgREyePos, newREyePot)
                #print("Left Eye Offset:", lEyeOffset, avgLEyePos, newLEyePot)

                # 计算两只眼睛的EAR
                leftEAR = calculate_ear(leftEye)
                rightEAR = calculate_ear(rightEye)

                # Get specific points for the mouth
                mouth = np.array([face[point][:2] for point in [13, 14, 61, 291, 133, 362, 130, 263]])

                #计算Mouth ratio
                ratioX, ratioY, ratioA, ratioE, ratioI, ratioO, ratioU = calculate_m_ratio(mouth)
                ratioM = f"{ratioX:.3f};{ratioY:.3f};{ratioA:.3f};{ratioE:.3f};{ratioI:.3f};{ratioO:.3f};{ratioU:.3f}"


                # Take the roll, pitch and yaw of the face
                imgpts, modelpts, rotate_degree, nose = face_orientation(img, face)

                # Stabilize the pose.
                steady_pose = []
                pose_np = np.array(rotate_degree).flatten()
                for value, ps_stb in zip(pose_np, pose_stabilizers):
                    ps_stb.update([value])
                    steady_pose.append(ps_stb.state[0])
                steady_pose = np.reshape(steady_pose, (-1, 3))

                roll = steady_pose[0][2]
                pitch = steady_pose[0][1]
                yaw = steady_pose[0][0]

                #不使用滤波直接输出
                #roll = rotate_degree[2]
                #pitch = rotate_degree[1]
                #yaw = rotate_degree[0]

                # Stabilize the eyes and MAR.
                pose_eye = [leftEAR, rightEAR, targetOffset[0], targetOffset[1]]
                steady_pose_eye = []
                for value, ps_stb in zip(pose_eye, eyes_stabilizers):
                    ps_stb.update([value])
                    steady_pose_eye.append(ps_stb.state[0])

                #leftEAR = steady_pose_eye[0]
                #rightEAR = steady_pose_eye[1]
                targetOffset[0] = steady_pose_eye[2]
                targetOffset[1] = steady_pose_eye[3]

                if DebugNum:
                    idList = [4, 152, 33, 263, 61, 291, 52, 282, 151, 50, 280, 468, 473]
                    for id in idList:
                        x, y, _ = face[id]
                        cv2.circle(img, (x, y), 5, (255, 0, 255), cv2.FILLED)
                    # Display roll, pitch and yaw values on the image
                    cv2.putText(img, f"Roll: {roll}", (20, 50), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
                    cv2.putText(img, f"Pitch: {pitch}", (20, 100), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
                    cv2.putText(img, f"Yaw: {yaw}", (20, 150), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)

                    # 标记左眼和右眼的特征点
                    for point in leftEye:
                        cv2.circle(img, (int(point[0]), int(point[1])), 2, (0, 255, 0), -1)
                    for point in rightEye:
                        cv2.circle(img, (int(point[0]), int(point[1])), 2, (0, 255, 0), -1)

                    #显示EAR
                    cv2.putText(img, f"LEAR: {leftEAR}", (20, 200), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
                    cv2.putText(img, f"REAR: {rightEAR}", (20, 250), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)

                    # 显示MAR
                    cv2.putText(img, f"MAR: {ratioM}", (20, 300), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
                    for point in mouth:
                        cv2.circle(img, (int(point[0]), int(point[1])), 2, (0, 255, 0), -1)

                    # 显示眼睛坐标
                    cv2.putText(img, f"TargetX: {targetOffset[0]}", (20, 350), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
                    cv2.putText(img, f"TargetY: {targetOffset[1]}", (20, 400), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)




        # Hands
        hands, img = detector.findHands(img)  # Draw
        data = []
        lmList = []
        lmList1 = []
        HandLeft = []
        HandRight = []

        # MAR EAR加入到传输数据中
        if leftEAR != 0 and rightEAR != 0 and ratioM != 0:
            data.extend([leftEAR, rightEAR, ratioM])
            data.extend(["LEAR, REAR, MAR"])

        # 眼珠子坐标加入到传输数据中
        if np.all(targetOffset) != None:
            data.extend([targetOffset[0], targetOffset[1]])
            data.extend(["leftEyePot, rightEyePot"])
        #print("rPot" + str(rightEyePot), "lPot" + str(leftEyePot))

        # face的roll pitch yaw加入到传输数据中
        if roll != 0 and pitch != 0 and yaw != 0:
            data.extend([roll, pitch, yaw])
            data.extend(["roll, pitch, yaw"])

        # Landmark values

        if hands:
            hand = hands[0]  # get first hand detected
            handType = hand['type']  # Get the hand type
            lmList.append(handType)
            lmList.append(hand['lmList'])  # Get the landmark list

            if lmList[0] == "Left" and len(hands) == 1:
                HandLeft = hand['lmList']
            else:
                HandRight = hand['lmList']

            if len(hands) == 2:
                hand1 = hands[1]
                hand1Type = hand1['type']
                lmList1.append(hand1Type)
                lmList1.append(hand1['lmList'])

                if lmList1[0] == "Left":
                    HandLeft = hand1['lmList']
                    HandRight = hand['lmList']
                else:
                    HandLeft = hand['lmList']
                    HandRight = hand1['lmList']

            if len(HandLeft) != 0 and len(HandRight) != 0:
                Lx1, Ly1 = HandLeft[5][:2]
                Lx2, Ly2 = HandLeft[17][:2]
                Ldistance = int(math.sqrt((Ly2 - Ly1) ** 2 + (Lx2 - Lx1) ** 2))
                A, B, C = coff
                LdistanceCM = A * Ldistance ** 2 + B * Ldistance + C

                Rx1, Ry1 = HandRight[5][:2]
                Rx2, Ry2 = HandRight[17][:2]
                Rdistance = int(math.sqrt((Ry2 - Ry1) ** 2 + (Rx2 - Rx1) ** 2))
                A, B, C = coff
                RdistanceCM = A * Rdistance ** 2 + B * Rdistance + C
            elif len(HandLeft) != 0 and len(HandRight) == 0:
                Lx1, Ly1 = HandLeft[5][:2]
                Lx2, Ly2 = HandLeft[17][:2]
                Ldistance = int(math.sqrt((Ly2 - Ly1) ** 2 + (Lx2 - Lx1) ** 2))
                A, B, C = coff
                LdistanceCM = A * Ldistance ** 2 + B * Ldistance + C
            elif len(HandLeft) == 0 and len(HandRight) != 0:
                Rx1, Ry1 = HandRight[5][:2]
                Rx2, Ry2 = HandRight[17][:2]
                Rdistance = int(math.sqrt((Ry2 - Ry1) ** 2 + (Rx2 - Rx1) ** 2))
                A, B, C = coff
                RdistanceCM = A * Rdistance ** 2 + B * Rdistance + C
            # print(LdistanceCM, RdistanceCM)

            if len(HandLeft) != 0:
                data.extend(["Left"])
            for lm in HandLeft:
                data.extend([lm[0], height - lm[1], lm[2], LdistanceCM])
            if len(HandRight) != 0:
                data.extend(["Right"])
            for lm1 in HandRight:
                data.extend([lm1[0], height - lm1[1], lm1[2], RdistanceCM])

        if faces or hands:
            print(data)
            sock.sendto(str.encode(str(data)), serverAddressPort)

        img = cv2.resize(img, (0, 0), None, 0.5, 0.5)
        cv2.imshow("Image", img)
        cv2.waitKey(1)

        if cv2.getWindowProperty('Image', cv2.WND_PROP_VISIBLE) < 1: # Close the window
            switch = False


# VideoStart
button = tk.Button(window, text='Running', width=15, height=2, command=changeSwitch)
button.pack()
window.mainloop()
