using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using System.Runtime.InteropServices;
using LibUsbDotNet.Info;
using UnityEngine.UI;

public class CanMode
{
    const byte GS_USB_BREQ_MODE = 2;

    const uint GS_CAN_MODE_START = 1;
    const uint GS_CAN_MODE_RESET = 0;

    const uint GS_CAN_MODE_NORMAL = 0;
    const uint GS_CAN_MODE_LOOP_BACK = 1;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct GsDeviceMode
    {
        public uint mode;
        public uint flags;
    }

    public static void EnableLoopMode(UsbDevice device)
    {
        Debug.Log($"Start set loop");

        SetMode(device, GS_CAN_MODE_START, GS_CAN_MODE_LOOP_BACK);
    }

    public static void SetNormalMode(UsbDevice device)
    {
        Debug.Log($"Start set normal mode:");

        SetMode(device, GS_CAN_MODE_START, GS_CAN_MODE_NORMAL);
    }

    public static void SetModeReset(UsbDevice device)
    {
        Debug.Log($"Reset:");

        SetMode(device, GS_CAN_MODE_RESET, GS_CAN_MODE_NORMAL);
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
            0x41,                 // Host -> Device | Vendor | Interface
            GS_USB_BREQ_MODE,
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
