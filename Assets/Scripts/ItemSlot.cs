using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour//, IDropHandler
{
    protected float xMin;
    protected float xMax;

    void Start()
    {
        Vector3[] worldCorners = new Vector3[4];
        transform.GetComponent<RectTransform>().GetWorldCorners(worldCorners);
        xMin = worldCorners[1].x; // x-coord of top left corner
        xMax = worldCorners[2].x; // x-coord of top right corner
    }

    public void SetItemPosition(Transform item)
    {
        float zPos = item.position.z;
        float yPos = transform.position.y;
        float xPos = item.position.x;

        xPos = Mathf.Min(xMax, xPos);
        xPos = Mathf.Max(xMin, xPos);

        item.position = new Vector3(xPos, yPos, zPos);
    }

}
