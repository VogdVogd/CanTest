using System;

public class AndroidCanUsb : ICanUsb
{
    public void Dispose()
    {
        AndroidUsbBridge.Instance.Dispose();
    }

    public void Init()
    {
        AndroidUsbBridge.Instance.Init();
    }

    public void Send(uint id, byte[] data)
    {
        AndroidUsbBridge.Instance.Send(id, data);
    }

    public void SetCallback(Action<uint, byte[]> receiveCallback)
    {
        AndroidUsbBridge.Instance.SetCallback(receiveCallback);
    }
}
