using UnityEngine;
using UnityEngine.UI;

public class ColorPicker : MonoBehaviour
{
    [Header("References")]
    public Slider redSlider;
    public Slider greenSlider;
    public Slider blueSlider;

    [Header("InputField References")]
    public InputField redInput;
    public InputField greenInput;
    public InputField blueInput;

    [Header("Target Camera")]
    public Camera targetCamera;

    public Image colorPreview;

    public GameObject openColorPickerButton;
    public GameObject closeColorPickerButton;
    public GameObject ModelButton;
    public GameObject colorPickerPanel;

    public Color[] presetColors = new Color[3]; // 预配置颜色数组

    void Start()
    {
        // 初始化Slider的范围
        redSlider.minValue = greenSlider.minValue = blueSlider.minValue = 0;
        redSlider.maxValue = greenSlider.maxValue = blueSlider.maxValue = 255;

        // 初始化InputField内容
        redInput.text = ((int)redSlider.value).ToString();
        greenInput.text = ((int)greenSlider.value).ToString();
        blueInput.text = ((int)blueSlider.value).ToString();

        // 添加监听事件
        redSlider.onValueChanged.AddListener(OnRedSliderChanged);
        greenSlider.onValueChanged.AddListener(OnGreenSliderChanged);
        blueSlider.onValueChanged.AddListener(OnBlueSliderChanged);

        redInput.onEndEdit.AddListener(OnRedInputChanged);
        greenInput.onEndEdit.AddListener(OnGreenInputChanged);
        blueInput.onEndEdit.AddListener(OnBlueInputChanged);

        // 初始化颜色
        LoadColor();
        colorPickerPanel.SetActive(false);
    }

    void OnRedSliderChanged(float value)
    {
        redInput.text = ((int)value).ToString();
        UpdateColor();
    }

    void OnGreenSliderChanged(float value)
    {
        greenInput.text = ((int)value).ToString();
        UpdateColor();
    }

    void OnBlueSliderChanged(float value)
    {
        blueInput.text = ((int)value).ToString();
        UpdateColor();
    }

    // InputField输入时更新Slider
    void OnRedInputChanged(string input)
    {
        if (int.TryParse(input, out int value))
        {
            value = Mathf.Clamp(value, 0, 255);
            redSlider.value = value;
        }
        else
        {
            redInput.text = ((int)redSlider.value).ToString();
        }
    }

    void OnGreenInputChanged(string input)
    {
        if (int.TryParse(input, out int value))
        {
            value = Mathf.Clamp(value, 0, 255);
            greenSlider.value = value;
        }
        else
        {
            greenInput.text = ((int)greenSlider.value).ToString();
        }
    }

    void OnBlueInputChanged(string input)
    {
        if (int.TryParse(input, out int value))
        {
            value = Mathf.Clamp(value, 0, 255);
            blueSlider.value = value;
        }
        else
        {
            blueInput.text = ((int)blueSlider.value).ToString();
        }
    }

    void UpdateColor()
    {
        // 将Slider的值从0-255映射到0-1范围
        float r = redSlider.value / 255f;
        float g = greenSlider.value / 255f;
        float b = blueSlider.value / 255f;

        targetCamera.backgroundColor = new Color(r, g, b);
        colorPreview.color = new Color(r, g, b);
        SaveColor();
    }

    public void openColorPicker()
    {
        colorPickerPanel.SetActive(true);
        ModelButton.SetActive(false);
        closeColorPickerButton.SetActive(true);
    }

    public void closeColorPicker()
    {
        colorPickerPanel.SetActive(false);
        ModelButton.SetActive(true);
        openColorPickerButton.SetActive(true);
        closeColorPickerButton.SetActive(false);
    }

    // 应用预配置颜色
    public void ApplyPresetColor(int index)
    {
        if (index < 0 || index >= presetColors.Length) return;

        Color color = presetColors[index];

        // 更新Slider值
        redSlider.value = color.r * 255;
        greenSlider.value = color.g * 255;
        blueSlider.value = color.b * 255;

        // 更新InputField值
        redInput.text = ((int)redSlider.value).ToString();
        greenInput.text = ((int)greenSlider.value).ToString();
        blueInput.text = ((int)blueSlider.value).ToString();

        // 更新背景颜色
        UpdateColor();
    }

    void UpdateSlidersFromColor(Color color)
    {
        redSlider.value = color.r * 255;
        greenSlider.value = color.g * 255;
        blueSlider.value = color.b * 255;
        redInput.text = ((int)(color.r * 255)).ToString();
        greenInput.text = ((int)(color.g * 255)).ToString();
        blueInput.text = ((int)(color.b * 255)).ToString();
    }

    void LoadColor()
    {
        // 从十六进制字符串加载
        if (PlayerPrefs.HasKey("SavedBackgroundColor"))
        {
            string hexColor = PlayerPrefs.GetString("SavedBackgroundColor");
            Color savedColor;
            if (ColorUtility.TryParseHtmlString("#" + hexColor, out savedColor))
            {
                targetCamera.backgroundColor = savedColor;
                UpdateSlidersFromColor(savedColor); // 更新Slider和InputField
            }
        }
    }

    void SaveColor()
        {
            // 获取当前背景颜色
            Color currentColor = targetCamera.backgroundColor;

            // 保存为十六进制字符串（例如 #FF0000）
            string hexColor = ColorUtility.ToHtmlStringRGB(currentColor);
            PlayerPrefs.SetString("SavedBackgroundColor", hexColor);

            PlayerPrefs.Save(); // 立即写入磁盘
        }
}