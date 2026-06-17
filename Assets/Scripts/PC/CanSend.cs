#if UNITY_EDITOR
using System;
using UnityEngine;
using LibUsbDotNet;
using LibUsbDotNet.Main;

public class CanSend
{
    public static void Send(UsbDevice device, uint id, byte[] data)
    {
        UsbEndpointWriter writer =
            device.OpenEndpointWriter(
                WriteEndpointID.Ep02);

        GsHostFrame frame = GsUsbConfig.CreateHostFrame(id, data);

        byte[] buffer = CanStructToBytes.StructToBytes(frame);

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
