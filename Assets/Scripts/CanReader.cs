using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using System.Runtime.InteropServices;
using LibUsbDotNet.Info;
using UnityEngine.UI;
using System.Threading.Tasks;

public class CanReader : IDisposable
{
    private UsbEndpointReader _reader;
    private CancellationTokenSource _cts;

    public CanReader(UsbDevice device)
    {
        _reader = device.OpenEndpointReader(ReadEndpointID.Ep01);
        _cts = new CancellationTokenSource();

        Debug.Log("Start listening for CAN frames...");

        Task.Run(() => ReadLoop(_cts.Token));
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _reader?.Dispose();
    }

    private async Task ReadLoop(CancellationToken token)
    {
        byte[] buffer = new byte[512];

        int frameSize = Marshal.SizeOf<CanSend.GsHostFrame>();

        while (!token.IsCancellationRequested)
        {
            try
            {
                ErrorCode ec = _reader.Read(
                    buffer,
                    1000,
                    out int bytesRead);

                if (ec != ErrorCode.None || bytesRead <= 0)
                    continue;

                int offset = 0;

                while (offset + frameSize <= bytesRead)
                {
                    byte[] slice = new byte[frameSize];

                    Array.Copy(buffer, offset, slice, 0, frameSize);

                    var frame = CanStructToBytes.BytesToStruct<CanSend.GsHostFrame>(slice);

                    OnFrameCatched(frame);

                    offset += frameSize;
                }

                await Task.Yield();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Read error: " + ex.Message);
                await Task.Delay(200);
            }
        }
    }

    private void OnFrameCatched(CanSend.GsHostFrame frame)
    {
        Debug.Log(
                $"ID: 0x{frame.can_id:X} DLC:{frame.can_dlc} " +
                $"Data: {BitConverter.ToString(frame.data, 0, frame.can_dlc)}" +
                $"Echo: {frame.echo_id:X}") ;
    }
}
