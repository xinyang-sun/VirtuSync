using System.IO;
using UnityEngine;
using VRM;
using UniGLTF;                           // ImmediateCaller lives here - note namespace
using VRMShaders;

// Make sure this script wins every race:
[DefaultExecutionOrder(-5000)]           // earlier than default 0 :contentReference[oaicite:3]{index=3}
class EarlyVrmLoader : MonoBehaviour
{
    async void Awake()
    {
        var path = PlayerPrefs.GetString("LastVRMPath", "");
        if (!File.Exists(path)) return;  // nothing to do
        Debug.Log($"[VRMReplacerUI] 已加载并替换模型：{path}");
        // Blocking-style import (same frame).
        byte[] bytes = File.ReadAllBytes(path);
        var ctx = await VrmUtility.LoadBytesAsync(path, bytes, new ImmediateCaller());   // ← key line! :contentReference[oaicite:4]{index=4}
        // 启用渲染
        foreach (var smr in ctx.Root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            smr.enabled = true;
        foreach (var mr in ctx.Root.GetComponentsInChildren<MeshRenderer>(true))
            mr.enabled = true;

        // Parent under M before anyone else runs Start()
        var parent = GameObject.Find("M");
        if (parent)
        {
            ctx.Root.transform.SetParent(parent.transform, false);
            ctx.Root.transform.localPosition = Vector3.zero;
        }
    }
}
