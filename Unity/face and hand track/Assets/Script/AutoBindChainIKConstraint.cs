using UnityEngine;
using UnityEngine.Animations.Rigging;

// 定义一个枚举，用来选择左手或右手
public enum HandType
{
    Left,
    Right
}

public class AutoBindChainIKConstraint : MonoBehaviour
{
    // 自动查找名称为 "M" 的模型
    public GameObject model;

    // 模型 Animator
    public Animator animator;

    // 根据手部类型选择绑定：左手或右手
    public HandType handType;

    // Chain IK Constraint 组件，需要在 Inspector 中赋值
    public ChainIKConstraint chainIKConstraint;

    // 分别用于左手和右手 IK target
    public Transform LH_IK_Target;
    public Transform RH_IK_Target;

    // 骨骼引用（根据手部类型自动获取）
    private Transform shoulder;
    private Transform hand;

    void Start()
    {
        // 查找场景中名称为 "M" 的模型
        model = GameObject.Find("M");
        if (model == null)
        {
            Debug.LogError("未找到名称为 'M' 的模型！");
            return;
        }

        // 获取模型上的 Animator 组件
        animator = model.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("模型 'M' 上未找到 Animator 组件！");
            return;
        }

        // 根据所选 HandType 获取对应的骨骼
        if (handType == HandType.Right)
        {
            shoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
            hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        }
        else // Left
        {
            shoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            hand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        }

        if (shoulder == null || hand == null)
        {
            Debug.LogError("无法获取到目标骨骼，请检查 Humanoid Avatar 映射是否正确。");
            return;
        }

        // 初始化 target 的位置与旋转，使其与手部骨骼同步
        if (hand != null)
        {
            if (handType == HandType.Right && RH_IK_Target != null)
            {
                RH_IK_Target.position = hand.position;
                RH_IK_Target.rotation = hand.rotation;
            }
            else if (handType == HandType.Left && LH_IK_Target != null)
            {
                LH_IK_Target.position = hand.position;
                LH_IK_Target.rotation = hand.rotation;
            }
        }

        // 自动绑定 Chain IK Constraint 的 root 和 tip
        if (chainIKConstraint != null)
        {
            chainIKConstraint.data.root = shoulder;
            chainIKConstraint.data.tip = hand;

            // 根据 HandType 指定 target
            if (handType == HandType.Right)
            {
                if (RH_IK_Target != null)
                {
                    chainIKConstraint.data.target = RH_IK_Target;
                }
                else
                {
                    Debug.LogWarning("RH_IK_Target 未指定，请在 Inspector 中设置。");
                }
            }
            else // Left
            {
                if (LH_IK_Target != null)
                {
                    chainIKConstraint.data.target = LH_IK_Target;
                }
                else
                {
                    Debug.LogWarning("LH_IK_Target 未指定，请在 Inspector 中设置。");
                }
            }
        }
        else
        {
            Debug.LogWarning("ChainIKConstraint 未指定，请在 Inspector 中赋值。");
        }

        
    }
    // void Start()
    // {
        
    // }
}
