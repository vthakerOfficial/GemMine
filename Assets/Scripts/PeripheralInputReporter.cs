using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using UnityEngine;

[AddComponentMenu("UnityEPL/Reporters/Input Reporter")]
public class PeripheralInputReporter : DataReporter {
#if UNITY_IPHONE
	[DllImport ("__Internal")]
#else
    [DllImport("UnityEPLNativePlugin")]
#endif
    public static extern double StartCocoaPlugin();

    [DllImport("UnityEPLNativePlugin")]
    public static extern void StopCocoaPlugin();

    [DllImport("UnityEPLNativePlugin")]
    public static extern int PopKeyKeycode();

    [DllImport("UnityEPLNativePlugin")]
    public static extern double PopKeyTimestamp();

    [DllImport("UnityEPLNativePlugin")]
    public static extern int CountKeyEvents();

    [DllImport("UnityEPLNativePlugin")]
    public static extern int PopMouseButton();

    [DllImport("UnityEPLNativePlugin")]
    public static extern double PopMouseTimestamp();

    [DllImport("UnityEPLNativePlugin")]
    public static extern int CountMouseEvents();

    public bool reportKeyStrokes = false;
    public bool reportMouseClicks = false;
    public bool reportMousePosition = false;
    public int framesPerMousePositionReport = 60;
    private Dictionary<int, bool> keyDownStates = new Dictionary<int, bool>();
    private Dictionary<int, bool> mouseDownStates = new Dictionary<int, bool>();

    private int lastMousePositionReportFrame;

    private InterfaceManager manager;

    void Awake() {
        GameObject mgr = GameObject.Find("InterfaceManager");
        manager = (InterfaceManager)mgr.GetComponent("InterfaceManager");     

        if(!nativePluginRunning && (bool) manager.GetSetting("nativePlugin")) {
            OSStartTime = StartCocoaPlugin();
            nativePluginRunning = true;
        }
    }

    void Update()
    {
        if (reportMouseClicks)
            CollectMouseEvents();
        if (reportKeyStrokes)
            CollectKeyEvents();
        if (reportMousePosition && Time.frameCount - lastMousePositionReportFrame > framesPerMousePositionReport)
            CollectMousePosition();
    }

    /// <summary>
    /// Collects the mouse events for MacOS.  For other platforms, mouse events are included in key events.
    /// </summary>

    private void CollectMouseEvents()
    {
        if (IsMacOS())
        {
            int eventCount = CountMouseEvents();
            if (eventCount >= 1)
            {
                int mouseButton = PopMouseButton();
                double timestamp = PopMouseTimestamp();
                bool downState;
                mouseDownStates.TryGetValue(mouseButton, out downState);
                mouseDownStates[mouseButton] = !downState;
                ReportMouse(mouseButton, mouseDownStates[mouseButton], OSXTimestampToTimestamp(timestamp));
            }
        }
    }

    private void ReportMouse(int mouseButton, bool pressed, System.DateTime timestamp)
    {
        Dictionary<string, object> dataDict = new Dictionary<string, object>();
        dataDict.Add("keyCode", mouseButton);
        dataDict.Add("isClicked", pressed);
        eventQueue.Enqueue(new DataPoint("mouseClick", timestamp, dataDict));
    }

    /// <summary>
    /// Collects the key events.  Except in MacOS, this includes mouse events, which are part of Unity's KeyCode enum.
    /// 
    /// In MacOS, UnityEPL uses a native plugin to achieve higher accuracy timestamping.
    /// </summary>

    private void CollectKeyEvents()
    {
        if (false) // (IsMacOS())
        {
            int eventCount = CountKeyEvents();
            if (eventCount >= 1)
            {
                int keyCode = PopKeyKeycode();
                double timestamp = PopKeyTimestamp();
                bool downState;
                keyDownStates.TryGetValue(keyCode, out downState);
                keyDownStates[keyCode] = !downState;
                ReportKey(keyCode, keyDownStates[keyCode], OSXTimestampToTimestamp(timestamp));
            }
        }
        else
        {
            foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(keyCode))
                {
                    ReportKey((int)keyCode, true, DataReporter.RealWorldTime());
                }
                if (Input.GetKeyUp(keyCode))
                {
                    ReportKey((int)keyCode, false, DataReporter.RealWorldTime());
                }
            }
        }
    }

    private void ReportKey(int keyCode, bool pressed, System.DateTime timestamp)
    {
        Dictionary<string, object> dataDict = new Dictionary<string, object>();
        string key;

        if (false) { //(IsMacOS()) {
            key = KeyLookup.get(keyCode, false);
        }
        else {
            key = ((KeyCode)keyCode).ToString();
        }
        dataDict.Add("keyCode", key);
        dataDict.Add("isPressed", pressed);
        string label = "keyPress";
        if (!IsMacOS())
            label = "keyPress";
        eventQueue.Enqueue(new DataPoint(label, timestamp, dataDict));

        manager.Do(new EventBase<string, bool>(manager.Key, key, pressed));
   }

    private void CollectMousePosition()
    {
        Dictionary<string, object> dataDict = new Dictionary<string, object>();
        dataDict.Add("mousePosition", Input.mousePosition);
        eventQueue.Enqueue(new DataPoint("mousePosition", DataReporter.RealWorldTime(), dataDict));
        lastMousePositionReportFrame = Time.frameCount;
    }

    public void OnDestroy() {
        if(nativePluginRunning && (bool)manager.GetSetting("nativePlugin")) {
            Debug.Log("stopping plugin");
            //StopCocoaPlugin();
            nativePluginRunning = false;
        }
    }
}