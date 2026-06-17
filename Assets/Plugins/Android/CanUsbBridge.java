package com.canadapter.usb;

import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.hardware.usb.UsbConstants;
import android.hardware.usb.UsbDevice;
import android.hardware.usb.UsbDeviceConnection;
import android.hardware.usb.UsbEndpoint;
import android.hardware.usb.UsbInterface;
import android.hardware.usb.UsbManager;
import android.os.Build;

import com.unity3d.player.UnityPlayer;

public class CanUsbBridge
{
    private static final String ACTION_USB_PERMISSION = "com.canadapter.usb.USB_PERMISSION";

    private static UsbManager usbManager;
    private static UsbDevice usbDevice;
    private static UsbDeviceConnection connection;
    private static UsbInterface usbInterface;

    private static String lastError = "";
    private static String unityCallbackObject = "";
    private static BroadcastReceiver permissionReceiver;
    private static boolean receiverRegistered;

    public static void setUnityCallbackObject(String objectName)
    {
        unityCallbackObject = objectName != null ? objectName : "";
    }

    public static void init()
    {
        Context context = UnityPlayer.currentActivity;
        usbManager = (UsbManager) context.getSystemService(Context.USB_SERVICE);
        registerPermissionReceiver(context);
    }

    private static void registerPermissionReceiver(Context context)
    {
        if (receiverRegistered)
            return;

        permissionReceiver = new BroadcastReceiver()
        {
            @Override
            public void onReceive(Context ctx, Intent intent)
            {
                if (!ACTION_USB_PERMISSION.equals(intent.getAction()))
                    return;

                UsbDevice device;
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU)
                    device = intent.getParcelableExtra(UsbManager.EXTRA_DEVICE, UsbDevice.class);
                else
                    device = intent.getParcelableExtra(UsbManager.EXTRA_DEVICE);

                if (intent.getBooleanExtra(UsbManager.EXTRA_PERMISSION_GRANTED, false))
                {
                    usbDevice = device;
                    lastError = "";
                    sendUnityMessage("OnUsbPermissionGranted", "");
                }
                else
                {
                    lastError = "USB permission denied";
                    sendUnityMessage("OnUsbPermissionDenied", lastError);
                }
            }
        };

