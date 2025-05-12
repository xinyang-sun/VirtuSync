using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;  // 目标对象
    public float distance = 5.0f;  // 距离目标的初始距离
    public float scrollSpeed = 2.0f;  // 滚轮缩放速度
    public float rotateSpeed = 5.0f;  // 右键旋转速度
    public float dragSpeed = 0.01f;   // 左键拖动速度

    private float currentDistance;
    private float x = 0.0f;
    private float y = 0.0f;
    private Vector3 dragOrigin; // 用于存储拖动起点
    private Vector3 currentPositionOffset; // 用于存储当前位置偏移量
    private Camera camera1;     // 用于存储名为 Camera1 的摄像机

    void Start()
    {
        if (target == null)
        {
            // 1. 查找场景中名为 "M" 的根物体
            GameObject mRoot = GameObject.Find("M");                                  // 静态查找根级对象&#8203;:contentReference[oaicite:1]{index=1}
            if (mRoot != null)
            {
                // 2. 在其子孙中获取第一个 Animator 组件
                Animator anim = mRoot.GetComponentInChildren<Animator>();              // 递归查找子对象&#8203;:contentReference[oaicite:2]{index=2}
                if (anim != null && anim.isHuman)
                {
                    // 3. 获取 Humanoid 骨骼中的 Neck Transform 并赋值
                    target = anim.GetBoneTransform(HumanBodyBones.Neck);               // 获取颈部骨骼&#8203;:contentReference[oaicite:3]{index=3}
                    if (target == null)
                        Debug.LogWarning("未能获取 Neck 骨骼 Transform，请检查 Avatar 是否为 Humanoid");
                }
            }
        }

        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        currentDistance = distance;
        currentPositionOffset = transform.position - target.position;

        // 获取名为 Camera1 的摄像机
        camera1 = GameObject.Find("Camera1").GetComponent<Camera>();
        if (camera1 == null)
        {
            Debug.LogError("Camera1 not found or does not have a Camera component.");
        }
    }

    void LateUpdate()
    {
        if (target)
        {
            // 右键旋转视角
            if (Input.GetMouseButton(1))
            {
                x += Input.GetAxis("Mouse X") * rotateSpeed;
                y -= Input.GetAxis("Mouse Y") * rotateSpeed;

                // 限制视角上下旋转的角度
                y = Mathf.Clamp(y, -80, 80);
            }

            // 滚轮放大缩小
            currentDistance -= Input.GetAxis("Mouse ScrollWheel") * scrollSpeed;
            currentDistance = Mathf.Clamp(currentDistance, -1f, 2.0f);  // 限制缩放距离

            // 左键拖动摄像头
            if (Input.GetMouseButtonDown(0))
            {
                dragOrigin = Input.mousePosition;
            }

            if (Input.GetMouseButton(0))
            {
                Vector3 dragDifference = Input.mousePosition - dragOrigin;
                Vector3 move = new Vector3(-dragDifference.x * dragSpeed, -dragDifference.y * dragSpeed, 0);

                currentPositionOffset += transform.TransformDirection(move);
                dragOrigin = Input.mousePosition;
            }

            // 更新摄像头的位置和旋转
            Quaternion rotation = Quaternion.Euler(y, x, 0);
            Vector3 position = target.position + currentPositionOffset + rotation * new Vector3(0.0f, 0.0f, -currentDistance);

            transform.rotation = rotation;
            transform.position = position;
        }
    }
}
