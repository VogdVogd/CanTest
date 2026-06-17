#if UNITY_EDITOR
using System;
using UnityEngine;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using System.Runtime.InteropServices;

public class CanSend
{
    // gs_usb host frame
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GsHostFrame
    {
        public uint echo_id;
        public uint can_id;

        public byte can_dlc;
        public byte channel;
        public byte flags;
        public byte reserved;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] data;
    }

    public static void Send(UsbDevice device, uint id, byte[] data)
    {
        // Endpoint 0x01 = bulk OUT
        UsbEndpointWriter writer =
            device.OpenEndpointWriter(
                WriteEndpointID.Ep02);

        // Build CAN frame
        GsHostFrame frame = new GsHostFrame
        {
            echo_id = 0xFFFFFFFF,
            can_id = id,  // StdId, CAN_ID_STD + CAN_RTR_DATA (no flags)
            can_dlc = 8,
            channel = 0,
            flags = 0,
            reserved = 0,
            data = data
        };

        byte[] buffer = CanStructToBytes.StructToBytes(frame);
        /*
        if (buffer.Length > 0)
        {
            Debug.LogError(
                $"RAW Send ({buffer.Length}): " +
                BitConverter.ToString(buffer, 0, buffer.Length));
        }
        */
        // Send
        ErrorCode ec = writer.Write(
            buffer,
            2000,
            out int transferred);

        Debug.Log(
               $"ID: 0x{frame.can_id:X} DLC:{frame.can_dlc} " +
               $"Data: {BitConverter.ToString(frame.data, 0, frame.can_dlc)}" +
               $"Echo: {frame.echo_id:X}");

        Debug.Log($"Write result: {ec}");
        Debug.Log($"Transferred: {transferred}");
    }
}
#endif