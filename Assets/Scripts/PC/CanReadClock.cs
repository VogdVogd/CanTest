#if UNITY_EDITOR
using UnityEngine;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using System.Runtime.InteropServices;

public class CanReadClock
{
    public static void Read(UsbDevice device)
    {
        int bufferSize = Marshal.SizeOf<GsDeviceBtConst>();
        byte[] buffer =
            new byte[bufferSize];

        Debug.Log($"Start read can clock, buffer size: {bufferSize}");

        UsbSetupPacket packet = new UsbSetupPacket(
            GsUsbConstants.RequestTypeDeviceToHost,
            GsUsbConstants.BreqBtConst,
            0,
            0,
            (short)buffer.Length);

        bool success = device.ControlTransfer(
            ref packet,
            buffer,
            buffer.Length,
            out int transferred);

        Debug.Log($"Success: {success}");
        Debug.Log($"Transferred: {transferred}");

        GsDeviceBtConst bt =
            CanStructToBytes.BytesToStruct<GsDeviceBtConst>(buffer);

        Debug.Log($"CAN Clock: {bt.fclk_can}");
    }
}
#endif
