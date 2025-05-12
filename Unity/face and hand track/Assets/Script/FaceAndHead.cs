using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRM;
namespace landmarktest
{
    //[RequireComponent(typeof(Animator))]
    public class FaceAndHead : MonoBehaviour
    {
        public UDPReceive udpReceive;
        //Face
        public float animSpeed = 1.5f;
        public float eye_close, eye_open, mouth_close, mouth_open;
        public float max_rotation_angle = 45.0f;
        VRMBlendShapeProxy m_blendShapes;
        //public SkinnedMeshRenderer eye, eye_lid, mouth, eyebrow;
        public Animator anim;
        private Vector3 head = new Vector3(0, 0, 0);
        private string mar;
        private float roll = 0, pitch = 0, yaw = 0, lear = 1.0f, rear = 1.0f, mRatioX = 0f, mRatioY = 0f, mRatioA = 0f, mRatioE = 0f, mRatioI = 0f, mRatioO = 0f, mRatioU = 0f, eyeTarget_x = 0f, eyeTarget_y = 0f;
        private Transform neck;
        private Quaternion neck_quat;

        // Variables for smoothing
        private float smoothFactor = 5.0f;
        private Quaternion targetNeckRotation;
        private Vector3 targetPosition;
        private Vector3 initialPosition;
        private bool AutoBlinkFlag = true;

        public void SetBlinkEnabled(bool isEnabled)
        {
            AutoBlinkFlag = isEnabled;
            Debug.Log(AutoBlinkFlag);
        }

        void RefreshReferences()
        {
            /* Animator */
            anim = GetComponentInChildren<Animator>(true);
            if (!anim)
            {
                Debug.LogWarning($"{name} 找不到 Animator，请确认 Model1 是子物体");
                return;
            }

            /* Neck transform & 基准旋转 */
            neck = anim.GetBoneTransform(HumanBodyBones.Neck);
            neck_quat = Quaternion.Euler(0, 90, -90);   // 原脚本基准

            /* BlendShapeProxy */
            m_blendShapes = GetComponentInChildren<VRMBlendShapeProxy>(true);
            if (!m_blendShapes)
                Debug.LogWarning($"{name} 找不到 VRMBlendShapeProxy");

            /* 初始化插值目标 */
            targetNeckRotation = neck ? neck.rotation : Quaternion.identity;
        }

        /* Unity 在子物体增删时会调用 */
        void OnTransformChildrenChanged() => RefreshReferences();

        void Start()
        {
            RefreshReferences();
            //anim = GetComponent<Animator>();
            //neck = anim.GetBoneTransform(HumanBodyBones.Neck);
            //neck_quat = Quaternion.Euler(0, 90, -90);

            // Initialize the target neck rotation to the current rotation and targetPosition for eyes
            // targetNeckRotation = neck.rotation;
            // targetPosition = transform.position;

            GameObject target = GameObject.Find("target");
            if (target != null)
            {
                initialPosition = target.transform.position;  // 保存初始位置
            }

        }

        public void ResetFaceAndHeadPositions()
        {
            //neck.rotation = Quaternion.Euler(0, 0, 0);
        }

