
using UnityEngine;
using UnityEngine.UI;

public class ChangeCamera : MonoBehaviour
{
    public GameObject MainCamera;
    public GameObject Camera;
    public Button CameraButton;
    public GameObject Port;
    public GameObject Text;
    public GameObject PortNumber;
    public GameObject PortButton;
    public Dropdown resolutionDropdown;
    public GameObject AutoBlinker;
    public GameObject ResetHandButton;
    public GameObject Panel;
    public GameObject EditExpressionsButton;
    public GameObject OpenColorPickerButton;
    private bool isHidden = false;
    private ResolutionManager resolutionManager;
    private Vector2 originalAnchorMin;  // 按钮初始位置的锚点
    private Vector2 originalAnchorMax;  // 按钮初始位置的锚点
    private Vector2 originalAnchoredPosition;  // 按钮初始锚定位置
    private Transform originalParent;
    private int originalSiblingIndex;
    // Start is called before the first frame update
    void Start()
    {
        Button button = GetComponent<Button>();
        button.onClick.AddListener(ChangeCamera1);
        resolutionManager = FindObjectOfType<ResolutionManager>();
        resolutionManager.EnableResizableWindow();
        ResetHandButton.SetActive(isHidden);

        // 初始化分辨率下拉菜单
        resolutionDropdown.value = 0;
        resolutionDropdown.onValueChanged.AddListener(delegate { OnResolutionChange(); });

        RectTransform rectTransform = CameraButton.GetComponent<RectTransform>();
        originalAnchorMin = rectTransform.anchorMin;
        originalAnchorMax = rectTransform.anchorMax;
        originalAnchoredPosition = rectTransform.anchoredPosition;

        originalParent = CameraButton.transform.parent;
        originalSiblingIndex = CameraButton.transform.GetSiblingIndex();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ChangeCamera1()
    {
        if (MainCamera.active == false)
        {
            MainCamera.active = true;
            Camera.active = false;
        }
        else
        {
            MainCamera.active = false;
            Camera.active = true;

        }

        isHidden = !isHidden;
        Port.SetActive(!isHidden);
        Text.SetActive(!isHidden);
        PortNumber.SetActive(!isHidden);
        PortButton.SetActive(!isHidden);
        resolutionDropdown.gameObject.SetActive(!isHidden);
        AutoBlinker.SetActive(!isHidden);
        EditExpressionsButton.SetActive(!isHidden);
        OpenColorPickerButton.SetActive(!isHidden);
        ResetHandButton.SetActive(isHidden);
        CameraButton.GetComponentInChildren<Text>().text = isHidden ? "Menu" : "Model";

        RectTransform rectTransform = CameraButton.GetComponent<RectTransform>();
        if (isHidden)
        {
            CameraButton.transform.SetParent(Panel.transform, worldPositionStays: false);
            
            rectTransform.anchorMin = new Vector2(1, 0);
            rectTransform.anchorMax = new Vector2(1, 0);
            rectTransform.anchoredPosition = new Vector2(-rectTransform.sizeDelta.x / 2, rectTransform.sizeDelta.y / 2);         
        }
        else
        {
            rectTransform.anchorMin = originalAnchorMin;
            rectTransform.anchorMax = originalAnchorMax;
            rectTransform.anchoredPosition = originalAnchoredPosition;

            CameraButton.transform.SetParent(originalParent, worldPositionStays: false);
            CameraButton.transform.SetSiblingIndex(originalSiblingIndex);
        }
    }

    private void OnResolutionChange()
    {
        int index = resolutionDropdown.value;
        switch (index)
        {
            case 1:
                resolutionManager.SetResolution(1920, 1080, false);
                Debug.Log("1");
                break;
            case 2:
                resolutionManager.SetResolution(1280, 720, false);
                Debug.Log("2");
                break;
            case 3:
                resolutionManager.SetResolution(800, 600, false);
                Debug.Log("3");
                break;
            default:
                resolutionManager.SetResolution(1920, 1080, false);
                break;
        }
    }
}
