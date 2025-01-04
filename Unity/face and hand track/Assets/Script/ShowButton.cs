using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShowButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private CanvasGroup targetCanvasGroup;
    
    // 当鼠标指针进入该UI对象区域时，会自动调用此方法
    public void OnPointerEnter(PointerEventData eventData)
    {

            targetCanvasGroup.alpha = 1f;

    }

    // 当鼠标指针离开该UI对象区域时，会自动调用此方法
    public void OnPointerExit(PointerEventData eventData)
    {

            targetCanvasGroup.alpha = 0f;

    }
}
