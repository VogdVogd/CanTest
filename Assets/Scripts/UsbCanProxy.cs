using System;

public class UsbCanProxy : ICanUsb
{
    private ICanUsb _inner;

    public void Init()
    {
        if (_inner != null)
        {
            _inner.Dispose();
            _inner = null;
        }

#if UNITY_EDITOR
        _inner = new WinCanUsb();
#elif UNITY_ANDROID
        _inner = new AndroidCanUsb();
#endif
        _inner.Init();
    }

    public void Send(uint id, byte[] data)
    {
        _inner.Send(id, data);
    }
    public void SetCallback(Action<uint, byte[]> receiveCallback)
    {
        _inner.SetCallback(receiveCallback);
    }

    public void Dispose()
    {
        if (_inner != null)
        {
            _inner.Dispose();
            _inner = null;
        }
    }
}
