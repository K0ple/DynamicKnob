using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Text;
class Program
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;

    private const int VK_VOLUME_UP = 0xAF;
    private const int VK_VOLUME_DOWN = 0xAE;
    const int KEYEVENTF_KEYUP = 0x0002;

    private static LowLevelKeyboardProc _proc = HookCallback;
    private static IntPtr _hookID = IntPtr.Zero;

    private static int WINDOW_SCROLL_START = 0;
    private static int SKIP_SCROLL_INPUT = 2; //Rename as sensitivity
    private static int SKIP_SCROLL_INPUT_COUNT = 0;

    static void Main()
    {
        _hookID = SetHook(_proc);
        Console.WriteLine("Listening for knob input...");
        Application.Run();
        UnhookWindowsHookEx(_hookID);
    }

    static void SendAltTab()
    {
        if (WINDOW_SCROLL_START == 1)
        {
            if (SKIP_SCROLL_INPUT_COUNT > SKIP_SCROLL_INPUT)
            {
                keybd_event((byte)Keys.Right, 0, 0, UIntPtr.Zero); //Right down
                keybd_event((byte)Keys.Right, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); //Right up
                SKIP_SCROLL_INPUT_COUNT = 0;
            }
            else
            {
                SKIP_SCROLL_INPUT_COUNT++;
            }
        }
        else
        {
            keybd_event((byte)Keys.Menu, 0, 0, UIntPtr.Zero);         // Alt down
            keybd_event((byte)Keys.Tab, 0, 0, UIntPtr.Zero);          // Tab down
            keybd_event((byte)Keys.Tab, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Alt up
            WINDOW_SCROLL_START = 1;
        }
    }

    static void SendShiftAltTab()
    {
        if (WINDOW_SCROLL_START == 1)
        {
            if (SKIP_SCROLL_INPUT_COUNT > SKIP_SCROLL_INPUT)
            {
                keybd_event((byte)Keys.Left, 0, 0, UIntPtr.Zero); //Right down
                keybd_event((byte)Keys.Left, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); //Right up
                SKIP_SCROLL_INPUT_COUNT = 0;
            }
            else
            {
                SKIP_SCROLL_INPUT_COUNT++;
            }

        }
        else
        {
            keybd_event((byte)Keys.ShiftKey, 0, 0, UIntPtr.Zero);     // Shift down
            keybd_event((byte)Keys.Menu, 0, 0, UIntPtr.Zero);         // Alt down
            keybd_event((byte)Keys.Tab, 0, 0, UIntPtr.Zero);          // Tab down
            keybd_event((byte)Keys.Tab, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Tab up
            keybd_event((byte)Keys.ShiftKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Shift up
            WINDOW_SCROLL_START = 1;
        }

    }

    static void SendAltTab2()
    {
        keybd_event((byte)Keys.Menu, 0, 0, UIntPtr.Zero);         // Alt down
        keybd_event((byte)Keys.Tab, 0, 0, UIntPtr.Zero);          // Tab down
        keybd_event((byte)Keys.Tab, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Tab up
        keybd_event((byte)Keys.Menu, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Alt up
    }

    static void SendShiftAltTab2()
    {
        keybd_event((byte)Keys.ShiftKey, 0, 0, UIntPtr.Zero);     // Shift down
        keybd_event((byte)Keys.Menu, 0, 0, UIntPtr.Zero);         // Alt down
        keybd_event((byte)Keys.Tab, 0, 0, UIntPtr.Zero);          // Tab down
        keybd_event((byte)Keys.Tab, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Tab up
        keybd_event((byte)Keys.Menu, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Alt up
        keybd_event((byte)Keys.ShiftKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Shift up
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule!)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                GetModuleHandle(curModule.ModuleName!), 0);
        }
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        string app = GetActiveWindowTitle();
        Console.WriteLine($"Active Window: {app}");
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);

            switch (vkCode)
            {
                case VK_VOLUME_UP:
                    Console.WriteLine("Knob turned ⏫");
                    SendAltTab();
                    return (IntPtr)1;

                case VK_VOLUME_DOWN:
                    Console.WriteLine("Knob turned ⏬");
                    SendShiftAltTab();
                    return (IntPtr)1;

                default:
                    // Uncomment below to see all keys if needed-=
                    // Console.WriteLine($"Other key: {(Keys)vkCode} ({vkCode})");
                    break;
            }
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    private static string GetActiveWindowTitle()
    {
        var handle = GetForegroundWindow();
        var buffer = new StringBuilder(256);
        _ = GetWindowText(handle, buffer, buffer.Capacity);
        return buffer.ToString();
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook,
        LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
        IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
}