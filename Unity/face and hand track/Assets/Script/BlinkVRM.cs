using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
namespace VRM
{
    public class BlinkVRM : MonoBehaviour
    {
        VRMBlendShapeProxy m_blendShapes;
        public float Interval = 5.0f;
        public float ClosingTime = 0.06f;
        public float OpeningSeconds = 0.03f;
        public float CloseSeconds = 0.1f;
        Coroutine m_coroutine;
        float m_nextRequest;
        bool m_request;
        public bool Request
        {
            get { return m_request; }
            set
            {
                if (Time.time < m_nextRequest)
                {
                    return;
                }
                m_request = value;
                m_nextRequest = Time.time + 1.0f;
            }
        }
        /// <summary>在自己或子物体里寻找 BlendShapeProxy</summary>
        void RefreshBlendShapeProxy()
        {
            // 先找已有引用是否还活着
            if (m_blendShapes) return;

            // 1) 自己身上
            m_blendShapes = GetComponent<VRMBlendShapeProxy>();

            // 2) 子物体（包括刚替换的新 Model1）
            if (!m_blendShapes)
                m_blendShapes = GetComponentInChildren<VRMBlendShapeProxy>(true);

            if (!m_blendShapes)
                Debug.LogWarning($"{name} 找不到 VRMBlendShapeProxy，请确认 Model1 已作为子物体挂载。");
        }

        /// <summary>模型被替换时会触发 TransformChildrenChanged，可在此重新绑定</summary>
        void OnTransformChildrenChanged() => RefreshBlendShapeProxy();

        IEnumerator BlinkRoutine()
        {
            while (true)
            {
                var waitTime = Time.time + Random.value * Interval;
                while (waitTime > Time.time)
                {
                    if (Request)
                    {
                        m_request = false;
                        break;
                    }
                    yield return null;
                }

                // close
                var value = 0.0f;
                var closeSpeed = 1.0f / CloseSeconds;
                while (true)
                {
                    value += Time.deltaTime * closeSpeed;
                    if (value >= 1.0f)
                    {
                        break;
                    }

                    m_blendShapes.ImmediatelySetValue(BlendShapeKey.CreateFromPreset(BlendShapePreset.Blink), value);
                    yield return null;
                }
                m_blendShapes.ImmediatelySetValue(BlendShapeKey.CreateFromPreset(BlendShapePreset.Blink), 1.0f);

                // wait...
                yield return new WaitForSeconds(ClosingTime);

                // open
                value = 1.0f;
                var openSpeed = 1.0f / OpeningSeconds;
                while (true)
                {
                    value -= Time.deltaTime * openSpeed;
                    if (value < 0)
                    {
                        break;
                    }

                    m_blendShapes.ImmediatelySetValue(BlendShapeKey.CreateFromPreset(BlendShapePreset.Blink), value);
                    yield return null;
                }
                m_blendShapes.ImmediatelySetValue(BlendShapeKey.CreateFromPreset(BlendShapePreset.Blink), 0);
            }
        }

        private void OnEnable()
        {
            RefreshBlendShapeProxy();
            m_coroutine = StartCoroutine(BlinkRoutine());
        }
        private void OnDisable()
        {
            Debug.Log("StopCoroutine");
            if (m_coroutine != null)
            {
                StopCoroutine(m_coroutine);
                m_coroutine = null;
            }
        }
    }

}

