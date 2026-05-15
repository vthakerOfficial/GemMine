using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using UnityEngine.Networking;
#endif

[AddComponentMenu("UnityEPL/Handlers/Updated Write to Disk Handler")]
public class UpdatedWriteToDiskHandler : DataHandler
{
    //more output formats may be added in the future
    public enum FORMAT { JSON_LINES };
    public FORMAT outputFormat;

    [HideInInspector]
    [SerializeField]
    private bool writeAutomatically = true;
    [HideInInspector]
    [SerializeField]
    private int framesPerWrite = 30;

    private System.Collections.Generic.Queue<DataPoint> waitingPoints = new System.Collections.Generic.Queue<DataPoint>();
    private InterfaceManager manager;
#if UNITY_WEBGL && !UNITY_EDITOR
    private bool webWriteInProgress = false;
    private const string WebDataEndpoint = "/goldmine/events";
#endif


    public void SetWriteAutomatically(bool newAutomatically)
    {
        writeAutomatically = newAutomatically;
    }
    public bool WriteAutomatically()
    {
        return writeAutomatically;
    }
    public void SetFramesPerWrite(int newFrames)
    {
        if (newFrames > 0)
            framesPerWrite = newFrames;
    }
    public int GetFramesPerWrite()
    {
        return framesPerWrite;
    }

    void Awake() {
        GameObject mgr = GameObject.Find("InterfaceManager");
        manager = (InterfaceManager)mgr.GetComponent("InterfaceManager");
    }

    protected override void Update()
    {
        base.Update();

        if (Time.frameCount % framesPerWrite == 0)
            DoWrite();
    }

    protected override void HandleDataPoints(DataPoint[] dataPoints)
    {
        foreach (DataPoint dataPoint in dataPoints)
            waitingPoints.Enqueue(dataPoint);
    }

    /// <summary>
    /// Writes data from the waitingPoints queue to disk.  The waitingPoints queue will be automatically updated whenever reporters report data.
    /// 
    /// DoWrite() will also be automatically be called periodically according to the settings in the component inspector window, but you can invoke this manually if desired.
    /// </summary>
    public void DoWrite()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (webWriteInProgress || waitingPoints.Count == 0)
        {
            return;
        }

        StartCoroutine(PostWaitingPoints());
#else
        while (waitingPoints.Count > 0)
        {
            string directory = manager.fileManager.SessionPath();
            if(directory == null) {
                return;
            }
            System.IO.Directory.CreateDirectory(directory);
            string filePath = System.IO.Path.Combine(directory, "unnamed_file");

            DataPoint dataPoint = waitingPoints.Dequeue();
            string writeMe = "unrecognized type";
            string extensionlessFileName = "events";//DataReporter.GetStartTime ().ToString("yyyy-MM-dd HH mm ss");
            switch (outputFormat)
            {
                case FORMAT.JSON_LINES:
                    writeMe = dataPoint.ToJSON();
                    filePath = System.IO.Path.Combine(directory, extensionlessFileName + ".jsonl");
                    break;
            }
            System.IO.File.AppendAllText(filePath, writeMe + System.Environment.NewLine);
        }
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private IEnumerator PostWaitingPoints()
    {
        webWriteInProgress = true;

        List<DataPoint> batch = new List<DataPoint>();
        while (waitingPoints.Count > 0)
        {
            batch.Add(waitingPoints.Dequeue());
        }

        string payload = BuildJsonLinesPayload(batch);
        string url = BuildDataEndpointUrl();
        byte[] body = Encoding.UTF8.GetBytes(payload);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/x-ndjson; charset=utf-8");

            yield return request.SendWebRequest();

            bool failed = request.result == UnityWebRequest.Result.ConnectionError ||
                          request.result == UnityWebRequest.Result.ProtocolError ||
                          request.result == UnityWebRequest.Result.DataProcessingError;

            if (failed)
            {
                RequeueFailedBatch(batch);
                Debug.LogError("Goldmine data server is unavailable. Keeping " + batch.Count +
                               " events queued. Please run the local python WebGL data server. Request error details: " +
                               request.error + " Code: (" + request.responseCode + ")");
            }
        }

        webWriteInProgress = false;
    }

    private string BuildJsonLinesPayload(List<DataPoint> batch)
    {
        StringBuilder builder = new StringBuilder();
        foreach (DataPoint dataPoint in batch)
        {
            builder.Append(dataPoint.ToJSON());
            builder.Append('\n');
        }
        return builder.ToString();
    }

    private string BuildDataEndpointUrl()
    {
        string experiment = manager.GetSetting<string>("experimentName", "Goldmine");
        string participant = manager.GetSetting<string>("participantCode", "U001");
        string session = manager.GetSetting<string>("session", "0");
        string endpoint = ResolveEndpointUrl();

        return endpoint +
               "?experiment=" + UnityWebRequest.EscapeURL(experiment) +
               "&participant=" + UnityWebRequest.EscapeURL(participant) +
               "&session=" + UnityWebRequest.EscapeURL(session);
    }

    private string ResolveEndpointUrl()
    {
        try
        {
            if (!string.IsNullOrEmpty(Application.absoluteURL))
            {
                Uri pageUri = new Uri(Application.absoluteURL);
                return new Uri(pageUri, WebDataEndpoint).ToString();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Couldn't resolve Goldmine data endpoint from page URL: " + ex.Message);
        }

        return "http://localhost:8000" + WebDataEndpoint;
    }

    private void RequeueFailedBatch(List<DataPoint> batch)
    {
        Queue<DataPoint> requeued = new Queue<DataPoint>();
        foreach (DataPoint dataPoint in batch)
        {
            requeued.Enqueue(dataPoint);
        }
        while (waitingPoints.Count > 0)
        {
            requeued.Enqueue(waitingPoints.Dequeue());
        }
        waitingPoints = requeued;
    }
#endif
}
