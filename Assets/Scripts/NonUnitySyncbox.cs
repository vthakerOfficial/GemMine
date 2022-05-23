using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;
using System.Threading;

public class NonUnitySyncbox : EventLoop
{
    public InterfaceManager im;

    protected const int PULSE_START_DELAY = 1000; // ms
    protected const int TIME_BETWEEN_PULSES_MIN = 800;
    protected const int TIME_BETWEEN_PULSES_MAX = 1200;

    protected volatile bool stopped = true;

    public NonUnitySyncbox(InterfaceManager _im)
    {
        im = _im;
    }

    public virtual void Init() {}

    public bool IsRunning()
    {
        return !stopped;
    }

    public void StartPulse()
    {
        if (!IsRunning())
        {
            stopped = false;
            DoIn(new EventBase(Pulse), PULSE_START_DELAY);
        }
    }

    protected virtual void Pulse() {}

    public void StopPulse()
    {
        stopped = true;
    }

    public virtual void Close() {}
}
