using UnityEngine;
using UnityEngine.UI;

public class UsbCanTest : MonoBehaviour
{
    [SerializeField] private Button _initButton;
    [SerializeField] private Button _sendButton;

    private UsbCanProxy _proxy = new UsbCanProxy();

    private void Start()
    {
        _sendButton.onClick.AddListener(Send);
        _initButton.onClick.AddListener(Init);
    }

    private void OnDestroy()
    {
        if (_proxy != null)
            _proxy.Dispose();
    }

    private void Send()
    {
        _proxy.Send(0x134, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
    }

    private void Init()
    {
        _proxy.Init();
        _proxy.SetCallback(OnSomethingReceived);
    }

    private void OnSomethingReceived(uint id, byte[] data)
    {
        Debug.Log($"Received: {id}");
    }
}
