using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

namespace landmarktest
{
    public class ResetHandButton : MonoBehaviour
    {
        public Button resetHandButton;
        public HandTracking RightHand; // 在检视面板中分配
        public HandTracking LeftHand; // 在检视面板中分配
        private Text buttonText;
        private string originalText;

        void Start()
        {
            buttonText = resetHandButton.GetComponentInChildren<Text>();
            originalText = buttonText.text;
            resetHandButton.onClick.AddListener(OnButtonClick);
        }

        void OnButtonClick()
        {
            StartCoroutine(CountdownCoroutine());
        }

        IEnumerator CountdownCoroutine()
        {
            for (int i = 3; i >= 0; i--)
            {
                buttonText.text = i.ToString();
                yield return new WaitForSeconds(1f);
            }

            RightHand.ResetHandPositions();
            LeftHand.ResetHandPositions();

            buttonText.text = originalText;
        }
    }

}
