using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KeyInputCapture : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    public InputField inputField;  // 要显示键名的 InputField
    private bool isCapturing = false;

    void Update()
    {
        if (isCapturing)
        {
            // 遍历所有KeyCode（这里遍历枚举的所有值，效率问题在配置界面中一般不敏感）
            foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(keyCode))
                {
                    string displayName = keyCode.ToString();
                    if (keyCode >= KeyCode.Alpha0 && keyCode <= KeyCode.Alpha9)
                    {
                        // 计算数字，例如 Alpha0 -> 0
                        int num = keyCode - KeyCode.Alpha0;
                        displayName = num.ToString();
                    }
                    // 当捕捉到任意按键时，设置 InputField 文本为该 KeyCode 的名称
                    inputField.text = displayName;
                    isCapturing = false;
                    // 自动取消选中（可选）
                    EventSystem.current.SetSelectedGameObject(null);
                    break;
                }
            }
        }
    }

    // 当 InputField 获得焦点时开始捕捉
    public void OnSelect(BaseEventData eventData)
    {
        isCapturing = true;
        // 清空原有文本（可选）
        inputField.text = "";
    }

    // 当 InputField 失去焦点时停止捕捉
    public void OnDeselect(BaseEventData eventData)
    {
        isCapturing = false;
    }
}
