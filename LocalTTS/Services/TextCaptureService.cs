using System.Runtime.InteropServices;
using System.Windows;

namespace LocalTTS.Services;

public static class TextCaptureService {
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private const byte VK_CONTROL = 0x11;
    private const byte VK_SHIFT = 0x10;
    private const byte VK_MENU = 0x12; // Alt
    private const byte VK_C = 0x43;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    public static string? CaptureSelectedText() {
        var hwnd = GetForegroundWindow();
        Log.Info($"Foreground window: {hwnd}");

        // Release ALL modifier keys and wait until they're actually up
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

        // Poll until modifiers are confirmed released (max 500ms)
        for (var i = 0; i < 50; i++) {
            Thread.Sleep(10);
            var ctrlHeld = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
            var shiftHeld = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;
            if (!ctrlHeld && !shiftHeld) {
                break;
            }
            // Re-send release in case the physical key is still down
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        var ctrlState = GetAsyncKeyState(VK_CONTROL);
        var shiftState = GetAsyncKeyState(VK_SHIFT);
        Log.Info($"After release - Ctrl: {ctrlState}, Shift: {shiftState}");

        // Save current clipboard
        string? previousClipboard = null;
        try {
            if (Clipboard.ContainsText()) {
                previousClipboard = Clipboard.GetText();
            }

            Log.Info($"Previous clipboard: {(previousClipboard != null ? $"{previousClipboard.Length} chars" : "empty")}");
        } catch (Exception ex) {
            Log.Error("Failed to read clipboard", ex);
        }

        try {
            Clipboard.SetDataObject(new DataObject(), true);
            Log.Info("Clipboard cleared");
        } catch (Exception ex) {
            Log.Error("Failed to clear clipboard", ex);
        }

        // Send Ctrl+C via keybd_event with sufficient delays
        Log.Info("Sending Ctrl+C via keybd_event...");
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        Thread.Sleep(60);
        keybd_event(VK_C, 0, 0, UIntPtr.Zero);
        Thread.Sleep(60);
        keybd_event(VK_C, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        Thread.Sleep(60);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

        Log.Info("Waiting for clipboard...");
        Thread.Sleep(300);

        string? text = null;
        try {
            var data = Clipboard.GetDataObject();
            var formats = data?.GetFormats() ?? [];
            Log.Info($"Clipboard formats: {string.Join(", ", formats)}");
            if (Clipboard.ContainsText()) {
                text = Clipboard.GetText();
            }
        } catch (Exception ex) {
            Log.Error("Failed to read clipboard after copy", ex);
        }

        Log.Info($"Captured text: {(text != null ? $"{text.Length} chars" : "null")}");

        // Restore previous clipboard
        try {
            if (previousClipboard != null) {
                Clipboard.SetText(previousClipboard);
            }
        } catch { }

        return text;
    }
}
