using System.Runtime.InteropServices;

namespace MediaRelic.Ui;

public static class NativeDrag
{
    private const int WmNclButtonDown = 0xA1;
    private const int HtCaption = 0x2;

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

    public static void MoveWindow(IntPtr handle)
    {
        ReleaseCapture();
        SendMessage(handle, WmNclButtonDown, HtCaption, 0);
    }

    public static void ResizeWindow(IntPtr handle, int hitTestCode)
    {
        ReleaseCapture();
        SendMessage(handle, WmNclButtonDown, hitTestCode, 0);
    }
}
