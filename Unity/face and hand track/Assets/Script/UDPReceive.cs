using UnityEngine;
using UnityEngine.UI;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class UDPReceive : MonoBehaviour
{
    Thread receiveThread;
    UdpClient client = null;
    public int port = 5056;
    public InputField portInputField;
    public Button updatePortButton;
    public Text Ptext;
    public bool isReceiving = false;
    public bool printToConsole = false;
    public string data;

    void Start()
    {
        portInputField.text = port.ToString();
        Ptext.text = port.ToString();
        updatePortButton.onClick.AddListener(ToggleReceive);
        UpdateButtonText();
    }

    void ToggleReceive()
    {
        if (isReceiving)
        {
            StopReceiving();
        }
        else
        {
            UpdatePort();
        }
        UpdateButtonText();
    }

    void UpdatePort()
    {
        if (client != null)
        {
            client.Close(); // Close the existing UDP client
            receiveThread.Abort(); // Stop the existing thread
        }

        if (int.TryParse(portInputField.text, out int newPort))
        {
            port = newPort;
            Ptext.text = port.ToString();
            isReceiving = true;
            Debug.Log($"Port updated to: {port}");
            RestartReceiving();
        }
        else
        {
            Debug.LogError("Invalid port number provided.");
        }
    }

    void RestartReceiving()
    {
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.Start();
        //Debug.Log("UDP receiving restarted on port: " + port);
    }

    private void ReceiveData()
    {
        client = new UdpClient(port);
        while (isReceiving)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] dataByte = client.Receive(ref anyIP);
                data = Encoding.UTF8.GetString(dataByte);
                if (printToConsole) { print(data); }
            }
            catch (Exception err)
            {
                print(err.ToString());
            }
        }
    }

    void StopReceiving()
    {
        if (client != null)
        {
            client.Close();
        }
        if (receiveThread != null)
        {
            receiveThread.Abort();
        }
        isReceiving = false;
        Debug.Log("UDP receiving stopped.");
    }

    void UpdateButtonText()
    {
        updatePortButton.GetComponentInChildren<Text>().text = isReceiving ? "Stop" : "Start";
    }

    private void OnDisable()
    {
        if (client != null)
        {
            client.Close();
        }
        if (receiveThread != null)
        {
            receiveThread.Abort();
        }
    }
}
