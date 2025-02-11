using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VRM;

public class VRMFacialController : MonoBehaviour
{
    // 表情配置数据类
    [System.Serializable]
    public class ExpressionConfig
    {
        public KeyCode triggerKey;
        public BlendShapeKey blendShapeKey; // 使用 BlendShapeKey
        public float targetWeight;
        public float transitionSpeed = 5f;
    }

    [Header("VRM Model")]
    public GameObject vrmModel;

    [Header("Configuration")]
    public List<ExpressionConfig> expressionConfigs = new List<ExpressionConfig>();

    [Header("UI Elements")]
    public GameObject configPanel;
    public Button openConfigurationButton;

    // 这里是5个Dropdown的数组，每个对应expressionConfigs里的一条表情配置
    public Dropdown[] blendShapeDropdowns;
    public InputField[] keyInputFields;
    public Slider[] weightSliders;
    public KeyCode resetKey = KeyCode.Alpha0;
    public InputField resetKeyInputField;

    private VRMBlendShapeProxy blendShapeProxy;
    private Dictionary<BlendShapeKey, float> blendShapeWeights = new Dictionary<BlendShapeKey, float>();

    void Start()
    {
        InitializeVRMComponents();
        PopulateBlendShapeList(); // 初始化下拉选单选项
        LoadSavedConfig();

        // 给唯一的“打开配置”按钮绑定点击事件
        if (openConfigurationButton != null)
        {
            openConfigurationButton.onClick.AddListener(OpenConfiguration);
        }
    }

    void Update()
    {
        HandleKeyboardInput();
        UpdateExpressionWeights();
    }

    void InitializeVRMComponents()
    {
        if (vrmModel == null) return;

        blendShapeProxy = vrmModel.GetComponent<VRMBlendShapeProxy>();
        if (blendShapeProxy != null)
        {
            blendShapeProxy.Apply();
        }
    }

    void HandleKeyboardInput()
    {
        // 按下数字键 0 后清空所有表情
        if (Input.GetKeyDown(resetKey))
        {
            ResetAllExpressions();
        }

        // 如果用户按下配置的按键，则激活对应表情
        foreach (var config in expressionConfigs)
        {
            if (Input.GetKeyDown(config.triggerKey))
            {
                ActivateExpression(config);
            }
        }

    }



    void ActivateExpression(ExpressionConfig config)
    {
        if (blendShapeProxy == null) return;
        // 将该配置的BlendShapeKey写入字典，并赋予targetWeight
        blendShapeWeights[config.blendShapeKey] = config.targetWeight;
    }

    void UpdateExpressionWeights()
    {
        if (blendShapeProxy == null) return;

        var values = new Dictionary<BlendShapeKey, float>();

        // 根据当前是否按下此配置的按键，进行插值
        foreach (var config in expressionConfigs)
        {
            if (config.blendShapeKey.Equals(default(BlendShapeKey)))
    {
        //Debug.LogWarning("检测到 ExpressionConfig 的 blendShapeKey 为 null，跳过此配置。");
        continue;
    }
            if (!blendShapeWeights.ContainsKey(config.blendShapeKey))
            {
                blendShapeWeights[config.blendShapeKey] = 0f;
            }

            float current = blendShapeWeights[config.blendShapeKey];
            float target = Input.GetKey(config.triggerKey) ? config.targetWeight : 0f;

            float newValue = Mathf.Lerp(current, target,
                Time.deltaTime * config.transitionSpeed);

            blendShapeWeights[config.blendShapeKey] = newValue;
            values[config.blendShapeKey] = newValue;
        }

        // 一次性设置给Proxy
        blendShapeProxy.SetValues(values);
    }

    // 配置界面相关方法
    public void OpenConfiguration()
    {
        configPanel.SetActive(true);
        openConfigurationButton.gameObject.SetActive(false);

        // 将 expressionConfigs 的数据回填到每组 UI 中
        for (int i = 0; i < expressionConfigs.Count; i++)
        {
            // 如果UI数组的大小小于expressionConfigs数量，需做安全检查
            if (i >= keyInputFields.Length ||
                i >= blendShapeDropdowns.Length ||
                i >= weightSliders.Length)
            {
                Debug.LogWarning($"UI数组长度不足，无法显示第 {i} 项配置");
                break;
            }

            var config = expressionConfigs[i];

            // 让UI显示当前的配置
            keyInputFields[i].text = config.triggerKey.ToString();
            weightSliders[i].value = config.targetWeight;

            // 根据 BlendShapeKey 的 Preset 找到Dropdown对应选项
            var presetName = config.blendShapeKey.Preset.ToString();
            var dropdown = blendShapeDropdowns[i];
            if (dropdown.options != null)
            {
                int idx = dropdown.options.FindIndex(opt => opt.text == presetName);
                if (idx >= 0)
                {
                    dropdown.value = idx;
                }
            }
        }
    }

