using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WindowsConfig", menuName = "UI/WindowsConfig")]
public class WindowsConfig : ScriptableObject
{
    public WindowId menuWindowId;
    public WindowId exitConfirmationPopupId;
    public WindowId settingsWindowId;
    public WindowId carSelectionWindowId;
    public WindowId trackSelectionWindowId;
    public WindowId inGameMenuWindowId;
    public WindowId pauseWindowId;
    public WindowId exitToMenuConfirmationPopupId;
    public WindowId finishWindowId;
    public List<WindowId> popupWindowIds = new List<WindowId>();

    public bool IsPopup(WindowId windowId)
    {
        return windowId != null && popupWindowIds != null && popupWindowIds.Contains(windowId);
    }
}
