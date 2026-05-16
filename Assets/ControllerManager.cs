using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
public class ControllerManager : MonoBehaviour
{
    private Rect SectorX;
    private Rect SectorY;
    private Rect SectorZ;
    private SectorState sentX;
    private SectorState sentY;
    private SectorState sentZ;

    public float SectorSize;
    public float SectorDistance;
    private Texture texture;
    private GUIStyle style;
    private string message = "Chemicalizer Controller";
    private GUIStyle messageStyle;
    private Rect messageRect;

    private bool isConnected = false;
    private float lastSendTime = 0f;
    public float targetSendRate = 120f;
    private int discoveryPort = 50101;
    private int inputPort = 51111;
    private int settingPort = 51999;
    private byte discoveryByte = 255;
    private byte discoverySendByte = 254;
    private byte heartBeatByte = 240;
    private UdpClient discoveryClient;
    private Thread discoveryThread;
    private bool discoveryReceived = false;
    private float lastDiscoveryTime = 0f;
    private UdpClient inputClient;
    private UdpClient settingClient;
    private Thread listenThread;
    private bool isRunning = true;
    private string serverIP = "";
    private float lastPhoneHeartBeatSent = 0f;
    public float heartBeatInterval = 0.5f;
    private bool recievedHeartBeat = false;
    private float lastHeartBeatRecieved = 0f;
    private float connectionTimeout = 3.0f;
    private float minInterval => 1f / targetSendRate;
    private AndroidJavaObject multicastLock;

    public enum SectorState : byte
    {
        None = 0,
        Tapped = 1,
        SlidedUp = 2,
        SlidedUpperRight = 3,
        SlidedRight = 4,
        SlidedLowerRight = 5,
        SlidedDown = 6,
        SlidedLowerLeft = 7,
        SlidedLeft = 8,
        SlidedUpperLeft = 9
    }
    // Start is called before the first frame update
    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        SectorSize = PlayerPrefs.GetFloat("size", 200.0f);
        SectorDistance = PlayerPrefs.GetFloat("distance", 400.0f);
        Texture2D tentativeTexture = new(1, 1);
        tentativeTexture.SetPixel(0, 0, Color.white);
        tentativeTexture.Apply();
        texture = tentativeTexture;
        style = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = Color.green;
        messageStyle = new GUIStyle();
        messageStyle.normal.textColor = Color.green;
        messageStyle.fontSize = Screen.height / 10;
        messageRect = new Rect(25, 25, 10, 10);

