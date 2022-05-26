using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragDrop : MonoBehaviour//, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
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

    //public void OnBeginDrag(PointerEventData eventData)
    //{
    //    Debug.Log("OnBeginDrag");
    //    canvasGroup.blocksRaycasts = false;
    //    canvasGroup.alpha = 0.6f;
    //}

    //public void OnEndDrag(PointerEventData eventData)
    //{
    //    Debug.Log("OnEndDrag");
    //    //float ypos = slider.transform.position.y;
    //    //transform.position = new Vector3(transform.position.x, ypos, transform.position.z);
    //    canvasGroup.blocksRaycasts = true;
    //    canvasGroup.alpha = 1f;
    //}

    //public void OnPointerDown(PointerEventData eventData)
    //{
    //    Debug.Log("OnPointerDown");
    //}

    //public void OnDrag(PointerEventData eventData)
    //{
    //    Vector3 delta = new Vector3(eventData.delta.x, eventData.delta.y, 0);
    //    Vector3 screenPos = camera.WorldToScreenPoint(transform.position);
    //    Vector3 newScreenPos = screenPos + delta;
    //    Vector3 worldPos = camera.ScreenToWorldPoint(newScreenPos);
    //    transform.position = worldPos;
    //}

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
