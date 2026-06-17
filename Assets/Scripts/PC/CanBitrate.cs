#if UNITY_EDITOR
using UnityEngine;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using System.Runtime.InteropServices;

public class CanBitrate
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct GsDeviceBitTiming
    {
        public uint prop_seg;
        public uint phase_seg1;
        public uint phase_seg2;
        public uint sjw;
        public uint brp;
    }

    const byte GS_USB_BREQ_BITTIMING = 1;

    public static void SetBitTiming(UsbDevice device)
    {
        Debug.Log($"Start set bittiming");
        // bitrate = fclk_can / (brp * (1 + prop_seg + phase_seg1 + phase_seg2))
        //
        // Target: 5 kbit/s
        // STM32 on bus (36 MHz): brp=450, tq=16 → 36_000_000 / (450 * 16) = 5000
        // USB adapter (48 MHz):   brp=600, tq=16 → 48_000_000 / (600 * 16) = 5000
        // Check fclk_can via CanReadClock.Read() and pick matching brp.
        GsDeviceBitTiming timing = new GsDeviceBitTiming
        {
            prop_seg = 1,
            phase_seg1 = 12,
            phase_seg2 = 2,
            sjw = 1,
            brp = 4  // 5 kbit/s @ 48 MHz adapter; use brp=450 if fclk_can = 36 MHz
        };

        byte[] buffer = CanStructToBytes.StructToBytes(timing);

        UsbSetupPacket packet = new UsbSetupPacket(
            0x41,                 // Host -> Device | Vendor | Interface
            GS_USB_BREQ_BITTIMING,
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