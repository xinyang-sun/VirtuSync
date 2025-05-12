using System.Diagnostics;
using System;
using System.Data.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace landmarktest
{
    // 用于配置角度与骨骼的映射
    [System.Serializable]
    public class HandJointMapping
    {
        public string angleName;       // 角度名称，如 "thumb_MCP"
        public Transform jointTransform; // 对应的骨骼 Transform
        //public Vector3 rotationAxis = Vector3.forward; // 默认绕 z 轴旋转
        public Vector3 rotationOffset = Vector3.zero;  // 初始补偿旋转（角度值）
        [HideInInspector]
        public Quaternion initialRotation; // 用于缓存初始 localRotation
        public Quaternion currentRotation; // 用于 Lerp 平滑更新的当前旋转值
    }
    public class HandTracking : MonoBehaviour
    {
        // Start is called before the first frame update
        public UDPReceive udpReceive;
        //public List<GameObject> handPoints;

        // 下面两个映射列表在 Inspector 中配置，自动设置新模型时只需配置这两个列表
        public List<HandJointMapping> leftHandMappings = new List<HandJointMapping>();
        public List<HandJointMapping> rightHandMappings = new List<HandJointMapping>();
        // 用于显示调试信息
        private Dictionary<string, float> currentAngles = new Dictionary<string, float>();
        public Animator animator;

        // [Header("骨骼引用 (按层级顺序)")]
        // public Transform shoulder;   // J_Bip_R_Shoulder
        // public Transform upperArm;   // J_Bip_R_UpperArm
        // public Transform lowerArm;   // J_Bip_R_LowerArm
        // public Transform hand;       // J_Bip_R_Hand

        // [Header("要到达的目标")]
        // public Transform target;     // 让手到达的世界坐标目标

        // [Header("可选：朝向修正 (根据模型的默认朝向调整)")]
        // // 这些是用来表示当骨骼的 localRotation = Quaternion.identity 时，骨骼实际上指向哪个轴
        // // 大多数 3D 软件里，角色的 UpperArm 可能是沿 X 或 Z 轴指向前方，需要自己实验。
        // public Vector3 upperArmLocalAxis = Vector3.right;   // 假设UpperArm在T姿势时指向 +X
        // public Vector3 lowerArmLocalAxis = Vector3.right;   // LowerArm同理
        // public Vector3 handLocalAxis = Vector3.right;   // 手掌

        // [Header("是否自动更新")]
        // public bool solveIKInUpdate = false;

        // // 记录上臂与前臂长度
        // private float upperArmLength;
        // private float lowerArmLength;

        // // 缓存初始的父级旋转，用于保持相对旋转
        // private Quaternion shoulderStartRot;
        // private Quaternion upperArmStartRot;
        // private Quaternion lowerArmStartRot;


        [SerializeField]
        //private float thumbModelLength = 0.03f;
        private float scale;
        private DepthCalibrator depthCalibrator = new DepthCalibrator(-0.0719f, 0.439f);
        private Vector3 initialRightPosition = new Vector3(0.261000007f, 1.09000003f, 0.171000004f);
        private Vector3 initialLeftPosition = new Vector3(-0.261000007f, 0.995000005f, 0.171000004f);
        public Vector3 initialRightRotationEuler = new Vector3(0, 270, 90);
        public Vector3 initialLeftRotationEuler = new Vector3(0, 90, 270);
        private Vector3 defaultLeftPosition = new Vector3(-0.273999989f, 0.91900003f, -0.0439999998f);//-0.194000006f, 0.818000019f, 0.075000003f
        private Vector3 defaultRightPosition = new Vector3(0.273999989f, 0.91900003f, -0.0439999998f);//0.238000005f, 0.836000025f, 0.0790000036f
        private Vector3 defaultLeftRotationEuler = new Vector3(0f, 0f, 60.0000038f);
        private Vector3 defaultRightRotationEuler = new Vector3(0f, 0f, 300f);

        private Vector3 initialRightOffset;
        private Vector3 initialLeftOffset;
        //private Vector3 direction;
        private bool isRightInitialized = false;
        private bool isLeftInitialized = false;
        private float noHandTimer = 0f;

        //private TransformLink[] transformLinkers;

        // 存储解析到的手部 21 个关键点数据
        private List<Vector3> leftHandLandmarks = new List<Vector3>();
        private List<Vector3> rightHandLandmarks = new List<Vector3>();

        // 用于平滑腕关节旋转的变量
        private Quaternion currentWristRotation = Quaternion.identity;

        public string LinkType = "None";
        //float RzD = 0;
        //float LzD = 0;
        float smoothFactor = 5.0f;
        void Awake()
        {
            //transformLinkers = GetComponentsInChildren<TransformLink>();
        }
        void Start()
        {
            // —— 自动查找 M 下的子模型并赋值 vrmModel —— 
            if (animator == null)
            {
                var mRoot = GameObject.Find("M");
                if (mRoot != null)
                {
                    animator = mRoot.GetComponentInChildren<Animator>(true);
                }
            }
            //defaultLeftPosition = animator.GetBoneTransform(HumanBodyBones.LeftHand).localPosition;
            //defaultRightPosition = animator.GetBoneTransform(HumanBodyBones.RightHand).localPosition;
            //animator = GetComponent<Animator>();
            AutoBindHandBones();
            // 自动获取并缓存手部骨骼初始 localRotation
            // 注意：Humanoid Avatar 自动映射包括以下骨骼（以左手为例）
            // 对于拇指：
            CacheBoneMapping(leftHandMappings);
            CacheBoneMapping(rightHandMappings);
            // 用当前模型高度重新设定四个锚点
            RecalculateAnchors();

            // // 记录各段长度（初始 T Pose 时）
            // upperArmLength = Vector3.Distance(upperArm.position, lowerArm.position);
            // lowerArmLength = Vector3.Distance(lowerArm.position, hand.position);

            // // 记录初始旋转
            // shoulderStartRot = shoulder.rotation;
            // upperArmStartRot = upperArm.localRotation;
            // lowerArmStartRot = lowerArm.localRotation;
        }

        void Update()
        {
            // foreach (var linker in transformLinkers)
            // {
            //     linker.UpdateTransform();
            // }
            // Debug.Log("handPoints.Count:" + handPoints.Count);
            if (string.IsNullOrEmpty(udpReceive.data)) return;
            ParseData(udpReceive.data);
            //UpdateHandTarget();
        }

        private void AutoBindHandBones()
        {
            // 清空旧数据
            leftHandMappings.Clear();
            rightHandMappings.Clear();

            // 左手映射
            leftHandMappings.Add(new HandJointMapping { angleName = "thumb_MCP", jointTransform = animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate) });
            leftHandMappings.Add(new HandJointMapping { angleName = "thumb_IP", jointTransform = animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal) });
            leftHandMappings.Add(new HandJointMapping { angleName = "index_MCP", jointTransform = animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal) });
            leftHandMappings.Add(new HandJointMapping { angleName = "index_PIP", jointTransform = animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate) });
            leftHandMappings.Add(new HandJointMapping { angleName = "index_DIP", jointTransform = animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal) });
            leftHandMappings.Add(new HandJointMapping { angleName = "middle_MCP", jointTransform = animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal) });
            leftHandMappings.Add(new HandJointMapping { angleName = "middle_PIP", jointTransform = animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate) });
            leftHandMappings.Add(new HandJointMapping { angleName = "middle_DIP", jointTransform = animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal) });
            leftHandMappings.Add(new HandJointMapping { angleName = "ring_MCP", jointTransform = animator.GetBoneTransform(HumanBodyBones.LeftRingProximal) });
            leftHandMappings.Add(new HandJointMapping { angleName = "ring_PIP", jointTransform = animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate) });
            leftHandMappings.Add(new HandJointMapping { angleName = "ring_DIP", jointTransform = animator.GetBoneTransform(HumanBodyBones.LeftRingDistal) });
            leftHandMappings.Add(new HandJointMapping { angleName = "pinky_MCP", jointTransform = animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal) });
            leftHandMappings.Add(new HandJointMapping { angleName = "pinky_PIP", jointTransform = animator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate) });
            leftHandMappings.Add(new HandJointMapping { angleName = "pinky_DIP", jointTransform = animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal) });
            // 手指展开角：这里简单采用各指 Proximal 作为目标（可根据实际需要调整）
            leftHandMappings.Add(new HandJointMapping { angleName = "spread_index_middle", jointTransform = animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal) });
            leftHandMappings.Add(new HandJointMapping { angleName = "spread_ring_middle", jointTransform = animator.GetBoneTransform(HumanBodyBones.LeftRingProximal) });
            leftHandMappings.Add(new HandJointMapping { angleName = "spread_pinky_middle", jointTransform = animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal) });

            // 右手映射（同理，前缀 "Right"）
            rightHandMappings.Add(new HandJointMapping { angleName = "thumb_MCP", jointTransform = animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate) });
            rightHandMappings.Add(new HandJointMapping { angleName = "thumb_IP", jointTransform = animator.GetBoneTransform(HumanBodyBones.RightThumbDistal) });
            rightHandMappings.Add(new HandJointMapping { angleName = "index_MCP", jointTransform = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal) });
            rightHandMappings.Add(new HandJointMapping { angleName = "index_PIP", jointTransform = animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate) });
            rightHandMappings.Add(new HandJointMapping { angleName = "index_DIP", jointTransform = animator.GetBoneTransform(HumanBodyBones.RightIndexDistal) });
            rightHandMappings.Add(new HandJointMapping { angleName = "middle_MCP", jointTransform = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal) });
            rightHandMappings.Add(new HandJointMapping { angleName = "middle_PIP", jointTransform = animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate) });
            rightHandMappings.Add(new HandJointMapping { angleName = "middle_DIP", jointTransform = animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal) });
            rightHandMappings.Add(new HandJointMapping { angleName = "ring_MCP", jointTransform = animator.GetBoneTransform(HumanBodyBones.RightRingProximal) });
            rightHandMappings.Add(new HandJointMapping { angleName = "ring_PIP", jointTransform = animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate) });
            rightHandMappings.Add(new HandJointMapping { angleName = "ring_DIP", jointTransform = animator.GetBoneTransform(HumanBodyBones.RightRingDistal) });
            rightHandMappings.Add(new HandJointMapping { angleName = "pinky_MCP", jointTransform = animator.GetBoneTransform(HumanBodyBones.RightLittleProximal) });
            rightHandMappings.Add(new HandJointMapping { angleName = "pinky_PIP", jointTransform = animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate) });
            rightHandMappings.Add(new HandJointMapping { angleName = "pinky_DIP", jointTransform = animator.GetBoneTransform(HumanBodyBones.RightLittleDistal) });
            rightHandMappings.Add(new HandJointMapping { angleName = "spread_index_middle", jointTransform = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal) });
            rightHandMappings.Add(new HandJointMapping { angleName = "spread_ring_middle", jointTransform = animator.GetBoneTransform(HumanBodyBones.RightRingProximal) });
            rightHandMappings.Add(new HandJointMapping { angleName = "spread_pinky_middle", jointTransform = animator.GetBoneTransform(HumanBodyBones.RightLittleProximal) });
        }

        private void CacheBoneMapping(List<HandJointMapping> mappings)
        {
            foreach (var mapping in mappings)
            {
                if (mapping.jointTransform != null)
                {
                    mapping.initialRotation = mapping.jointTransform.localRotation;
                    mapping.currentRotation = mapping.initialRotation;
                }
            }
        }

        float GetLocalY(HumanBodyBones bone, float fallback)
        {
            var t = animator.GetBoneTransform(bone);        // 运行时取骨骼
            return t ? t.position.y : fallback;
        }
        void RecalculateAnchors()
        {
            float UpperChestY = GetLocalY(HumanBodyBones.UpperChest,
                         GetLocalY(HumanBodyBones.Spine, initialRightPosition.y));

            float HipsY = GetLocalY(HumanBodyBones.Hips, defaultRightPosition.y);

            initialRightPosition.y = UpperChestY;
            initialLeftPosition.y = UpperChestY;
            defaultRightPosition.y = HipsY-0.01f;
            defaultLeftPosition.y = HipsY-0.01f;
            UnityEngine.Debug.Log("initialRightPosition.y:" + initialRightPosition.y);
            UnityEngine.Debug.Log("defaultRightPosition.y:" + defaultRightPosition.y);
            UnityEngine.Debug.Log("chestY:" + UpperChestY);
            UnityEngine.Debug.Log("spineY:" + HipsY);
        }

        public void ResetHandPositions()
        {
            isRightInitialized = false;
            isLeftInitialized = false;

            // 将 initialOffset 重置为零向量
            initialRightOffset = Vector3.zero;
            initialLeftOffset = Vector3.zero;

            // 立即将手的位置设置为初始位置
            if (LinkType == "Right")
            {
                this.transform.localPosition = initialRightPosition;
                this.transform.localRotation = Quaternion.Euler(initialRightRotationEuler);
            }
            else if (LinkType == "Left")
            {
                this.transform.localPosition = initialLeftPosition;
                this.transform.localRotation = Quaternion.Euler(initialLeftRotationEuler);
            }
        }

        private void UpdateHandTarget()
        {
            if (LinkType == "Left")
            {
                if (leftHandLandmarks != null && leftHandLandmarks.Count > 0)
                {
                    transform.localPosition = initialLeftPosition;
                    transform.localRotation = Quaternion.Euler(initialLeftRotationEuler);
                }
                else
                {
                    transform.localPosition = defaultLeftPosition;
                    transform.localRotation = Quaternion.Euler(defaultLeftRotationEuler);
                }
            }
            else if (LinkType == "Right")
            {
                if (rightHandLandmarks != null && rightHandLandmarks.Count > 0)
                {
                    transform.localPosition = initialRightPosition;
                    transform.localRotation = Quaternion.Euler(initialRightRotationEuler);
                }
                else
                {
                    transform.localPosition = defaultRightPosition;
                    transform.localRotation = Quaternion.Euler(defaultRightRotationEuler);
                }
            }
        }

        // private void ParseData(string data)
        // {
        //     // Remove the part of the data up to "roll, pitch, yaw"
        //     if (data.Contains("roll, pitch, yaw"))
        //     {
        //         int indexStart = data.LastIndexOf("roll, pitch, yaw") + 19;
        //         data = data.Substring(indexStart).TrimEnd(new char[] { ',' });
        //         Debug.Log("data: " + data);
        //     }

        //     // Handle different data ordering
        //     string leftData = ExtractHandData(data, "Left");
        //     string rightData = ExtractHandData(data, "Right");

        //     if (!string.IsNullOrEmpty(leftData))
        //     {
        //         leftData = leftData.Remove(0, 3);
        //         leftData = leftData.Remove(leftData.Length - 3, 3);
        //         Debug.Log("leftData:" + leftData);
        //         UpdateHandPoints(leftData.Split(','), "Left");
        //         UpdateHand("Left", leftData.Split(','), ref isLeftInitialized, ref initialLeftOffset);
        //     }

        //     if (!string.IsNullOrEmpty(rightData))
        //     {
        //         rightData = rightData.Remove(0, 3);
        //         rightData = rightData.Remove(rightData.Length - 1, 1);
        //         Debug.Log("rightData:" + rightData);
        //         UpdateHandPoints(rightData.Split(','), "Right");
        //         UpdateHand("Right", rightData.Split(','), ref isRightInitialized, ref initialRightOffset);
        //     }
        //     UpdateWristRotation();
        // }

        private void ParseData(string data)
        {
            //Remove the part of the data up to "roll, pitch, yaw"
            if (data.Contains("roll, pitch, yaw"))
            {
                int indexStart = data.LastIndexOf("roll, pitch, yaw") + 19;
                if (indexStart < data.Length)
                {
                    data = data.Substring(indexStart).TrimEnd(new char[] { ',' });
                }

            }

            string leftAnglesData = ExtractHandAngles(data, "Left");
            string rightAnglesData = ExtractHandAngles(data, "Right");

            if (LinkType == "Left" && !string.IsNullOrEmpty(leftAnglesData))
            {
                noHandTimer = 0f;
                leftAnglesData = leftAnglesData.Remove(0, 3);
                leftAnglesData = leftAnglesData.Remove(leftAnglesData.Length - 3, 3);
                string[] angleStrings = leftAnglesData.Split(',');
                if (angleStrings.Length >= 17)
                {
                    float[] angles = new float[17];
                    for (int i = 0; i < 17; i++)
                    {
                        float val;
                        float.TryParse(angleStrings[i], out val);
                        angles[i] = val;
                        currentAngles[GetMappingName(i)] = val; // 保存用于显示
                    }
                    UpdateHandJoints(angles, leftHandMappings);
                    if (angleStrings.Length >= 17 + 63)
                    {
                        leftHandLandmarks.Clear();
                        for (int i = 17; i < 17 + 63; i += 3)
                        {
                            float x, y, z;
                            float.TryParse(angleStrings[i], out x);
                            float.TryParse(angleStrings[i + 1], out y);
                            float.TryParse(angleStrings[i + 2], out z);
                            leftHandLandmarks.Add(new Vector3(x, y, z));
                        }
                        transform.localPosition = Vector3.Lerp(transform.localPosition, initialLeftPosition, Time.deltaTime * smoothFactor);
                        transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(initialLeftRotationEuler), Time.deltaTime * smoothFactor);
                        UpdateHand(LinkType, leftHandLandmarks, ref isLeftInitialized, ref initialLeftOffset);
                        //UnityEngine.Debug.Log("Left hand landmarks count: " + leftHandLandmarks.Count);
                    }
                }
                UpdateWristRotation();
            }
            else if (LinkType == "Right" && !string.IsNullOrEmpty(rightAnglesData))
            {
                noHandTimer = 0f;
                rightAnglesData = rightAnglesData.Remove(0, 3);
                rightAnglesData = rightAnglesData.Remove(rightAnglesData.Length - 1, 1);
                string[] angleStrings = rightAnglesData.Split(',');
                if (angleStrings.Length >= 17)
                {
                    float[] angles = new float[17];
                    for (int i = 0; i < 17; i++)
                    {
                        float val;
                        float.TryParse(angleStrings[i], out val);
                        angles[i] = val;
                        currentAngles[GetMappingName(i)] = val;
                    }
                    UpdateHandJoints(angles, rightHandMappings);
                    if (angleStrings.Length >= 17 + 63)
                    {
                        rightHandLandmarks.Clear();
                        for (int i = 17; i < 17 + 63; i += 3)
                        {
                            float x, y, z;
                            float.TryParse(angleStrings[i], out x);
                            float.TryParse(angleStrings[i + 1], out y);
                            float.TryParse(angleStrings[i + 2], out z);
                            rightHandLandmarks.Add(new Vector3(x, y, z));
                        }
                        transform.localPosition = Vector3.Lerp(transform.localPosition, initialRightPosition, Time.deltaTime * smoothFactor);
                        transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(initialRightRotationEuler), Time.deltaTime * smoothFactor);
                        UpdateHand(LinkType, rightHandLandmarks, ref isRightInitialized, ref initialRightOffset);
                        //UnityEngine.Debug.Log("Right hand landmarks count: " + rightHandLandmarks.Count);
                    }

                }
                UpdateWristRotation();
            }
            else
            {
                noHandTimer += Time.deltaTime;
                if (noHandTimer >= 1f)
                {
                    if (LinkType == "Left")
                    {
                        // 没有检测到手，则设置为 defaultLeftPosition 和 defaultLeftRotationEuler
                        transform.localPosition = Vector3.Lerp(transform.localPosition, defaultLeftPosition, Time.deltaTime * smoothFactor);
                        transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(defaultLeftRotationEuler), Time.deltaTime * smoothFactor);
                    }
                    else if (LinkType == "Right")
                    {
                        // 没有检测到右手
                        transform.localPosition = Vector3.Lerp(transform.localPosition, defaultRightPosition, Time.deltaTime * smoothFactor);
                        transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(defaultRightRotationEuler), Time.deltaTime * smoothFactor);
                    }
                }

            }
            //UpdateWristRotation();
        }


        // private string ExtractHandData(string data, string handType)
        // {
        //     int start = data.IndexOf(handType);
        //     if (start == -1) return null; // Hand data not found

        //     start += handType.Length;
        //     int end = data.IndexOf(handType == "Left" ? "Right" : "Left", start); // Look for the start of the other hand data
        //     if (end == -1) end = data.Length; // If the other hand data isn't found, assume end of data

        //     return data.Substring(start, end - start).Trim();
        // }

        private string ExtractHandAngles(string data, string tag)
        {
            int index = data.IndexOf(tag);
            if (index == -1) return null;
            index += tag.Length;
            // 假设数据以下一个标签或结尾结束，这里简单处理
            int endIndex = data.IndexOf(tag == "Left" ? "Right" : "Left", index);
            if (endIndex == -1) endIndex = data.Length;
            return data.Substring(index, endIndex - index).Trim();
        }


        // private void UpdateHandPoints(string[] points, string handType)
        // {
        //     if (LinkType == handType)
        //     {
        //         for (int i = 1; i < handPoints.Count; i++)
        //         {
        //             if (i * 4 + 2 >= points.Length) break; // Prevent index out of bounds
        //             float x = float.Parse(points[i * 4]) - float.Parse(points[0]);
        //             float y = float.Parse(points[i * 4 + 1]) - float.Parse(points[1]);
        //             float z = float.Parse(points[i * 4 + 2]) - float.Parse(points[2]);
        //             float parsedValue = float.Parse(points[i * 4 + 3]);
        //             direction = new Vector3(x, y, z);
        //             direction.Normalize();

        //             if (parsedValue != 0)
        //             {
        //                 if (handType == "Left")
        //                 {
        //                     LzD = parsedValue; // Update LzD for Left hand
        //                 }
        //                 else if (handType == "Right")
        //                 {
        //                     RzD = parsedValue; // Update RzD for Right hand
        //                 }
        //             }

        //             Vector3 newPosition = new Vector3(x, y, z);
        //             handPoints[i].transform.localPosition = Vector3.Lerp(handPoints[i].transform.localPosition, newPosition, Time.deltaTime * smoothFactor);
        //         }

        //     }
        // }

        private void UpdateHandJoints(float[] angles, List<HandJointMapping> mappings)
        {
            // 映射顺序：
            // 0: thumb_MCP, 1: thumb_IP,
            // 2: index_MCP, 3: index_PIP, 4: index_DIP,
            // 5: middle_MCP, 6: middle_PIP, 7: middle_DIP,
            // 8: ring_MCP, 9: ring_PIP, 10: ring_DIP,
            // 11: pinky_MCP, 12: pinky_PIP, 13: pinky_DIP,
            // 14: spread_index_middle, 15: spread_ring_middle, 16: spread_pinky_middle
            int count = Mathf.Min(mappings.Count, angles.Length);
            for (int i = 0; i < count; i++)
            {
                HandJointMapping mapping = mappings[i];
                if (mapping.jointTransform == null) continue;

                float rawAngle = angles[i]; // 传入角度，例如伸直时为约180°
                float finalAngle = 0f;

                // 对于大拇指（使用 Y 轴更新）
                if (mapping.angleName.ToLower().Contains("thumb"))
                {
                    // 对于大拇指：默认伸直手时应为0, 所以我们用公式：final = 180 - rawAngle
                    rawAngle = Mathf.Clamp(rawAngle, 90f, 180f);
                    finalAngle = 180f - rawAngle;
                    // 根据描述，右手大拇指为正，左手为负
                    if (LinkType == "Left")
                    {
                        finalAngle = -finalAngle;
                    }
                    // 更新时沿 Y 轴旋转
                    Quaternion targetRotation = Quaternion.Euler(new Vector3(0, finalAngle, 0)) * Quaternion.Euler(mapping.rotationOffset);
                    mapping.currentRotation = Quaternion.Lerp(mapping.currentRotation, targetRotation, Time.deltaTime * smoothFactor);
                    mapping.jointTransform.localRotation = mapping.currentRotation;
                }
                else if (!mapping.angleName.ToLower().Contains("spread"))
                {
                    // 对于其它关节（手指弯曲），
                    // 默认伸直手时应为0°，但传入角度为180°，故用公式：final = 180 - rawAngle
                    // 也即 final = 90 + (90 - rawAngle)
                    rawAngle = Mathf.Clamp(rawAngle, 90f, 180f);
                    finalAngle = 180f - rawAngle;
                    // 根据描述，右手的 Z 值负表示向下，因此右手取负
                    if (LinkType == "Right")
                    {
                        finalAngle = -finalAngle;
                    }
                    // 更新时沿 Z 轴旋转
                    Quaternion targetRotation = Quaternion.Euler(new Vector3(0, 0, finalAngle)) * Quaternion.Euler(mapping.rotationOffset);
                    mapping.currentRotation = Quaternion.Lerp(mapping.currentRotation, targetRotation, Time.deltaTime * smoothFactor);
                    mapping.jointTransform.localRotation = mapping.currentRotation;
                }
                // 对于 spread 关节（使用 Y 轴更新）
                else if (mapping.angleName.ToLower().Contains("spread"))
                {
                    // 默认 0 表示正常状态，所以公式：final = 90 - rawAngle
                    rawAngle = Mathf.Clamp(rawAngle, 90f, 100f);
                    finalAngle = 90f - rawAngle;
                    // 右手 spread 取负，左手取正，index则相反
                    if (LinkType == "Right")
                    {
                        finalAngle = -finalAngle;
                    }
                    if (mapping.angleName.ToLower().Contains("index"))
                    {
                        finalAngle = -finalAngle;
                    }
                    Vector3 targetEuler = mapping.jointTransform.localRotation.eulerAngles;
                    targetEuler.y = finalAngle + mapping.rotationOffset.y;
                    Quaternion targetRotation = Quaternion.Euler(targetEuler);
                    mapping.currentRotation = Quaternion.Lerp(mapping.currentRotation, targetRotation, Time.deltaTime * smoothFactor);
                    mapping.jointTransform.localRotation = mapping.currentRotation;
                }

            }
        }

        // public void SolveIK(Vector3 targetPos)
        // {
        //     // 0) Shoulder 世界位置 (根基点)
        //     Vector3 shoulderPos = shoulder.position;

        //     // 1) 计算肩 到 目标 的距离
        //     float targetDist = Vector3.Distance(targetPos, shoulderPos);

        //     // 2) clamp：不能超过最大伸直距离，也不能小于(可能要加一个最短距离)
        //     float maxReach = upperArmLength + lowerArmLength;
        //     if (targetDist > maxReach)
        //     {
        //         targetDist = maxReach;
        //     }
        //     float minReach = Mathf.Abs(upperArmLength - lowerArmLength);
        //     if (targetDist < minReach)
        //     {
        //         targetDist = minReach;
        //     }

        //     // 3) 用三角函数求解"上臂"与"前臂"的弯曲角度
        //     //    a=上臂长, b=前臂长, c=shoulder到target距离
        //     float a = upperArmLength;
        //     float b = lowerArmLength;
        //     float c = targetDist;

        //     // 求弧度
        //     float cosElbow = Mathf.Clamp((a * a + b * b - c * c) / (2 * a * b), -1f, 1f);
        //     float elbowAngle = Mathf.PI - Mathf.Acos(cosElbow); // 肘关节内角 (弧度)

        //     float cosShoulder = Mathf.Clamp((c * c + a * a - b * b) / (2 * c * a), -1f, 1f);
        //     float shoulderAngle = Mathf.Acos(cosShoulder); // 肩关节 "开合" 角度 (弧度)

        //     // 4) Shoulder 要先"朝向目标方向"
        //     //    先求当前 upperArmLocalAxis 在世界坐标下的 "原始前方向量"
        //     //    这里先把 shoulder 恢复到 Start 时的旋转, 
        //     //    然后我们再叠加新的旋转(否则每帧会越转越偏)
        //     shoulder.rotation = shoulderStartRot;

        //     // 我们拿到 "上臂本地朝向" 在 shoulder.rotation 下的 世界方向：
        //     Vector3 shoulderWorldAxis = shoulder.rotation * upperArmLocalAxis;
        //     // 目标方向
        //     Vector3 dirToTarget = (targetPos - shoulderPos).normalized;

        //     // 让 shoulderWorldAxis 指向 dirToTarget
        //     Quaternion rotToTarget = Quaternion.FromToRotation(shoulderWorldAxis, dirToTarget);
        //     shoulder.rotation = shoulder.rotation * rotToTarget;

        //     // 5) 现在再设置 UpperArm 的局部旋转：给它加上"肩关节弯曲" (弧度 shoulderAngle)
        //     //    但因为 UpperArm 已经跟着 shoulder 转向目标，所以只需要在"弯曲轴"上添加一点旋转
        //     //    假设 UpperArm 的弯曲在 Z 轴 (或者 Y 轴)，要根据你模型具体情况调整
        //     //
        //     //    这里先把 upperArm 的 localRotation 复位到初始
        //     upperArm.localRotation = upperArmStartRot;
        //     //    然后我们在 "某条轴" 上加上 -shoulderAngle(要看是正旋还是负旋)
        //     //    例如在 你的模型里, 可能是 -shoulderAngle 绕 Z
        //     // Vector3 eulerShoulder = upperArm.localEulerAngles;
        //     // eulerShoulder.z -= Mathf.Rad2Deg * shoulderAngle;
        //     // upperArm.localEulerAngles = eulerShoulder;

        //     upperArm.localRotation = upperArmStartRot * Quaternion.AngleAxis(
        //         -Mathf.Rad2Deg * shoulderAngle,
        //         new Vector3(0, 0, 1) // 绕Z轴旋转
        //     );


        //     // 6) 肘关节 (lowerArm) 弯曲 
        //     //    同理，先恢复初始，再在某轴上加 elbowAngle 或 -elbowAngle
        //     // 使用四元数绕正确轴旋转（假设绕Y轴弯曲）
        //     lowerArm.localRotation = lowerArmStartRot * Quaternion.AngleAxis(
        //         -Mathf.Rad2Deg * elbowAngle,
        //         new Vector3(0, 1, 0) // Y轴
        //     );
        //     // lowerArm.localRotation = lowerArmStartRot;
        //     // Vector3 eulerElbow = lowerArm.localEulerAngles;
        //     // eulerElbow.z += Mathf.Rad2Deg * elbowAngle;  // 注意正负号
        //     //eulerElbow.z = eulerElbow.z-180f;
        //     //lowerArm.localEulerAngles = eulerElbow;

        //     // 7) 如果还要让"手掌"对准目标方向(或者保持水平)，可以在这里做一下处理：
        //     //    例如我们希望 handBone 的某个轴(比如 localAxis)指向目标
        //     //    也可以保持 hand 的原始朝向不变，这里先示例 "保持原始角度"
        // }


        private string GetMappingName(int index)
        {
            string[] names = new string[]
            {
                "thumb_MCP", "thumb_IP",
                "index_MCP", "index_PIP", "index_DIP",
                "middle_MCP", "middle_PIP", "middle_DIP",
                "ring_MCP", "ring_PIP", "ring_DIP",
                "pinky_MCP", "pinky_PIP", "pinky_DIP",
                "spread_index_middle", "spread_ring_middle", "spread_pinky_middle"
            };
            if (index < names.Length) return names[index];
            return "unknown";
        }

        // void OnGUI()
        // {
        //     int startY = 10;
        //     foreach (var kv in currentAngles)
        //     {
        //         GUI.Label(new Rect(10, startY, 200, 20), kv.Key + ": " + kv.Value.ToString("F1"));
        //         startY += 20;
        //     }
        // }

        private void UpdateHand(string handType, List<Vector3> landmarks, ref bool isInitialized, ref Vector3 initialOffset)
        {
            // 只处理与当前 LinkType 相匹配的手
            if (LinkType != handType)
                return;

            // 根据当前 scale 计算深度（假设 depthCalibrator 已经设置好）
            float depth = depthCalibrator.GetDepthFromThumbLength(scale);

            // 使用 landmarks 中的第 0 个点作为手腕当前的位置
            Vector3 currentPosition = landmarks[0];

            // 如果还未初始化，则记录当前的位置作为初始偏移
            if (!isInitialized)
            {
                initialOffset = currentPosition;
                isInitialized = true;
            }

            // 根据当前手的位置、初始偏移和深度计算新的目标位置
            Vector3 newPosition = CalculatePosition(currentPosition, initialOffset, handType, depth);

            // 平滑过渡到目标位置
            this.transform.localPosition = Vector3.Lerp(this.transform.localPosition, newPosition, Time.deltaTime * smoothFactor);
        }

        private Vector3 CalculatePosition(Vector3 current, Vector3 initial, string handType, float d)
        {
            // 根据 LinkType 选择对应的初始位置
            Vector3 initialPosition = (LinkType == "Right") ? initialRightPosition : initialLeftPosition;

            // 计算 x,y 的偏移（单位转换系数 1000f，可根据需要调整）
            float offsetX = (-current.x + initial.x) / 1000f;
            float offsetY = (-current.y + initial.y) / 1000f;
            // z 轴采用深度 d 与当前 scale 计算（这里使用除以 200f 的系数，可根据需要调整）
            float offsetZ = (d * scale) / 200f;

            // 新的位置等于初始位置加上计算得到的偏移
            return new Vector3(initialPosition.x + offsetX,
                               initialPosition.y + offsetY,
                               initialPosition.z + offsetZ);
        }


        // private void UpdateHand(string handType, string[] points, ref bool isInitialized, ref Vector3 initialOffset)
        // {
        //     if (LinkType == handType)
        //     {
        //         if (handType == "Left")
        //         {
        //             float depth = depthCalibrator.GetDepthFromThumbLength(scale);
        //             float wristZ = float.Parse(points[2]);
        //             Vector3 currentPosition = new Vector3(float.Parse(points[0]), float.Parse(points[1]), wristZ);
        //             if (!isInitialized)
        //             {
        //                 initialLeftOffset = new Vector3(float.Parse(points[0]), float.Parse(points[1]), wristZ);
        //                 isLeftInitialized = true;
        //             }
        //             Vector3 newPosition = CalculatePosition(currentPosition, initialLeftOffset, handType, depth);
        //             this.transform.localPosition = Vector3.Lerp(this.transform.localPosition, newPosition, Time.deltaTime * smoothFactor);
        //             //this.transform.localPosition = new Vector3(float.Parse(points[0]), float.Parse(points[1]), depth * scale);
        //             UpdateScale(points);
        //         }
        //         if (handType == "Right")
        //         {
        //             float depth = depthCalibrator.GetDepthFromThumbLength(scale);
        //             float wristZ = float.Parse(points[2]);
        //             Vector3 currentPosition = new Vector3(float.Parse(points[0]), float.Parse(points[1]), wristZ);
        //             if (!isInitialized)
        //             {
        //                 initialRightOffset = new Vector3(float.Parse(points[0]), float.Parse(points[1]), wristZ);
        //                 isRightInitialized = true;
        //             }
        //             Vector3 newPosition = CalculatePosition(currentPosition, initialRightOffset, handType, depth);
        //             this.transform.localPosition = Vector3.Lerp(this.transform.localPosition, newPosition, Time.deltaTime * smoothFactor);
        //             //this.transform.localPosition = new Vector3(float.Parse(points[0]), float.Parse(points[1]), depth * scale);
        //             UpdateScale(points);
        //         }
        //     }

        // }

        // private Vector3 CalculatePosition(Vector3 current, Vector3 initial, string handType, float d)
        // {
        //     Vector3 initialPosition = LinkType == "Right" ? initialRightPosition : initialLeftPosition;
        //     return new Vector3((-current.x + initial.x) / 1000 + initialPosition.x,
        //                        (current.y - initial.y) / 1000 + initialPosition.y,
        //                        initialPosition.z + (d * scale) / 200);//initialPosition.z + (d * scale) initialPosition.z + (current.z - initial.z) / 200
        // }

        // private void UpdateScale(string[] points)
        // {
        //     Vector3 pointA = new Vector3(float.Parse(points[0]), float.Parse(points[1]), float.Parse(points[2]));
        //     Vector3 pointB = new Vector3(float.Parse(points[4]), float.Parse(points[5]), float.Parse(points[6]));
        //     float thumbDetectedLength = Vector3.Distance(pointA, pointB);

        //     if (thumbDetectedLength == 0) return;

        //     scale = thumbModelLength / thumbDetectedLength;
        //     this.transform.localScale = new Vector3(scale, scale, scale);
        // }

        private void UpdateWristRotation()
        {
            List<Vector3> landmarks = null;
            if (LinkType == "Left")
            {
                landmarks = leftHandLandmarks;
            }
            else if (LinkType == "Right")
            {
                landmarks = rightHandLandmarks;
            }
            if (landmarks == null || landmarks.Count < 10)
            {
                return;
            }
            // the finger root
            // var wristTransform = handPoints[0].transform;
            // var thumb = handPoints[1].transform.position;
            // var middleFinger = handPoints[9].transform.position;
            Vector3 wristPos = landmarks[0];
            Vector3 thumbPos = landmarks[1];
            Vector3 middlePos = landmarks[9];

            // 计算从手腕到拇指和中指的向量
            Vector3 vectorToThumb = thumbPos - wristPos;
            Vector3 vectorToMiddle = middlePos - wristPos;

            // 正交归一化向量
            Vector3.OrthoNormalize(ref vectorToMiddle, ref vectorToThumb);

            // 计算法线向量
            Vector3 normalVector = Vector3.Cross(vectorToThumb, vectorToMiddle);

            // 计算目标旋转
            Quaternion targetRotation = Quaternion.LookRotation(normalVector, vectorToMiddle);

            Quaternion presetOffset = (LinkType == "Right") ?
                                            Quaternion.Euler(0, -90, -90) :
                                            Quaternion.Euler(0, -90, 90);
            // 结合预设偏移
            targetRotation = targetRotation * presetOffset;

            // 平滑过渡
            if (currentWristRotation == Quaternion.identity)
            {
                currentWristRotation = targetRotation;
            }
            currentWristRotation = Quaternion.Lerp(currentWristRotation, targetRotation, Time.deltaTime * smoothFactor);

            // 应用到当前对象的 rotation
            transform.rotation = currentWristRotation;

            // // 计算新的旋转
            // Quaternion lookRotation = Quaternion.LookRotation(normalVector, vectorToMiddle);

            // // 计算Y轴旋转修正（如果需要）
            // Quaternion yRotationCorrection = Quaternion.Euler(0, 90, -90);
            // if (LinkType == "Right")
            // {
            //     yRotationCorrection = Quaternion.Euler(0, 90, 90);
            // }

            // // 应用新的旋转并保留修正的Y轴旋转
            // wristTransform.rotation = lookRotation * yRotationCorrection;

        }
    }
}