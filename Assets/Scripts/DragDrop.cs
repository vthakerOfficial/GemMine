using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragDrop : MonoBehaviour
{
    [SerializeField] private Camera camera;

    protected Vector3 originalPos;
    protected Vector3 lastMousePos;
    protected CanvasGroup canvasGroup;
    protected Collider collidedItem = null;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Start()
    {
        transform.GetComponent<BoxCollider>().enabled = true;
        transform.GetComponent<BoxCollider>().isTrigger = true;

        originalPos = transform.position;
    }

    private void OnMouseDown()
    {
        lastMousePos = camera.ScreenToWorldPoint(Input.mousePosition);
    }

    private void OnMouseDrag()
    {
        Vector3 currMousePos = camera.ScreenToWorldPoint(Input.mousePosition);
        Vector3 mouseDelta = currMousePos - lastMousePos;
        transform.position += new Vector3(mouseDelta.x, mouseDelta.y, 0f);
        lastMousePos = currMousePos;
    }

    private void OnMouseUp()
    {
        if (collidedItem != null)
        {
            collidedItem.GetComponent<ItemSlot>().SetItemPosition(transform);
            Debug.Log(collidedItem.GetComponent<ItemSlot>().GetItemTimeNormalized(transform));
        }
        else
        {
            transform.position = originalPos;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<ItemSlot>() != null)
        {
            collidedItem = other;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        collidedItem = null;
    }
}
