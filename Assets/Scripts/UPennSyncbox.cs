using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;
using System.Threading;

public class UPennSyncbox : NonUnitySyncbox 
{

    //Function from Corey's Syncbox plugin (called "ASimplePlugin")
	[DllImport ("ASimplePlugin")]
	private static extern IntPtr OpenUSB();
	[DllImport ("ASimplePlugin")]
	private static extern IntPtr CloseUSB();
	[DllImport ("ASimplePlugin")]
	private static extern IntPtr TurnLEDOn();
	[DllImport ("ASimplePlugin")]
	private static extern IntPtr TurnLEDOff();
	[DllImport ("ASimplePlugin")]
	private static extern float SyncPulse();

    public UPennSyncbox(InterfaceManager _im) : base(_im) {
        
    }

    public override void Init() {
        Debug.Log(Marshal.PtrToStringAuto(OpenUSB()));

        StopPulse();
        Start();
    }

	protected override void Pulse ()
    {
		if(!stopped)
        {
            // Send a pulse
            Debug.Log("Pew!");
            im.scriptedInput.ReportOutOfThreadScriptedEvent("syncPulse", new System.Collections.Generic.Dictionary<string, object>());
            SyncPulse();

            // Wait a random interval between min and max
            int timeBetweenPulses = (int)(TIME_BETWEEN_PULSES_MIN + (int)(InterfaceManager.rnd.NextDouble() * (TIME_BETWEEN_PULSES_MAX - TIME_BETWEEN_PULSES_MIN)));
            DoIn(new EventBase(Pulse), timeBetweenPulses);
		}
	}

    public override void Close() {
        CloseUSB();
    }
}
