using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PickupItem : MonoBehaviour
{
    public bool isPickedUp { get; private set; } = false;
    public float pickupTimeMs { get; private set; } = float.MinValue;

    private DateTime itemCreationTime = DateTime.MinValue;
    private Transform lookAtMeTransform = null;

    private void Update()
    {
        const float RPM = 10;

        if (transform.name != "gold")
        {
            float yRot = RPM * 6f * Time.deltaTime;
            transform.Rotate(0, yRot, 0, Space.World);
        }
        Debug.Log(transform.name + "  " + transform.name.Contains("picture"));
        if (transform.name.Contains("picture") && lookAtMeTransform != null)
        {
            Vector3 dir = lookAtMeTransform.position - transform.position;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }
    }

    public void InitPickup()
    {
        isPickedUp = false;
        itemCreationTime = DateTime.Now;
    }

    public void InitPickup(Transform LookAtMe)
    {
        InitPickup();
        lookAtMeTransform = LookAtMe;
    }

    public void Pickup()
    {
        isPickedUp = true;
        pickupTimeMs = (DateTime.Now - itemCreationTime).Ticks / TimeSpan.TicksPerMillisecond;
    }
}
