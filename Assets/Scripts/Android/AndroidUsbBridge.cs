using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Minimal test for com.canadapter.usb.CanUsbBridge on Android.
/// Attach to a GameObject named exactly "AndroidUsbTest" (or set _callbackObjectName).
/// </summary>
public class AndroidUsbBridge : MonoBehaviour
{
    public static AndroidUsbBridge Instance;

    private const string BridgeClass = "com.canadapter.usb.CanUsbBridge";

    private static int Vid => GsUsbConstants.Vid;
    private static int Pid => GsUsbConstants.Pid;

    [SerializeField] private string _callbackObjectName = "AndroidUsbBridge";

    private AndroidJavaClass _bridge;
    private bool _opened;
    private Action<uint, byte[]> _receiveCallback;
    private CancellationTokenSource _readCts;
    private readonly object _pendingLock = new object();
    private readonly List<(uint id, byte[] data)> _pendingFrames = new List<(uint id, byte[] data)>();

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

        if (!SetBitTiming())
            return;

        if (!SetNormalMode())
            return;

        _opened = true;
        StartReadLoop();
        TestReadClock();
    }

    private bool ControlTransferOut(int breq, byte[] buffer)
    {
        int transferred = _bridge.CallStatic<int>(
            "controlTransfer",
            GsUsbConstants.RequestTypeHostToDevice,
            breq,
            0,
            0,
            buffer,
            2000
        );

        if (transferred < buffer.Length)
        {
            Debug.LogError($"[AndroidUsb] controlTransfer OUT breq={breq} transferred={transferred}: "
                + _bridge.CallStatic<string>("getLastError"));
            return false;
        }

        return true;
    }

    private bool SetBitTiming()
    {
        Debug.Log("[AndroidUsb] SetBitTiming");
        GsDeviceBitTiming timing = GsUsbConfig.DefaultBitTiming();
        byte[] buffer = CanStructToBytes.StructToBytes(timing);
        return ControlTransferOut(GsUsbConstants.BreqBitTiming, buffer);
    }

    private bool SetNormalMode(bool oneShot = true)
    {
        Debug.Log($"[AndroidUsb] SetNormalMode (oneShot={oneShot})");
        GsDeviceMode mode = GsUsbConfig.NormalMode(oneShot);
        byte[] buffer = CanStructToBytes.StructToBytes(mode);
        return ControlTransferOut(GsUsbConstants.BreqMode, buffer);
    }

    private void TestReadClock()
    {
        byte[] buffer = new byte[System.Runtime.InteropServices.Marshal.SizeOf<GsDeviceBtConst>()];
        // All numeric args must be int — Unity JNI maps C# byte to Java 'B', not 'I'.
        int transferred = _bridge.CallStatic<int>(
            "controlTransfer",
            GsUsbConstants.RequestTypeDeviceToHost,
            (int)GsUsbConstants.BreqBtConst,
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

        GsDeviceBtConst bt = CanStructToBytes.BytesToStruct<GsDeviceBtConst>(buffer);
        Debug.Log($"[AndroidUsb] CAN clock fclk_can={bt.fclk_can} Hz");
    }

    private void Update()
    {
        if (_receiveCallback == null)
            return;

        List<(uint id, byte[] data)> batch = null;
        lock (_pendingLock)
        {
            if (_pendingFrames.Count == 0)
                return;

            batch = new List<(uint id, byte[] data)>(_pendingFrames);
            _pendingFrames.Clear();
        }

        foreach (var (id, data) in batch)
            _receiveCallback(id, data);
    }

    private void StartReadLoop()
    {
        StopReadLoop();
        _readCts = new CancellationTokenSource();
        Task.Run(() => ReadLoop(_readCts.Token));
        Debug.Log("[AndroidUsb] Start listening for CAN frames...");
    }

    private void StopReadLoop()
    {
        _readCts?.Cancel();
        _readCts = null;

        lock (_pendingLock)
            _pendingFrames.Clear();
    }

    private async Task ReadLoop(CancellationToken token)
    {
        AndroidJNI.AttachCurrentThread();

        try
        {
            int frameSize = Marshal.SizeOf<GsHostFrame>();
            byte[] readBuffer = new byte[512];

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (!_opened)
                    {
                        await Task.Delay(100, token);
                        continue;
                    }

                    byte[] chunk = _bridge.CallStatic<byte[]>(
                        "bulkTransferIn",
                        GsUsbConstants.EndpointBulkIn,
                        readBuffer.Length,
                        1000
                    );

                    if (chunk == null || chunk.Length == 0)
                        continue;

                    int offset = 0;
                    while (offset + frameSize <= chunk.Length)
                    {
                        byte[] slice = new byte[frameSize];
                        Array.Copy(chunk, offset, slice, 0, frameSize);

                        GsHostFrame frame = CanStructToBytes.BytesToStruct<GsHostFrame>(slice);
                        OnFrameReceived(frame);

                        offset += frameSize;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError("[AndroidUsb] Read error: " + ex.Message);
                    await Task.Delay(200, token);
                }
            }
        }
        finally
        {
            AndroidJNI.DetachCurrentThread();
        }
    }

    private void OnFrameReceived(GsHostFrame frame)
    {
        Debug.Log(
            $"[AndroidUsb] RX ID: 0x{frame.can_id:X} DLC:{frame.can_dlc} " +
            $"Data: {BitConverter.ToString(frame.data, 0, frame.can_dlc)} " +
            $"Echo: {frame.echo_id:X}");

        byte[] data = new byte[frame.can_dlc];
        if (frame.data != null)
            Array.Copy(frame.data, 0, data, 0, Math.Min(frame.can_dlc, frame.data.Length));

        lock (_pendingLock)
            _pendingFrames.Add((frame.can_id, data));
    }

    public void Dispose()
    {
        StopReadLoop();
        _opened = false;

        if (_bridge != null)
            _bridge.CallStatic("dispose");
    }

    public void Send(uint id, byte[] data)
    {
        if (!_opened)
        {
            Debug.LogError("[AndroidUsb] Send failed: device not opened");
            return;
        }

        GsHostFrame frame = GsUsbConfig.CreateHostFrame(id, data);
        byte[] buffer = CanStructToBytes.StructToBytes(frame);

        int transferred = _bridge.CallStatic<int>(
            "bulkTransferOut",
            GsUsbConstants.EndpointBulkOut,
            buffer,
            2000
        );

        if (transferred < buffer.Length)
        {
            Debug.LogError($"[AndroidUsb] bulkTransferOut transferred={transferred}: "
                + _bridge.CallStatic<string>("getLastError"));
            return;
        }

        Debug.Log(
            $"[AndroidUsb] Send ID: 0x{frame.can_id:X} DLC:{frame.can_dlc} " +
            $"Data: {BitConverter.ToString(frame.data, 0, frame.can_dlc)}");
    }

    public void SetCallback(Action<uint, byte[]> receiveCallback)
    {
        _receiveCallback = receiveCallback;
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
