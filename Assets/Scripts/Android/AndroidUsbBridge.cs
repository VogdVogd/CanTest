using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimal test for com.canadapter.usb.CanUsbBridge on Android.
/// Attach to a GameObject named exactly "AndroidUsbTest" (or set _callbackObjectName).
/// </summary>
public class AndroidUsbBridge : MonoBehaviour
{
    public static AndroidUsbBridge Instance;

    private const string BridgeClass = "com.canadapter.usb.CanUsbBridge";

    // CandleLight VID/PID
    private const int Vid = 0x1D50;
    private const int Pid = 0x606F;

    [SerializeField] private string _callbackObjectName = "AndroidUsbBridge";

    private AndroidJavaClass _bridge;
    private bool _opened;

#if UNITY_ANDROID && !UNITY_EDITOR
    private void Awake()
    {
        Instance = this;
        _bridge = new AndroidJavaClass(BridgeClass);
        _bridge.CallStatic("setUnityCallbackObject", _callbackObjectName);
        _bridge.CallStatic("init");
    }

    private void OnDestroy()
    {
        Dispose();
    }

    public void Init()
    {
        bool found = _bridge.CallStatic<bool>("findDevice", Vid, Pid);
        Debug.Log($"[AndroidUsb] findDevice: {found}, info={_bridge.CallStatic<string>("getDeviceInfo")}");

        if (!found)
        {
            Debug.LogError("[AndroidUsb] " + _bridge.CallStatic<string>("getLastError"));
            return;
        }

        if (_bridge.CallStatic<bool>("hasPermission"))
            OnPermissionGranted();
        else
            _bridge.CallStatic("requestPermission");
    }

    // Called from Java via UnitySendMessage
    public void OnUsbPermissionGranted(string _)
    {
        OnPermissionGranted();
    }

    public void OnUsbPermissionDenied(string message)
    {
        Debug.LogError("[AndroidUsb] Permission denied: " + message);
    }

    private void OnPermissionGranted()
    {
        if (_opened)
            return;

        bool opened = _bridge.CallStatic<bool>("open", 0);
        Debug.Log($"[AndroidUsb] open: {opened}");

        if (!opened)
        {
            Debug.LogError("[AndroidUsb] " + _bridge.CallStatic<string>("getLastError"));
            return;
        }

        _opened = true;
        TestReadClock();
    }

    private void TestReadClock()
    {
        const byte GS_USB_BREQ_BT_CONST = 4;
        const int bufferSize = 40; // GsDeviceBtConst

        byte[] buffer = new byte[bufferSize];
        // All numeric args must be int — Unity JNI maps C# byte to Java 'B', not 'I'.
        int transferred = _bridge.CallStatic<int>(
            "controlTransfer",
            0xC1, // Device -> Host | Vendor | Interface
            (int)GS_USB_BREQ_BT_CONST,
            0,
            0,
            buffer,
            2000
        );

        Debug.Log($"[AndroidUsb] BT_CONST transferred={transferred}");

        if (transferred < 8)
        {
            Debug.LogError("[AndroidUsb] " + _bridge.CallStatic<string>("getLastError"));
            return;
        }

        uint fclk = System.BitConverter.ToUInt32(buffer, 4);
        Debug.Log($"[AndroidUsb] CAN clock fclk_can={fclk} Hz");
    }

    public void Dispose()
    {
        if (_bridge != null)
            _bridge.CallStatic("dispose");
    }

    public void Send(uint id, byte[] data)
    {
    }

    public void SetCallback(Action<uint, byte[]> receiveCallback)
    {
    }
#else
    private void Awake()
    {
        Instance = this;
    }

    public void Init()
    {

    }

    public void Dispose()
    {

    }

    public void Send(uint id, byte[] data)
    {
    }

    public void SetCallback(Action<uint, byte[]> receiveCallback)
    {
    }
#endif
}
