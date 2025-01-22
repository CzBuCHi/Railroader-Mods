using UnityEngine;

namespace MapEditor.Utility;

public static class InputHelper
{
    public static bool GetControl() => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    public static bool GetShift() => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    public static bool GetAlt() => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
}
