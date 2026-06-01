using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

public class DetectComPort : MonoBehaviour
{
    private void Start()
    {
        var names = SerialPort.GetPortNames();
        Debug.LogError($"DetectComPort amount: {names.Length}");
        foreach (string name in names)
        {
            try
            {
                using var port = new SerialPort(name, 115200);

                port.ReadTimeout = 300;
                port.WriteTimeout = 300;

                port.Open();

                port.Write("V\r");

                Thread.Sleep(100);

                string response = port.ReadExisting();

                Debug.LogError($"{name} -> {response}");

                port.Close();
            }
            catch (Exception exception)
            {
                Debug.LogError($"DetectComPort error: {exception.Message}");
            }
        }
    }
}
