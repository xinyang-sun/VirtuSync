using System.IO;
using UnityEngine;
using UnityEngine.UI;
using SFB;                             // StandaloneFileBrowser 插件命名空间
using UniGLTF;                         // RuntimeOnlyAwaitCaller
using VRMShaders; // RuntimeOnlyAwaitCaller 命名空间
using VRM;
using UnityEngine.SceneManagement;

public class VRMReplacerUI : MonoBehaviour
{
    [Header("UI 绑定")]
    public Button replaceButton;       // Inspector 拖入 Button
    [Header("模型根节点")]
    public GameObject M;               // Inspector 指向场景中 M 物体

    private GameObject currentModel;   // 当前加载的模型实例
    private const string PrefKey = "LastVRMPath";

    public bool IsLoaded { get; private set; } = false;  // 新增：加载完成标志

    void Awake()
    {
        // // 启动时尝试恢复
        // if (PlayerPrefs.HasKey(PrefKey))
        // {
        //     string savedPath = PlayerPrefs.GetString(PrefKey);
        //     if (File.Exists(savedPath)) // 确保文件仍存在
        //     {
        //         LoadAndReplace(savedPath);
        //     }
        //     else
        //     {
        //         Debug.LogWarning($"[VRMReplacerUI] 未找到上次保存的模型：{savedPath}");
        //     }
        // }
    }

    void Start()
    {
        if (replaceButton != null)
            replaceButton.onClick.AddListener(OnReplaceClicked);
        else
            Debug.LogWarning("[VRMReplacerUI] replaceButton 未绑定");
    }

    void OnReplaceClicked()
    {
        // 打开本地文件对话框，只过滤 .vrm
        var paths = StandaloneFileBrowser.OpenFilePanel(
            "Select VRM Model",
            "",
            new[] { new ExtensionFilter("VRM Files", "vrm") },
            false
        );

        if (paths != null && paths.Length > 0)
        {
            // 复制并记录路径，再加载
            string userPath = paths[0];
            PlayerPrefs.SetString("LastVRMPath", userPath);   // 保存原始模型地址
            PlayerPrefs.Save();
            // LoadAndReplace(userPath);
            var scene = SceneManager.GetActiveScene();            // 取得当前场景
            SceneManager.LoadScene(scene.buildIndex);
        }
    }

    // public async void LoadAndReplace(string vrmPath)
    // {
    //     byte[] vrmBytes = File.ReadAllBytes(vrmPath);

    //     // 销毁旧模型
    //     if (currentModel != null)
    //         Destroy(currentModel);

    //     // 异步加载
    //     var context = await VrmUtility.LoadBytesAsync(
    //         vrmPath,
    //         vrmBytes,
    //         new RuntimeOnlyAwaitCaller()
    //     );

    //     // 启用渲染组件（针对 VRM0.x）
    //     foreach (var smr in context.Root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
    //         smr.enabled = true;
    //     foreach (var mr in context.Root.GetComponentsInChildren<MeshRenderer>(true))
    //         mr.enabled = true;

    //     // 重置 Transform 并挂到 M 下
    //     var root = context.Root;
    //     root.transform.SetParent(M.transform, false);
    //     root.transform.localPosition = Vector3.zero;
    //     root.transform.localRotation = Quaternion.identity;
    //     root.transform.localScale = Vector3.one;

    //     currentModel = root;
    //     IsLoaded = true;
    //     Debug.Log($"[VRMReplacerUI] 已加载并替换模型：{vrmPath}");
    // }
}