        // Update is called once per frame
        void Update()
        {
            /* 没有引用就尝试补绑一次（防止极端时机错过） */
            if (!m_blendShapes || !anim) RefreshReferences();
            if (!anim) return;   // 依旧找不到就先退出

            string data = udpReceive.data;
            string[] eyeMARpoints = null;
            string[] mRatioM = null;
            string[] eyePotpoints = null;
            string[] neckpoints = null;
            string faceData = data;
            string EARData = data;
            string eyePotData = data;
            //Face
            if (data.Contains("LEAR, REAR, MAR") == true)
            {
                EARData = EARData.Remove(0, 1);
                EARData = EARData.Remove(EARData.LastIndexOf("LEAR, REAR, MAR") - 3);
                Debug.Log("EARData: " + EARData);
                eyeMARpoints = EARData.Split(',');
                lear = float.Parse(eyeMARpoints[0]);
                rear = float.Parse(eyeMARpoints[1]);
                mar = eyeMARpoints[2].Trim(' ', '\'');
                mRatioM = mar.Split(';');
                mRatioX = float.Parse(mRatioM[0]);
                mRatioY = float.Parse(mRatioM[1]);
                mRatioA = Mathf.Clamp01(float.Parse(mRatioM[2]));
                mRatioE = Mathf.Clamp01(float.Parse(mRatioM[3]));
                mRatioI = Mathf.Clamp01(float.Parse(mRatioM[4]));
                mRatioO = Mathf.Clamp01(float.Parse(mRatioM[5]));
                mRatioU = Mathf.Clamp01(float.Parse(mRatioM[6]));
                Debug.Log("lear: " + lear);
                Debug.Log("rear: " + rear);
                Debug.Log("mar: " + mar);
                Debug.Log("mRX" + mRatioX);
                Debug.Log("mRY" + mRatioY);
                //m_blendShapes = GetComponent<VRM.VRMBlendShapeProxy>();

                if (!AutoBlinkFlag)
                {
                    if ((lear + rear) / 2 < 0.2f)
                    {
                        m_blendShapes.ImmediatelySetValue(BlendShapeKey.CreateFromPreset(BlendShapePreset.Blink_L), 1.0f);
                        m_blendShapes.ImmediatelySetValue(BlendShapeKey.CreateFromPreset(BlendShapePreset.Blink_R), 1.0f);
                    }
                    else
                    {
                        m_blendShapes.ImmediatelySetValue(BlendShapeKey.CreateFromPreset(BlendShapePreset.Blink_L), 0);
                        m_blendShapes.ImmediatelySetValue(BlendShapeKey.CreateFromPreset(BlendShapePreset.Blink_R), 0);
                    }

                }
                if (mRatioY > -0.2f)
                {
                    m_blendShapes.ImmediatelySetValue(BlendShapeKey.CreateFromPreset(BlendShapePreset.Neutral), 0);
                    m_blendShapes.ImmediatelySetValue(BlendShapeKey.CreateFromPreset(BlendShapePreset.A), mRatioA);
                    m_blendShapes.ImmediatelySetValue(BlendShapeKey.CreateFromPreset(BlendShapePreset.E), mRatioE);
                    m_blendShapes.ImmediatelySetValue(BlendShapeKey.CreateFromPreset(BlendShapePreset.I), mRatioI);
                    m_blendShapes.ImmediatelySetValue(BlendShapeKey.CreateFromPreset(BlendShapePreset.O), mRatioO);
                    m_blendShapes.ImmediatelySetValue(BlendShapeKey.CreateFromPreset(BlendShapePreset.U), mRatioU);
                }
                else
                {
                    m_blendShapes.ImmediatelySetValue(BlendShapeKey.CreateFromPreset(BlendShapePreset.Neutral), 1.0f);
                }

            }

            if (data.Contains("leftEyePot, rightEyePot") == true)
            {
                eyePotData = eyePotData.Remove(0, eyePotData.LastIndexOf("LEAR, REAR, MAR") + 17);
                eyePotData = eyePotData.Remove(0, 1);
                eyePotData = eyePotData.Remove(eyePotData.LastIndexOf("leftEyePot, rightEyePot") - 3);
                eyePotpoints = eyePotData.Split(',');
                eyeTarget_x = float.Parse(eyePotpoints[0]);
                eyeTarget_y = float.Parse(eyePotpoints[1]);
                // 直接四舍五入到小数点后三位
                float posX = Mathf.Round(eyeTarget_x * 10f) / 10f;
                float posY = Mathf.Round(eyeTarget_y * 10f) / 10f;
                // 更新目标位置
                GameObject target = GameObject.Find("target");
                if (target != null)
                {
                    float threshold = 0.1f;  // 定义一个阈值
                    Vector3 currentOffset = new Vector3(posX, posY, 0);  // 计算当前偏移
                    Vector3 newPosition = initialPosition + currentOffset;  // 计算新的相对位置

                    if (Mathf.Abs(posX) > threshold || Mathf.Abs(posY) > threshold)
                    {
                        target.transform.position = Vector3.Lerp(target.transform.position, newPosition, Time.deltaTime * smoothFactor);
                    }
                    else
                    {
                        target.transform.position = Vector3.Lerp(target.transform.position, initialPosition, Time.deltaTime * smoothFactor);  // 重置到初始位置
                    }
                }
            }

            if (data.Contains("roll, pitch, yaw") == true)
            {
                //string faceData = data;
                if (faceData.Contains("LEAR, REAR, MAR") == true)
                {
                    faceData = faceData.Remove(0, faceData.LastIndexOf("LEAR, REAR, MAR") + 20);
                }
                if (faceData.Contains("leftEyePot, rightEyePot") == true)
                {
                    faceData = faceData.Remove(0, faceData.LastIndexOf("leftEyePot, rightEyePot") + 24);
                }
                faceData = faceData.Remove(0, 1);
                faceData = faceData.Remove(faceData.LastIndexOf("roll, pitch, yaw") - 3);
                Debug.Log("faceData: " + faceData);
                neckpoints = faceData.Split(',');
                roll = float.Parse(neckpoints[0]) * 2;
                pitch = float.Parse(neckpoints[1]) * 2;
                yaw = float.Parse(neckpoints[2]) * 2;

                //print("facedata"+roll);
                //neck.rotation = Quaternion.Euler(-pitch, yaw, -roll) * neck_quat;

                // Smoothly interpolate the neck rotation
                float pitch_clamp = Mathf.Clamp(pitch, -max_rotation_angle, max_rotation_angle);
                float yaw_clamp = Mathf.Clamp(yaw, -max_rotation_angle, max_rotation_angle);
                float roll_clamp = Mathf.Clamp(roll, -max_rotation_angle, max_rotation_angle);
                Quaternion newNeckRotation = Quaternion.Euler(pitch_clamp, -yaw_clamp, roll_clamp);
                targetNeckRotation = Quaternion.Slerp(targetNeckRotation, newNeckRotation, Time.deltaTime * smoothFactor);
                neck.rotation = targetNeckRotation;
            }
        }
    }

}
