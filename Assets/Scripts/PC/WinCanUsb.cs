#if UNITY_EDITOR
using System;
using LibUsbDotNet;

public class WinCanUsb : ICanUsb
{
    private UsbDevice _device;
    private CanReader _canReader;
    private Action<uint, byte[]> _callback;
    public void Init()
    {
        Dispose();

        _device = CanOpenClose.OpenDevice();

        if (_device == null)
            return;

        CanBitrate.SetBitTiming(_device);
        CanMode.SetNormalMode(_device);
        _canReader = new CanReader(_device, OnDataReceived);

        CanReadClock.Read(_device);
    }

    public void Send(uint id, byte[] data)
    {
        CanSend.Send(_device, id, data);
    }

    public void SetCallback(Action<uint, byte[]> receiveCallback)
    {
        _callback = receiveCallback;
    }

    private void OnDataReceived(uint id, byte[] data)
    {
        _callback?.Invoke(id, data);
    }

    public void Dispose()
    {
        if (_canReader != null)
            _canReader.Dispose();
        if (_device != null)
        {
            CanOpenClose.CloseDevice(_device);
            _device = null;
        }
    }
}
#endif