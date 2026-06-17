#if UNITY_EDITOR
using UnityEngine;
using LibUsbDotNet;
using LibUsbDotNet.Main;

public class CanMode
{
    public static void EnableLoopMode(UsbDevice device)
    {
        Debug.Log($"Start set loop");

        SetMode(device, GsUsbConstants.CanModeStart, GsUsbConstants.CanModeLoopBack);
    }

    public static void SetNormalMode(UsbDevice device, bool oneShot = true)
    {
        Debug.Log($"Start set normal mode (oneShot={oneShot}):");
        GsDeviceMode deviceMode = GsUsbConfig.NormalMode(oneShot);
        SetMode(device, deviceMode.mode, deviceMode.flags);
    }

    public static void SetModeReset(UsbDevice device)
    {
        Debug.Log($"Reset:");

        SetMode(device, GsUsbConstants.CanModeReset, GsUsbConstants.CanModeNormal);
    }

    private static void SetMode(UsbDevice device, uint mode, uint flags)
    {
        Debug.Log($"SetMode mode:{mode}, flags:{flags}");
        GsDeviceMode deviceMode = new GsDeviceMode
        {
            mode = mode,
            flags = flags
        };

        byte[] buffer = CanStructToBytes.StructToBytes(deviceMode);

        UsbSetupPacket packet = new UsbSetupPacket(
            GsUsbConstants.RequestTypeHostToDevice,
            GsUsbConstants.BreqMode,
            0,
            0,
            (short)buffer.Length);

        bool success = device.ControlTransfer(
            ref packet,
            buffer,
            buffer.Length,
            out var transferred);

        Debug.Log(success);
        Debug.Log(transferred);
    }
}
#endif