    public void SaveConfiguration()
    {
        // 将UI的修改写回 expressionConfigs
        for (int i = 0; i < expressionConfigs.Count; i++)
        {
            if (i >= keyInputFields.Length ||
                i >= blendShapeDropdowns.Length ||
                i >= weightSliders.Length)
            {
                break;
            }

            var config = expressionConfigs[i];

            // 读取 InputField 文本，转换后解析为 KeyCode
            string keyStr = ConvertKeyString(keyInputFields[i].text);
            config.triggerKey = (KeyCode)Enum.Parse(typeof(KeyCode), keyStr);

            // 从Dropdown读取新的Preset
            var dropdown = blendShapeDropdowns[i];
            var presetString = dropdown.options[dropdown.value].text;
            var preset = (BlendShapePreset)Enum.Parse(typeof(BlendShapePreset), presetString);
            config.blendShapeKey = BlendShapeKey.CreateFromPreset(preset);

            // 从Slider读取新的权重
            config.targetWeight = weightSliders[i].value;
        }

        // 对于清空表情的按键也同样处理（如果你也使用 InputField 来修改 resetKey）
        string resetKeyStr = ConvertKeyString(resetKeyInputField.text);
        KeyCode newKey;
        if (Enum.TryParse<KeyCode>(resetKeyStr, out newKey))
        {
            resetKey = newKey;
        }
        else
        {
            //Debug.LogError("输入的按键名称无效，请输入一个有效的 KeyCode 字符串（例如：Alpha0, Space, A 等）。");
            resetKey = KeyCode.Alpha0;
        }

        // 保存到 PlayerPrefs
        SaveToPlayerPrefs();

        // 关闭配置面板
        configPanel.SetActive(false);
        openConfigurationButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// 初始化下拉列表选项
    /// </summary>
    void PopulateBlendShapeList()
    {
        // 先收集模型中的可用 Preset
        List<string> shapeNames = new List<string>();
        if (blendShapeProxy != null && blendShapeProxy.BlendShapeAvatar != null)
        {
            foreach (var clip in blendShapeProxy.BlendShapeAvatar.Clips)
            {
                shapeNames.Add(clip.Preset.ToString());
            }
        }

        // 依次对每个Dropdown进行 ClearOptions() 和 AddOptions()
        foreach (var dropdown in blendShapeDropdowns)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(shapeNames);
        }
    }

    // 持久化配置
    void SaveToPlayerPrefs()
    {
        for (int i = 0; i < expressionConfigs.Count; i++)
        {
            PlayerPrefs.SetString($"ExpressionConfig_{i}_Key",
                expressionConfigs[i].triggerKey.ToString());
            PlayerPrefs.SetString($"ExpressionConfig_{i}_Shape",
                expressionConfigs[i].blendShapeKey.Preset.ToString());
            PlayerPrefs.SetFloat($"ExpressionConfig_{i}_Weight",
                expressionConfigs[i].targetWeight);
        }
    }

    void LoadSavedConfig()
    {
        for (int i = 0; i < expressionConfigs.Count; i++)
        {
            // 若没有存储，说明是首次运行或尚未保存过
            if (PlayerPrefs.HasKey($"ExpressionConfig_{i}_Key"))
            {
                expressionConfigs[i].triggerKey = (KeyCode)Enum.Parse(
                    typeof(KeyCode),
                    PlayerPrefs.GetString($"ExpressionConfig_{i}_Key"));

                var preset = (BlendShapePreset)Enum.Parse(
                    typeof(BlendShapePreset),
                    PlayerPrefs.GetString($"ExpressionConfig_{i}_Shape"));

                expressionConfigs[i].blendShapeKey = BlendShapeKey.CreateFromPreset(preset);

                expressionConfigs[i].targetWeight =
                    PlayerPrefs.GetFloat($"ExpressionConfig_{i}_Weight");
            }
        }
    }

    public void ResetAllExpressions()
    {
        if (blendShapeProxy == null) return;

        var values = new Dictionary<BlendShapeKey, float>();
        foreach (var clip in blendShapeProxy.BlendShapeAvatar.Clips)
        {
            var key = BlendShapeKey.CreateFromClip(clip);
            values[key] = 0f;
        }
        blendShapeWeights.Clear();
        blendShapeProxy.SetValues(values);
    }

    private string ConvertKeyString(string input)
    {
        // 如果输入长度为1且为数字，则转换为 "Alpha" + 数字
        if (input.Length == 1 && char.IsDigit(input[0]))
        {
            return "Alpha" + input;
        }
        return input;
    }
}
