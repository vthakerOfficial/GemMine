using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;

public abstract class DataHandler : MonoBehaviour
{
    protected List<DataReporter> reportersToHandle = new List<DataReporter>();
    protected ConcurrentQueue<DataReporter> toAdd = new ConcurrentQueue<DataReporter>();
    protected ConcurrentQueue<DataReporter> toRemove = new ConcurrentQueue<DataReporter>();

    protected virtual void Update()
    {
        DataReporter result;

        if (toRemove.Count > 0 && toRemove.TryDequeue(out result))
        {
            reportersToHandle.Remove(result);
        }

        if(toAdd.Count > 0 && toAdd.TryDequeue(out result)) {
            reportersToHandle.Add(result);
        }

        foreach (DataReporter reporter in reportersToHandle)
        {
            if (reporter.UnreadDataPointCount() > 0)
            {
                DataPoint[] newPoints = reporter.ReadDataPoints(reporter.UnreadDataPointCount());
                HandleDataPoints(newPoints);
            }
        }
    }

    public void AddReporter(DataReporter add) {
        toAdd.Enqueue(add);
    }

    public void RemoveReporter(DataReporter remove) {
        toRemove.Enqueue(remove);
    }

    protected abstract void HandleDataPoints(DataPoint[] dataPoints);
}