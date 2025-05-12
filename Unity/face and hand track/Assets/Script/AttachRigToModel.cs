using UnityEngine;

public class AttachRigToModel : MonoBehaviour
{
    void Awake()
    {
        GameObject mRoot = GameObject.Find("M"); 
        if (mRoot == null)
        {
            Debug.LogWarning("[AttachRigToModel] 未找到名称为 'M' 的 GameObject！");
            return;
        }

        // 2. 确认 M 至少有一个子对象
        if (mRoot.transform.childCount < 1)
        {
            Debug.LogWarning("[AttachRigToModel] M 没有任何子对象，无法继续附加！");
            return;
        }

        Transform firstChild = mRoot.transform.GetChild(0);

        transform.SetParent(firstChild, false);
        Debug.Log("[AttachRigToModel] 已成功将 Rig 附加到 M → 子对象 下。");
        
    }
}
