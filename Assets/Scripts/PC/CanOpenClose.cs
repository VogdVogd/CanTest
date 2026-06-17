#if UNITY_EDITOR
using UnityEngine;
using LibUsbDotNet;
using LibUsbDotNet.Main;

public class CanOpenClose
{
    public static UsbDevice OpenDevice()
    {
        UsbDevice device;

        // CandleLight VID/PID
        UsbDeviceFinder finder =
            new UsbDeviceFinder(0x1D50, 0x606F);

        device = UsbDevice.OpenUsbDevice(finder);

        if (device == null)
        {
            Debug.LogError("Device not found");
            return null;
        }

        // Configure device
        IUsbDevice wholeUsbDevice = device as IUsbDevice;

        if (wholeUsbDevice != null)
        {
            wholeUsbDevice.SetConfiguration(1);
            wholeUsbDevice.ClaimInterface(0);
        }

        Debug.Log("Device opened");

        return device;
    }

    public static void CloseDevice(UsbDevice device)
    {
        (device as IUsbDevice)?.ReleaseInterface(0);
        device.Close();
        UsbDevice.Exit();

        Debug.Log("Device closed");
    }
}
#endif