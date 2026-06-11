using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class CotTcpSender : MonoBehaviour
{
    public string host = "127.0.0.1";
    public int port = 8087;

    public float reconnectDelay = 5f;
    private bool _reconnectPending = false;
    public bool disableReconnect = false; // for testing

    private TcpClient _client;
    private NetworkStream _stream;

    void Start()
    {
        Connect();
    }

    private void Connect()
    {
        _reconnectPending = false;
        try
        {
            _client = new TcpClient();
            _client.Connect(host, port);
            _stream = _client.GetStream();
            Debug.Log($"[CotTcpSender] TCP connected to FTS at {host}:{port}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[CotTcpSender] Connect failed ({ex.Message}) — retrying in {reconnectDelay}s");
            ScheduleReconnect();
        }
    }

    private void ScheduleReconnect()
    {
        if (_reconnectPending) return;
        _reconnectPending = true;

        if (disableReconnect) return;
        Invoke(nameof(Connect), reconnectDelay);
    }

    private void CloseConnection()
    {
        try { _stream?.Close(); } catch { /* ignored */ }
        try { _client?.Close(); } catch { /* ignored */ }
        _stream = null;
        _client = null;
    }

    private bool IsConnected()
    {
        if (_client == null || _stream == null) return false;
        if (!_client.Connected) return false;

        // Poll the socket: if it's readable with 0 bytes available, the
        // remote end has closed the connection.
        try
        {
            Socket s = _client.Client;
            return !(s.Poll(0, SelectMode.SelectRead) && s.Available == 0);
        }
        catch
        {
            return false;
        }
    }



    public void SendCot(string xml)
    {
        if (!IsConnected())
        {
            if (_reconnectPending) return;
            Debug.LogWarning("[CotTcpSender] Not connected — discarding message and scheduling reconnect.");
            ScheduleReconnect();
            return;
        }

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(xml + "\n");
            _stream.Write(data, 0, data.Length);
            _stream.Flush();
            //Debug.Log("[CotTcpSender] Sent:\n" + xml);
        }
        catch (Exception ex)
        {
            // The connection was alive according to the socket state check but
            // FTS had already closed it on its end — classic half-open socket.
            Debug.LogWarning($"[CotTcpSender] Send failed ({ex.GetType().Name}: {ex.Message}) — scheduling reconnect.");
            CloseConnection();
            ScheduleReconnect();
        }

    }

    void OnDestroy()
    {
        _stream?.Close();
        _client?.Close();
    }
}