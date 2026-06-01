using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using System.Runtime.InteropServices;
using LibUsbDotNet.Info;
using UnityEngine.UI;

public class CanStructToBytes
{
    public static byte[] StructToBytes<T>(T obj)
    {
        int size = Marshal.SizeOf(obj);

        byte[] arr = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(obj, ptr, true);

        Marshal.Copy(ptr, arr, 0, size);

        Marshal.FreeHGlobal(ptr);

        return arr;
    }

    public static T BytesToStruct<T>(byte[] bytes)
    {
        IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);

        Marshal.Copy(bytes, 0, ptr, bytes.Length);

        T obj = Marshal.PtrToStructure<T>(ptr);

        Marshal.FreeHGlobal(ptr);

        return obj;
    }
}
