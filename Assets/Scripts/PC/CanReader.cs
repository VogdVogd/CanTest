#if UNITY_EDITOR
using System;
using System.Threading;
using UnityEngine;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

public class CanReader : IDisposable
{
    private UsbEndpointReader _reader;
    private CancellationTokenSource _cts;
    private Action<uint, byte[]> _callback;

    public CanReader(UsbDevice device, Action<uint, byte[]> callback)
    {
        _callback = callback;
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

        int frameSize = Marshal.SizeOf<GsHostFrame>();

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

                    var frame = CanStructToBytes.BytesToStruct<GsHostFrame>(slice);

                    OnFrameCatched(frame);

                    offset += frameSize;
                }

                await Task.Yield();
            }
            catch (Exception ex)
            {
                Debug.LogError("Read error: " + ex.Message);
                await Task.Delay(200);
            }
        }
    }

    private void OnFrameCatched(GsHostFrame frame)
    {
        Debug.Log(
                $"ID: 0x{frame.can_id:X} DLC:{frame.can_dlc} " +
                $"Data: {BitConverter.ToString(frame.data, 0, frame.can_dlc)}" +
                $"Echo: {frame.echo_id:X}") ;

        _callback?.Invoke(frame.can_id, frame.data);
    }
}
#endif
