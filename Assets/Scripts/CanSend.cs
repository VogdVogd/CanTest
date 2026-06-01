using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using System.Runtime.InteropServices;
using LibUsbDotNet.Info;
using UnityEngine.UI;

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

    public static void Send(UsbDevice device)
    {


        // Endpoint 0x01 = bulk OUT
        UsbEndpointWriter writer =
            device.OpenEndpointWriter(
                WriteEndpointID.Ep02);

        // Build CAN frame
        GsHostFrame frame = new GsHostFrame
        {
            echo_id = 0xFFFFFFFF,
            can_id = 0x123,
            can_dlc = 4,
            channel = 0,
            flags = 0,
            reserved = 0,
            data = new byte[8]
        };

        frame.data[0] = 0x11;
        frame.data[1] = 0x22;
        frame.data[2] = 0x33;
        frame.data[3] = 0x44;

        byte[] buffer = CanStructToBytes.StructToBytes(frame);

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
