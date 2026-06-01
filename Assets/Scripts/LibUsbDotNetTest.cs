using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using System.Runtime.InteropServices;
using LibUsbDotNet.Info;
using UnityEngine.UI;

public class LibUsbDotNetTest : MonoBehaviour
{
    [SerializeField] private Button _button;

    private UsbDevice _device;
    private CanReader _canReader;

    private void Start()
    {
        _button.onClick.AddListener(Send);

        _device = CanOpenClose.OpenDevice();
        ShowDescriptors(_device);
        CanBitrate.SetBitTiming(_device);
        //CanMode.EnableLoopMode(_device, false);
        CanMode.SetNormalMode(_device);
        _canReader = new CanReader(_device);

        CanReadClock.Read(_device);
    }

    private void OnDestroy()
    {
        _canReader.Dispose();
        CanOpenClose.CloseDevice(_device);
        _device = null;
    }

    private void Send()
    {
        CanSend.Send(_device);
    }

    private  void ShowDescriptors(UsbDevice device)
    {
        foreach (UsbConfigInfo cfg in device.Configs)
        {
            Debug.Log($"Config: {cfg.Descriptor.ConfigID}");

            foreach (UsbInterfaceInfo iface in cfg.InterfaceInfoList)
            {
                Debug.Log(
                    $"Interface: {iface.Descriptor.InterfaceID}");

                foreach (UsbEndpointInfo ep in iface.EndpointInfoList)
                {
                    Debug.Log(
                        $"Endpoint: 0x{ep.Descriptor.EndpointID:X2}");
                }
            }
        }
    }
}
