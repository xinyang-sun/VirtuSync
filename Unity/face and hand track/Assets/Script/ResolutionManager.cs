using UnityEngine;

public class ResolutionManager : MonoBehaviour
{
    public void SetResolution(int width, int height, bool fullScreen)
    {
        Screen.SetResolution(width, height, fullScreen);
        Screen.fullScreenMode = FullScreenMode.Windowed;
    }
    public void EnableResizableWindow()
    {
        Screen.fullScreenMode = FullScreenMode.Windowed;
    }
}