        IntentFilter filter = new IntentFilter(ACTION_USB_PERMISSION);
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU)
            context.registerReceiver(permissionReceiver, filter, Context.RECEIVER_NOT_EXPORTED);
        else
            context.registerReceiver(permissionReceiver, filter);

        receiverRegistered = true;
    }

    public static boolean findDevice(int vid, int pid)
    {
        if (usbManager == null)
        {
            lastError = "Not initialized. Call init() first.";
            return false;
        }

        for (UsbDevice dev : usbManager.getDeviceList().values())
        {
            if (dev.getVendorId() == vid && dev.getProductId() == pid)
            {
                usbDevice = dev;
                lastError = "";
                return true;
            }
        }

        usbDevice = null;
        lastError = "Device not found (VID=0x" + Integer.toHexString(vid)
                + " PID=0x" + Integer.toHexString(pid) + ")";
        return false;
    }

    public static boolean hasPermission()
    {
        return usbDevice != null && usbManager != null && usbManager.hasPermission(usbDevice);
    }

    public static void requestPermission()
    {
        if (usbDevice == null)
        {
            lastError = "No device. Call findDevice() first.";
            sendUnityMessage("OnUsbPermissionDenied", lastError);
            return;
        }

        if (usbManager.hasPermission(usbDevice))
        {
            lastError = "";
            sendUnityMessage("OnUsbPermissionGranted", "");
            return;
        }

        Context context = UnityPlayer.currentActivity;
        int flags = Build.VERSION.SDK_INT >= Build.VERSION_CODES.S
                ? PendingIntent.FLAG_MUTABLE
                : 0;

        PendingIntent pendingIntent = PendingIntent.getBroadcast(
                context,
                0,
                new Intent(ACTION_USB_PERMISSION),
                flags
        );

        usbManager.requestPermission(usbDevice, pendingIntent);
    }

    public static boolean open(int interfaceIndex)
    {
        if (usbDevice == null)
        {
            lastError = "No device";
            return false;
        }

        if (!usbManager.hasPermission(usbDevice))
        {
            lastError = "No USB permission";
            return false;
        }

        close();

        connection = usbManager.openDevice(usbDevice);
        if (connection == null)
        {
            lastError = "openDevice() returned null";
            return false;
        }

        if (interfaceIndex < 0 || interfaceIndex >= usbDevice.getInterfaceCount())
        {
            lastError = "Invalid interface index: " + interfaceIndex;
            connection.close();
            connection = null;
            return false;
        }

        usbInterface = usbDevice.getInterface(interfaceIndex);
        if (!connection.claimInterface(usbInterface, true))
        {
            lastError = "claimInterface() failed";
            connection.close();
            connection = null;
            usbInterface = null;
            return false;
        }

        lastError = "";
        return true;
    }

    public static void close()
    {
        if (connection != null && usbInterface != null)
            connection.releaseInterface(usbInterface);

        if (connection != null)
            connection.close();

        connection = null;
        usbInterface = null;
    }

    public static int controlTransfer(
            int requestType,
            int request,
            int value,
            int index,
            byte[] buffer,
            int timeout)
    {
        if (connection == null)
        {
            lastError = "Not connected";
            return -1;
        }

        int length = buffer != null ? buffer.length : 0;
        int result = connection.controlTransfer(
                requestType,
                request,
                value,
                index,
                buffer,
                length,
                timeout
        );

        if (result < 0)
            lastError = "controlTransfer failed: " + result;

        return result;
    }

    public static int bulkTransferOut(int endpointAddress, byte[] buffer, int timeout)
    {
        if (connection == null || usbInterface == null)
        {
            lastError = "Not connected";
            return -1;
        }

        UsbEndpoint endpoint = findEndpoint(endpointAddress, UsbConstants.USB_DIR_OUT);
        if (endpoint == null)
        {
            lastError = "Bulk OUT endpoint not found: 0x" + Integer.toHexString(endpointAddress);
            return -1;
        }

        int result = connection.bulkTransfer(endpoint, buffer, buffer.length, timeout);
        if (result < 0)
            lastError = "bulkTransfer OUT failed: " + result;

        return result;
    }

    public static byte[] bulkTransferIn(int endpointAddress, int maxSize, int timeout)
    {
        if (connection == null || usbInterface == null)
        {
            lastError = "Not connected";
            return null;
        }

        UsbEndpoint endpoint = findEndpoint(endpointAddress, UsbConstants.USB_DIR_IN);
        if (endpoint == null)
        {
            lastError = "Bulk IN endpoint not found: 0x" + Integer.toHexString(endpointAddress);
            return null;
        }

        byte[] buffer = new byte[maxSize];
        int result = connection.bulkTransfer(endpoint, buffer, maxSize, timeout);
        if (result < 0)
        {
            lastError = "bulkTransfer IN failed: " + result;
            return null;
        }

        byte[] data = new byte[result];
        System.arraycopy(buffer, 0, data, 0, result);
        return data;
    }

    private static UsbEndpoint findEndpoint(int address, int direction)
    {
        for (int i = 0; i < usbInterface.getEndpointCount(); i++)
        {
            UsbEndpoint ep = usbInterface.getEndpoint(i);
            if (ep.getAddress() == address && ep.getDirection() == direction)
                return ep;
        }

        for (int i = 0; i < usbInterface.getEndpointCount(); i++)
        {
            UsbEndpoint ep = usbInterface.getEndpoint(i);
            if (ep.getType() == UsbConstants.USB_ENDPOINT_XFER_BULK
                    && ep.getDirection() == direction)
                return ep;
        }

        return null;
    }

    public static String getLastError()
    {
        return lastError != null ? lastError : "";
    }

    public static String getDeviceInfo()
    {
        if (usbDevice == null)
            return "no device";

        return "VID=0x" + Integer.toHexString(usbDevice.getVendorId())
                + " PID=0x" + Integer.toHexString(usbDevice.getProductId())
                + " interfaces=" + usbDevice.getInterfaceCount();
    }

    public static void dispose()
    {
        close();

        if (receiverRegistered && permissionReceiver != null)
        {
            try
            {
                UnityPlayer.currentActivity.unregisterReceiver(permissionReceiver);
            }
            catch (Exception ignored)
            {
            }

            receiverRegistered = false;
            permissionReceiver = null;
        }
    }

    private static void sendUnityMessage(String method, String param)
    {
        if (unityCallbackObject == null || unityCallbackObject.isEmpty())
            return;

        UnityPlayer.UnitySendMessage(unityCallbackObject, method, param != null ? param : "");
    }
}
