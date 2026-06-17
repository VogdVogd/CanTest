#if UNITY_EDITOR
using UnityEngine;
using LibUsbDotNet;
using LibUsbDotNet.Main;

public class CanBitrate
{
    public static void SetBitTiming(UsbDevice device)
    {
        Debug.Log($"Start set bittiming");
        GsDeviceBitTiming timing = GsUsbConfig.DefaultBitTiming();

        byte[] buffer = CanStructToBytes.StructToBytes(timing);

        UsbSetupPacket packet = new UsbSetupPacket(
            GsUsbConstants.RequestTypeHostToDevice,
            GsUsbConstants.BreqBitTiming,
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
