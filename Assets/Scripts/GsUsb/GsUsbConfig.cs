using System;

public static class GsUsbConfig
{
    public static GsDeviceBitTiming DefaultBitTiming()
    {
        return new GsDeviceBitTiming
        {
            prop_seg = 1,
            phase_seg1 = 12,
            phase_seg2 = 2,
            sjw = 1,
            brp = 4
        };
    }

    public static GsDeviceMode NormalMode(bool oneShot = true)
    {
        uint flags = GsUsbConstants.CanModeNormal;
        if (oneShot)
            flags |= GsUsbConstants.CanModeOneShot;

        return new GsDeviceMode
        {
            mode = GsUsbConstants.CanModeStart,
            flags = flags
        };
    }

    public static GsHostFrame CreateHostFrame(uint id, byte[] data)
    {
        byte[] frameData = new byte[8];
        if (data != null)
        {
            int copyLen = Math.Min(data.Length, 8);
            Array.Copy(data, 0, frameData, 0, copyLen);
        }

        return new GsHostFrame
        {
            echo_id = GsUsbConstants.EchoIdNone,
            can_id = id,
            can_dlc = 8,
            channel = 0,
            flags = 0,
            reserved = 0,
            data = frameData
        };
    }
}
