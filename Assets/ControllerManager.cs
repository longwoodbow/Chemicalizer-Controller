using UnityEngine;
public class ControllerManager : MonoBehaviour
{
    private Rect SectorX;
    private Rect SectorY;
    private Rect SectorZ;
    public float SectorSize;
    public float SectorDistance;
    private Texture texture;
    private GUIStyle style;
    private string message = "Chemicalizer Controller";
    private GUIStyle messageStyle;
    private Rect messageRect;
    public enum SectorState
    {
        None,
        Tapped,
        SlidedUp,
        SlidedUpperRight,
        SlidedRight,
        SlidedLowerRight,
        SlidedDown,
        SlidedLowerLeft,
        SlidedLeft,
        SlidedUpperLeft
    }
    // Start is called before the first frame update
    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.orientation = PlayerPrefs.GetInt("orientation", 0) == 0 ? ScreenOrientation.LandscapeLeft : ScreenOrientation.LandscapeRight;
        SectorSize = PlayerPrefs.GetFloat("size", 100.0f);
        SectorDistance = PlayerPrefs.GetFloat("distance", 500.0f);
        Texture2D tentativeTexture = new Texture2D(1, 1);
        tentativeTexture.SetPixel(0, 0, Color.white);
        tentativeTexture.Apply();
        texture = tentativeTexture;
        style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.green;
        messageStyle = new GUIStyle();
        messageStyle.normal.textColor = Color.green;
        messageStyle.fontSize = Screen.height / 10;
        messageRect = new Rect(10, 10, 10, 10);
        Debug.Log("=== ControllerManager Start ===");
        InitializeUsbManager();
        CheckAccessoryIntent();
    }
    // Update is called once per frame
    void Update()
    {
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
        message = "Chemicalizer Controller\n" + (isConnected ? "Connected" : "Disconnected");
        SendInput(SectorStateToByte(sectorX), SectorStateToByte(sectorY), SectorStateToByte(sectorZ));
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
        SectorX = new Rect(centerX - SectorDistance - SectorSize / 2.0f, centerY - SectorSize / 2.0f, SectorSize, SectorSize);
        SectorY = new Rect(centerX - SectorSize / 2.0f, centerY - SectorSize / 2.0f, SectorSize, SectorSize);
        SectorZ = new Rect(centerX + SectorDistance - SectorSize / 2.0f, centerY - SectorSize / 2.0f, SectorSize, SectorSize);
        GUI.DrawTexture(SectorX, texture, ScaleMode.StretchToFill, true, 0.0f, Color.white, 0.0f, 0.0f);
        GUI.DrawTexture(SectorY, texture, ScaleMode.StretchToFill, true, 0.0f, Color.white, 0.0f, 0.0f);
        GUI.DrawTexture(SectorZ, texture, ScaleMode.StretchToFill, true, 0.0f, Color.white, 0.0f, 0.0f);
        style.fontSize = (int)(SectorSize - 5.0f);
        GUI.Label(SectorX, "X", style);
        GUI.Label(SectorY, "Y", style);
        GUI.Label(SectorZ, "Z", style);
    }
    private byte SectorStateToByte(SectorState state)
    {
        return state switch
        {
            SectorState.None => 0,
            SectorState.Tapped => 1,
            SectorState.SlidedUp => 2,
            SectorState.SlidedUpperRight => 3,
            SectorState.SlidedRight => 4,
            SectorState.SlidedLowerRight => 5,
            SectorState.SlidedDown => 6,
            SectorState.SlidedLowerLeft => 7,
            SectorState.SlidedLeft => 8,
            SectorState.SlidedUpperLeft => 9,
            _ => 0,
        };
    }
    public struct InputData
    {
        public byte InputX;
        public byte InputY;
        public byte InputZ;
    }
    private AndroidJavaObject usbManager;
    private AndroidJavaObject accessory;
    private AndroidJavaObject fileDescriptor;
    private AndroidJavaObject outputStream;
    private bool isConnected = false;
    private InputData lastSentData;
    void OnApplicationFocus(bool hasFocus)
    {
        Debug.Log($"OnApplicationFocus: {hasFocus}");
        if (hasFocus)
            CheckAccessoryIntent();
    }
    // 新しいIntentを受け取ったときに呼ばれる
    void OnNewIntent(AndroidJavaObject intent)
    {
        Debug.Log("=== OnNewIntent が呼ばれました ===");
        if (intent != null)
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                activity.Call("setIntent", intent);
            }
            CheckAccessoryIntent();
        }
    }
    private void InitializeUsbManager()
    {
        Debug.Log("InitializeUsbManager を呼び出しています");
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        {
            usbManager = activity.Call<AndroidJavaObject>("getSystemService", "usb");
            Debug.Log($"usbManager取得結果: {(usbManager != null ? "成功" : "失敗")}");
        }
    }
    private void CheckAccessoryIntent()
    {
        Debug.Log("<color=yellow>=== CheckAccessoryIntent 実行 ===</color>");
        if (usbManager == null)
        {
            Debug.LogWarning("usbManager が null → 再初期化");
            InitializeUsbManager();
            if (usbManager == null) return;
        }
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var intent = activity.Call<AndroidJavaObject>("getIntent"))
        {
            string action = intent.Call<string>("getAction");
            Debug.Log($"Intent Action: {action}");
            // 1. Intentから直接
            using (var usbManagerClass = new AndroidJavaClass("android.hardware.usb.UsbManager"))
            {
                string extraAccessory = usbManagerClass.GetStatic<string>("EXTRA_ACCESSORY");
                AndroidJavaObject accessoryObj = intent.Call<AndroidJavaObject>("getParcelableExtra", extraAccessory);
                if (accessoryObj != null)
                {
                    Debug.Log("<color=green>IntentからAccessory取得成功！</color>");
                    accessory = accessoryObj;
                    OpenAccessory();
                    return;
                }
            }
            // 2. usbManagerから直接一覧取得（重要）
            Debug.Log("AccessoryList を確認中...");
            AndroidJavaObject accessoryList = usbManager.Call<AndroidJavaObject>("getAccessoryList");
            if (accessoryList != null)
            {
                int length = accessoryList.Call<int>("getLength");
                Debug.Log($"<color=cyan>Accessory List Length: {length}</color>");
                if (length > 0)
                {
                    accessory = accessoryList.Call<AndroidJavaObject>("get", 0);
                    if (accessory != null)
                    {
                        Debug.Log("<color=green>AccessoryListから取得成功！ OpenAccessoryを実行</color>");
                        OpenAccessory();
                        return;
                    }
                }
            }
            Debug.LogWarning("Accessoryが見つかりませんでした。PC側がAccessoryモードで待機しているか確認してください。");
        }
    }
    private void OpenAccessory()
    {
        Debug.Log("OpenAccessory を開始");
        if (usbManager == null || accessory == null)
        {
            Debug.LogError("OpenAccessory: usbManager または accessory が null");
            return;
        }
        try
        {
            Debug.Log("openAccessory を呼び出しています...");
            fileDescriptor = usbManager.Call<AndroidJavaObject>("openAccessory", accessory);
            if (fileDescriptor != null)
            {
                Debug.Log("<color=green>openAccessory 成功！</color>");
                var javaFileDescriptor = fileDescriptor.Call<AndroidJavaObject>("getFileDescriptor");
                outputStream = new AndroidJavaObject("java.io.FileOutputStream", javaFileDescriptor);
                isConnected = true;
                Debug.Log("<color=green>=== USB Accessory接続 完了！ ===</color>");
                SendInput(0, 0, 0);
            }
            else
            {
                Debug.LogError("openAccessory が null を返しました");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"OpenAccessory エラー: {e.Message}\nStackTrace: {e.StackTrace}");
        }
    }
    // SendInput, SendData はそのまま（変更なし）
    public void SendInput(byte x, byte y, byte z)
    {
        if (!isConnected || outputStream == null) return;
        byte[] buffer = new byte[] { x, y, z };
        try
        {
            outputStream.Call("write", buffer);
            outputStream.Call("flush");
        }
        catch (System.Exception e)
        {
            Debug.LogError("送信エラー: " + e.Message);
            isConnected = false;
        }
    }
    void OnApplicationQuit()
    {
        outputStream?.Call("close");
        fileDescriptor?.Call("close");
    }
}
