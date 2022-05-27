using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour
{
    protected float xMin;
    protected float xMax;
    protected List<GameObject> itemsOnTimeline = new List<GameObject>();

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

    public float GetItemTimeNormalized(Transform item)
    {
        if (item.position.y == transform.position.y)
        {
            float width = xMax - xMin;
            return (item.position.x - xMin) / width;
        }
        else
        {
            return -1;
        }
    }

    // By default, this returns a normalized value (between 0 and 1)
    public List<(GameObject, float)> GetItemTimes(float scale = 1)
    {
        List<(GameObject, float)> items = new List<(GameObject, float)>();
        foreach (var item in itemsOnTimeline)
        {
            float itemTime = GetItemTimeNormalized(item.transform) * scale;
            items.Add((item, itemTime));
        }

        return items;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<DragDrop>() != null)
        {
            itemsOnTimeline.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.GetComponent<DragDrop>() != null)
        {
            itemsOnTimeline.Remove(other.gameObject);
        }
    }
}
