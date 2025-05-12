using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;  // RigBuilder 所在命名空间
using landmarktest;
using VRM;
using System.IO;

public class StartupController : MonoBehaviour
{
    [Header("Rig 控件 (M 物品)")]
    public RigBuilder rigBuilder;          // M 物品上的 RigBuilder

    [Header("Animator 控件")]
    public Animator sourceAnimator;        // M 子对象上的 Animator
    public Animator targetAnimator;        // 自身的 Animator

    [Header("手部目标 (TransformLink)")]
    public MonoBehaviour rHandLink;        // RHandTarget 上的 TransformLink（可用 MonoBehaviour 代替具体类型）
    public MonoBehaviour lHandLink;        // LHandTarget 上的 TransformLink
    public GameObject O;

    void Awake()
    {
        // 1. 自动查找并缓存组件
        rigBuilder = GetComponent<RigBuilder>();                            // 获取本体上的 RigBuilder :contentReference[oaicite:4]{index=4}
        targetAnimator = GetComponent<Animator>();                              // 获取本体上的 Animator :contentReference[oaicite:5]{index=5}

        var animators = GetComponentsInChildren<Animator>(true);
        sourceAnimator = null;
        foreach (var anim in animators)
        {
            // 排除挂在 M 本体上的 Animator（即 targetAnimator）
            if (anim.gameObject != this.gameObject)
            {
                sourceAnimator = anim;
                break;  // 找到第一个子级 Animator 就跳出
            }
        }
        if (sourceAnimator == null)
        {
            Debug.LogWarning("[StartupController] 未在子物体中找到 Animator 组件");
        }
        var rHand = GameObject.Find("RHandTarget")?.transform;                              // 查找 RHandTarget 子对象 :contentReference[oaicite:8]{index=8}
        if (rHand != null)
            rHandLink = rHand.GetComponent<TransformLink>();                     // 获取其上的 TransformLink 脚本 :contentReference[oaicite:9]{index=9}

        var lHand = GameObject.Find("LHandTarget")?.transform;                              // 同理查找 LHandTarget 子对象 :contentReference[oaicite:10]{index=10}
        if (lHand != null)
            lHandLink = lHand.GetComponent<TransformLink>();                     // 获取其上的 TransformLink 脚本 :contentReference[oaicite:11]{index=11}
        // 禁用所有脚本组件以备按顺序启动
        if (rigBuilder != null) rigBuilder.enabled = false;          // 禁用 RigBuilder :contentReference[oaicite:0]{index=0}
        if (sourceAnimator != null) sourceAnimator.enabled = false;  // 禁用 子 Animator :contentReference[oaicite:1]{index=1}
        if (targetAnimator != null) targetAnimator.enabled = false;  // 禁用 自身 Animator :contentReference[oaicite:2]{index=2}
        if (rHandLink != null) rHandLink.enabled = false;
        if (lHandLink != null) lHandLink.enabled = false;

        // 新增：查找名为 "target" 的物体
        GameObject targetObj = GameObject.Find("target");
        if (sourceAnimator != null && targetObj != null)
        {
            var lookAt = sourceAnimator.GetComponent<VRMLookAtHead>();
            if (lookAt != null)
            {
                lookAt.Target = targetObj.transform;
            }
            else
            {
                Debug.LogWarning("[StartupController] 子物体 Animator 上未找到 VRMLookAtHead 组件");
            }
        }

        // // 1. 启用 RigBuilder
        // if (rigBuilder != null)
        // {
        //     rigBuilder.enabled = true;  // 启用 RigBuilder 控件 :contentReference[oaicite:3]{index=3}

        // }

        // 2. 仅更新 Avatar 并启用自身 Animator
        if (sourceAnimator != null && targetAnimator != null)
        {
            // 用子物体 Animator 的 Avatar 替换 M 物体上的 Animator.avatar
            targetAnimator.avatar = sourceAnimator.avatar;

            // // 启用 M 物体上的 Animator
            // targetAnimator.enabled = true;
        }

        // // 3. 启用手部目标的 TransformLink 脚本
        // if (rHandLink != null) rHandLink.enabled = true;
        // if (lHandLink != null) lHandLink.enabled = true;
    }

    IEnumerator Start()
    {
        // 等待一帧确保所有 Awake 逻辑完成
        yield return null;

        // 1. 启用 RigBuilder
        if (rigBuilder != null)
        {
            rigBuilder.enabled = true;  // 启用 RigBuilder 控件 :contentReference[oaicite:3]{index=3}

        }

        yield return null;

        // 2. 仅更新 Avatar 并启用自身 Animator
        if (sourceAnimator != null && targetAnimator != null)
        {
            // 启用 M 物体上的 Animator
            targetAnimator.enabled = true;
        }

        // 3. 启用手部目标的 TransformLink 脚本
        if (rHandLink != null) rHandLink.enabled = true;
        if (lHandLink != null) lHandLink.enabled = true;
    }
}