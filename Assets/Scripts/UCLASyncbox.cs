using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;
using System.Threading;

public class UCLASyncbox : NonUnitySyncbox 
{
    [DllImport ("UCLASync")]
    private static extern int OpenSyncbox();

    [DllImport ("UCLASync")]
    private static extern void CloseSyncbox();

    [DllImport ("UCLASync")]
    private static extern void SendPulse();

    [DllImport("UCLASync")]
    private static extern int GetStatus();

    public UCLASyncbox(InterfaceManager _im) : base(_im) {
        
    }

    public override void Init() {
        int err = OpenSyncbox();
        Debug.Log(err);

        StopPulse();
        Start();
    }

    protected override void Pulse()
    {
		if(!stopped)
        {
            // Send a pulse
            Debug.Log("Pew!");
            im.scriptedInput.ReportOutOfThreadScriptedEvent("syncPulse", new System.Collections.Generic.Dictionary<string, object>());
            SendPulse(); // try calling SendPulse(1) if SendPulse(0) doesn't work (and vice versa)

            // Wait a random interval between min and max
            int timeBetweenPulses = (int)(TIME_BETWEEN_PULSES_MIN + (int)(InterfaceManager.rnd.NextDouble() * (TIME_BETWEEN_PULSES_MAX - TIME_BETWEEN_PULSES_MIN)));
            DoIn(new EventBase(Pulse), timeBetweenPulses);
		}
	}

    public override void Close() {
        CloseSyncbox();
    }
}
