using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupItem : MonoBehaviour
{
    void Update()
    {
        const float RPM = 10;

        if (transform.name != "gold")
        {
            float yRot = RPM * 6f * Time.deltaTime;
            transform.Rotate(0, yRot, 0, Space.World);
        }
    }
}
