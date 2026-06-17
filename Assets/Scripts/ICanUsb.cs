using System;

public interface ICanUsb : IDisposable
{
    void Init();

    void Send(uint id, byte[] data);
    void SetCallback(Action<uint, byte[]> receiveCallback);
}