        AcquireMulticastLock();
        discoveryClient = new UdpClient();
        discoveryClient.EnableBroadcast = true;
        InvokeRepeating(nameof(SendDiscoveryPing), 0.5f, 1f);
        discoveryThread = new Thread(ListenDiscovery);
        discoveryThread.IsBackground = true;
        discoveryThread.Start();
        inputClient = new UdpClient();
        settingClient = new UdpClient(settingPort);
        settingClient.Client.ReceiveTimeout = 1000;
        listenThread = new Thread(ListenFromPC);
        listenThread.IsBackground = true;
        listenThread.Start();
        lastPhoneHeartBeatSent = Time.time;
    }
    // Update is called once per frame
    void Update()
    {
        // Discovery / HeartBeatフラグ処理
        if (discoveryReceived || recievedHeartBeat)
        {
            recievedHeartBeat = false;
            discoveryReceived = false;
            isConnected = true;
            lastHeartBeatRecieved = Time.time;
        }

        // タイムアウトチェック
        if (isConnected && Time.time - lastHeartBeatRecieved > connectionTimeout)
        {
            isConnected = false;
            // Debug.Log("Connection timeout");   // スパム防止でコメントアウト推奨
        }
        SectorState sectorX = SectorState.None;
        SectorState sectorY = SectorState.None;
        SectorState sectorZ = SectorState.None;
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (SectorX.Contains(touch.position))
            {
                sectorX = UpdateSector(sectorX, GetSectorState(touch));
            }
            else if (SectorY.Contains(touch.position))
            {
                sectorY = UpdateSector(sectorY, GetSectorState(touch));
            }
            else if (SectorZ.Contains(touch.position))
            {
                sectorZ = UpdateSector(sectorZ, GetSectorState(touch));
            }
        }
        if (!string.IsNullOrEmpty(serverIP))
        {
            if (Time.time - lastPhoneHeartBeatSent >= heartBeatInterval)
            {
                SendPhoneHeartBeat();
                lastPhoneHeartBeatSent = Time.time;
            }

            if ((sectorX != sentX || sectorY != sentY || sectorZ != sentZ) &&
                    Time.time - lastSendTime >= minInterval)
            {
                try
                {
                    lastSendTime = Time.time;

                    var endpoint = new IPEndPoint(IPAddress.Parse(serverIP), inputPort);
                    byte[] data = { (byte)sectorX, (byte)sectorY, (byte)sectorZ };
                    inputClient.Send(data, data.Length, endpoint);
                    sentX = sectorX;
                    sentY = sectorY;
                    sentZ = sectorZ;
                }
                catch { }
            }
        }
        message = "Chemicalizer Controller\n" + (isConnected ? "Connected" : "Connecting...");
    }
    private SectorState GetSectorState(Touch touch)
    {
        if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Stationary)
        {
            return SectorState.Tapped;
        }
        else if (touch.phase == TouchPhase.Moved)
        {
            //動いた角度によってスライド系を返す
            float angle = Vector2.SignedAngle(Vector2.right, touch.deltaPosition);
            if (angle >= 112.5f && angle < 157.5f)
            {
                return SectorState.SlidedUpperLeft;
            }
            else if (angle >= 67.5f && angle < 112.5f)
            {
                return SectorState.SlidedUp;
            }
            else if (angle >= 22.5f && angle < 67.5f)
            {
                return SectorState.SlidedUpperRight;
            }
            else if (angle >= -22.5f && angle < 22.5f)
            {
                return SectorState.SlidedRight;
            }
            else if (angle >= -67.5f && angle < -22.5f)
            {
                return SectorState.SlidedLowerRight;
            }
            else if (angle >= -112.5f && angle < -67.5f)
            {
                return SectorState.SlidedDown;
            }
            else if (angle >= -157.5f && angle < -112.5f)
            {
                return SectorState.SlidedLowerLeft;
            }
            else if (touch.deltaPosition != Vector2.zero)
            {
                return SectorState.SlidedLeft;
            }
            else
            {
                return SectorState.Tapped;
            }
        }
        return SectorState.None;
    }
    private SectorState UpdateSector(SectorState current, SectorState next)
    {
        // 現在がNoneなら次の状態をそのまま返す
        if (current == SectorState.None)
        {
            return next;
        }
        // 現在がTappedなら次の状態がスライド系ならそのまま返す。そうでないならTappedを維持する
        if (current == SectorState.Tapped)
        {
            if (next == SectorState.SlidedUp)
            {
                return next;
            }
            if (next == SectorState.SlidedUpperRight)
            {
                return next;
            }
            if (next == SectorState.SlidedRight)
            {
                return next;
            }
            if (next == SectorState.SlidedLowerRight)
            {
                return next;
            }
            if (next == SectorState.SlidedDown)
            {
                return next;
            }
            if (next == SectorState.SlidedLowerLeft)
            {
                return next;
            }
            if (next == SectorState.SlidedLeft)
            {
                return next;
            }
            if (next == SectorState.SlidedUpperLeft)
            {
                return next;
            }
            return SectorState.Tapped;
        }
        // 現在がスライド系なら書き換えない
        return current;
    }
    private void OnGUI()
    {
        GUI.Label(messageRect, message, messageStyle);
        float centerX = Screen.width / 2.0f;
        float centerY = Screen.height / 2.0f;
        SectorX = new Rect(centerX - SectorDistance - SectorSize * 1.5f, centerY - SectorSize / 2.0f, SectorSize, SectorSize);
        SectorY = new Rect(centerX - SectorSize * 0.5f, centerY - SectorSize / 2.0f, SectorSize, SectorSize);
        SectorZ = new Rect(centerX + SectorDistance + SectorSize * 0.5f, centerY - SectorSize / 2.0f, SectorSize, SectorSize);
        GUI.DrawTexture(SectorX, texture, ScaleMode.StretchToFill, true, 0.0f, Color.green, 2.0f, 0.0f);
        GUI.DrawTexture(SectorY, texture, ScaleMode.StretchToFill, true, 0.0f, Color.green, 2.0f, 0.0f);
        GUI.DrawTexture(SectorZ, texture, ScaleMode.StretchToFill, true, 0.0f, Color.green, 2.0f, 0.0f);
        style.fontSize = (int)(SectorSize - 5.0f);
        GUI.Label(SectorX, "X", style);
        GUI.Label(SectorY, "Y", style);
        GUI.Label(SectorZ, "Z", style);
    }

    void SendDiscoveryPing()
    {
        if (isConnected) return;
        try
        {
            byte[] data = { discoverySendByte };
            discoveryClient.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, discoveryPort));
        }
        catch { }
    }
    private void SendPhoneHeartBeat()
    {
        if (string.IsNullOrEmpty(serverIP)) return;

        try
        {
            byte[] data = { heartBeatByte };
            var endpoint = new IPEndPoint(IPAddress.Parse(serverIP), inputPort);
            inputClient.Send(data, data.Length, endpoint);
        }
        catch { }
    }
    void ListenDiscovery()
    {
        UdpClient listener = null;
        try
        {
            listener = new UdpClient(discoveryPort);
            listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            while (isRunning)
            {
                try
                {
                    IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = listener.Receive(ref remote);

                    if (data != null && data.Length == 1 && data[0] == discoveryByte)
                    {
                        serverIP = remote.Address.ToString();
                        discoveryReceived = true;        // ここだけ
                        lastHeartBeatRecieved = Time.time; // 念のため
                    }
                }
                catch { }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("ListenDiscovery setup failed: " + e.Message);
        }
        finally
        {
            listener?.Close();
        }
    }
    private void ListenFromPC()
    {
        while (isRunning)
        {
            try
            {
                IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = settingClient.Receive(ref remote);

                if (data.Length == 1 && data[0] == heartBeatByte)
                {
                    recievedHeartBeat = true;
                    isConnected = true;
                    discoveryReceived = true;
                }
                else if (data.Length == 8)
                {
                    float size = BitConverter.ToSingle(data, 0);
                    SectorSize = size;
                    float distance = BitConverter.ToSingle(data, 4);
                    SectorDistance = distance;
                }
            }
            catch (Exception)
            {
                Thread.Sleep(1);
            }
        }
    }
    private void AcquireMulticastLock()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject wifiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi"))
            {
                multicastLock = wifiManager.Call<AndroidJavaObject>("createMulticastLock", "ChemicalizerMulticastLock");
                multicastLock.Call("setReferenceCounted", true);
                multicastLock.Call("acquire");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to acquire MulticastLock: " + e.Message);
        }
#endif
    }
    void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            PlayerPrefs.SetFloat("size", SectorSize);
            PlayerPrefs.SetFloat("distance", SectorDistance);
            PlayerPrefs.Save();
        }
    }
    void OnApplicationQuit()
    {
        ReleaseMulticastLock();
        isRunning = false;
        discoveryClient?.Close();
        inputClient?.Close();
        settingClient?.Close();
    }
    private void ReleaseMulticastLock()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (multicastLock != null)
            {
                multicastLock.Call("release");
                multicastLock.Dispose();
                multicastLock = null;
                Debug.Log("MulticastLock released");
            }
        }
        catch { }
#endif
    }
}
