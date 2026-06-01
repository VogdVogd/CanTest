using UnityEngine;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using System.Runtime.InteropServices;
using System;

public class CanReadClock
{
    const byte GS_USB_BREQ_BT_CONST = 4;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct GsDeviceBtConst
    {
        public uint feature;
        public uint fclk_can;

        public uint tseg1_min;
        public uint tseg1_max;

        public uint tseg2_min;
        public uint tseg2_max;

        public uint sjw_max;

        public uint brp_min;
        public uint brp_max;
        public uint brp_inc;
    }
    public static void Read(UsbDevice device)
    {
        int bufferSize = Marshal.SizeOf<GsDeviceBtConst>();
        byte[] buffer =
            new byte[bufferSize];

        Debug.Log($"Start read can clock, buffer size: {bufferSize}");

        UsbSetupPacket packet = new UsbSetupPacket(
            0xC1, // Device->Host | Vendor | Interface
            GS_USB_BREQ_BT_CONST,
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
