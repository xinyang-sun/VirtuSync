using System;
using System.Data.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace landmarktest
{
    public class HandTracking : MonoBehaviour
    {
        // Start is called before the first frame update
        public UDPReceive udpReceive;
        public List<GameObject> handPoints;


        [SerializeField]
        private float thumbModelLength = 0.03f;
        private float scale;
        private DepthCalibrator depthCalibrator = new DepthCalibrator(-0.0719f, 0.439f);
        private Vector3 initialRightPosition = new Vector3(0.243000001f, 1.09000003f, 0.254999995f);
        private Vector3 initialLeftPosition = new Vector3(-0.261000007f, 0.995000005f, 0.171000004f);
        private Vector3 initialRightOffset;
        private Vector3 initialLeftOffset;
        private Vector3 direction;
        private bool isRightInitialized = false;
        private bool isLeftInitialized = false;

        private TransformLink[] transformLinkers;
        public string LinkType = "None";
        float RzD = 0;
        float LzD = 0;
        float smoothFactor = 5.0f;
        void Awake()
        {
            transformLinkers = GetComponentsInChildren<TransformLink>();
        }

        void Update()
        {
            foreach (var linker in transformLinkers)
            {
                linker.UpdateTransform();
            }
            Debug.Log("handPoints.Count:" + handPoints.Count);
            if (string.IsNullOrEmpty(udpReceive.data)) return;
            ParseData(udpReceive.data);
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
            }
            else if (LinkType == "Left")
            {
                this.transform.localPosition = initialLeftPosition;
            }
        }

        private void ParseData(string data)
        {
            // Remove the part of the data up to "roll, pitch, yaw"
            if (data.Contains("roll, pitch, yaw"))
            {
                int indexStart = data.LastIndexOf("roll, pitch, yaw") + 19;
                data = data.Substring(indexStart).TrimEnd(new char[] { ',' });
                Debug.Log("data: " + data);
            }

            // Handle different data ordering
            string leftData = ExtractHandData(data, "Left");
            string rightData = ExtractHandData(data, "Right");

            if (!string.IsNullOrEmpty(leftData))
            {
                leftData = leftData.Remove(0, 3);
                leftData = leftData.Remove(leftData.Length - 3, 3);
                Debug.Log("leftData:" + leftData);
                UpdateHandPoints(leftData.Split(','), "Left");
                UpdateHand("Left", leftData.Split(','), ref isLeftInitialized, ref initialLeftOffset);
            }

            if (!string.IsNullOrEmpty(rightData))
            {
                rightData = rightData.Remove(0, 3);
                rightData = rightData.Remove(rightData.Length - 1, 1);
                Debug.Log("rightData:" + rightData);
                UpdateHandPoints(rightData.Split(','), "Right");
                UpdateHand("Right", rightData.Split(','), ref isRightInitialized, ref initialRightOffset);
            }
            UpdateWristRotation();
        }

        private string ExtractHandData(string data, string handType)
        {
            int start = data.IndexOf(handType);
            if (start == -1) return null; // Hand data not found

            start += handType.Length;
            int end = data.IndexOf(handType == "Left" ? "Right" : "Left", start); // Look for the start of the other hand data
            if (end == -1) end = data.Length; // If the other hand data isn't found, assume end of data

            return data.Substring(start, end - start).Trim();
        }



        private void UpdateHandPoints(string[] points, string handType)
        {
            if (LinkType == handType)
            {
                for (int i = 1; i < handPoints.Count; i++)
                {
                    if (i * 4 + 2 >= points.Length) break; // Prevent index out of bounds
                    float x = float.Parse(points[i * 4]) - float.Parse(points[0]);
                    float y = float.Parse(points[i * 4 + 1]) - float.Parse(points[1]);
                    float z = float.Parse(points[i * 4 + 2]) - float.Parse(points[2]);
                    float parsedValue = float.Parse(points[i * 4 + 3]);
                    direction = new Vector3(x, y, z);
                    direction.Normalize();

                    if (parsedValue != 0)
                    {
                        if (handType == "Left")
                        {
                            LzD = parsedValue; // Update LzD for Left hand
                        }
                        else if (handType == "Right")
                        {
                            RzD = parsedValue; // Update RzD for Right hand
                        }
                    }

                    Vector3 newPosition = new Vector3(x, y, z);
                    handPoints[i].transform.localPosition = Vector3.Lerp(handPoints[i].transform.localPosition, newPosition, Time.deltaTime * smoothFactor);
                }

            }
        }

        private void UpdateHand(string handType, string[] points, ref bool isInitialized, ref Vector3 initialOffset)
        {
            if (LinkType == handType)
            {
                if (handType == "Left")
                {
                    float depth = depthCalibrator.GetDepthFromThumbLength(scale);
                    float wristZ = float.Parse(points[2]);
                    Vector3 currentPosition = new Vector3(float.Parse(points[0]), float.Parse(points[1]), wristZ);
                    if (!isInitialized)
                    {
                        initialLeftOffset = new Vector3(float.Parse(points[0]), float.Parse(points[1]), wristZ);
                        isLeftInitialized = true;
                    }
                    Vector3 newPosition = CalculatePosition(currentPosition, initialLeftOffset, handType, depth);
                    this.transform.localPosition = Vector3.Lerp(this.transform.localPosition, newPosition, Time.deltaTime * smoothFactor);
                    //this.transform.localPosition = new Vector3(float.Parse(points[0]), float.Parse(points[1]), depth * scale);
                    UpdateScale(points);
                }
                if (handType == "Right")
                {
                    float depth = depthCalibrator.GetDepthFromThumbLength(scale);
                    float wristZ = float.Parse(points[2]);
                    Vector3 currentPosition = new Vector3(float.Parse(points[0]), float.Parse(points[1]), wristZ);
                    if (!isInitialized)
                    {
                        initialRightOffset = new Vector3(float.Parse(points[0]), float.Parse(points[1]), wristZ);
                        isRightInitialized = true;
                    }
                    Vector3 newPosition = CalculatePosition(currentPosition, initialRightOffset, handType, depth);
                    this.transform.localPosition = Vector3.Lerp(this.transform.localPosition, newPosition, Time.deltaTime * smoothFactor);
                    //this.transform.localPosition = new Vector3(float.Parse(points[0]), float.Parse(points[1]), depth * scale);
                    UpdateScale(points);
                }
            }

        }

        private Vector3 CalculatePosition(Vector3 current, Vector3 initial, string handType, float d)
        {
            Vector3 initialPosition = LinkType == "Right" ? initialRightPosition : initialLeftPosition;
            return new Vector3((-current.x + initial.x) / 1000 + initialPosition.x,
                               (current.y - initial.y) / 1000 + initialPosition.y,
                               initialPosition.z + (d * scale) / 200);//initialPosition.z + (d * scale) initialPosition.z + (current.z - initial.z) / 200
        }

        private void UpdateScale(string[] points)
        {
            Vector3 pointA = new Vector3(float.Parse(points[0]), float.Parse(points[1]), float.Parse(points[2]));
            Vector3 pointB = new Vector3(float.Parse(points[4]), float.Parse(points[5]), float.Parse(points[6]));
            float thumbDetectedLength = Vector3.Distance(pointA, pointB);

            if (thumbDetectedLength == 0) return;

            scale = thumbModelLength / thumbDetectedLength;
            this.transform.localScale = new Vector3(scale, scale, scale);
        }

        private void UpdateWristRotation()
        {
            // the finger root
            var wristTransform = handPoints[0].transform;
            var thumb = handPoints[1].transform.position;
            var middleFinger = handPoints[9].transform.position;

            var vectorToThumb = thumb - wristTransform.position;
            var vectorToMiddle = middleFinger - wristTransform.position;

            // 正交归一化向量
            Vector3.OrthoNormalize(ref vectorToMiddle, ref vectorToThumb);

            // 计算法线向量
            Vector3 normalVector = Vector3.Cross(vectorToThumb, vectorToMiddle);

            // 计算新的旋转
            Quaternion lookRotation = Quaternion.LookRotation(normalVector, vectorToMiddle);

            // 计算Y轴旋转修正（如果需要）
            Quaternion yRotationCorrection = Quaternion.Euler(0, 90, -90);
            if (LinkType == "Right")
            {
                yRotationCorrection = Quaternion.Euler(0, 90, 90);
            }

            // 应用新的旋转并保留修正的Y轴旋转
            wristTransform.rotation = lookRotation * yRotationCorrection;

        }
    }
}