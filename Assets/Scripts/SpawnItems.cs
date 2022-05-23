using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnItems : MonoBehaviour
{
    public float xMinRange = 1.5f;
    public float xMaxRange = 31.5f;
    public float zMinRange = 1.5f;
    public float zMaxRange = 31.5f;
    public GameObject goldObject; // prefab to spawn
    public GameObject[] gemObjects; // prefabs to spawn
    protected InterfaceManager im;
    private GameObject[] items;
    private int numItemsSpawned = 0;

    void Awake()
    {
        GameObject mgr = GameObject.Find("InterfaceManager");
        if (mgr)
        {
            im = (InterfaceManager)mgr.GetComponent("InterfaceManager");
        }
    }

    public void SpawnGold(int nItems)
    {
        im.scriptedInput.ReportScriptedEvent("goldSpawned", new Dictionary<string, object> { { "nItems", nItems } });
        for (int i = 0; i < nItems; i++)
        {
            SpawnItem(goldObject);
        }
    }

    public void SpawnGems(int nItems)
    {
        if (nItems > gemObjects.Length)
        {
            im.Do(new EventBase<string, int>(im.ShowWarning, "The game is trying to spawn repeat gems. Please quit the game.", 5000));
        }

        im.scriptedInput.ReportScriptedEvent("gemsSpawned", new Dictionary<string, object> { { "nItems", nItems } });
        for (int i = 0; i < nItems; i++)
        {
            SpawnItem(gemObjects[i % gemObjects.Length]);
        }
    }

    public void SpawnItem(GameObject item)
    {
        Vector3 spawnPosition = new Vector3();
        string itemName = char.ToLowerInvariant(item.name[0]) + item.name.Substring(1);

        // Randomly generate a spawn position that doesn't collide with other objects
        int nCollisions = 1;
        while (nCollisions > 0)
        {
            spawnPosition.x = Random.Range(xMinRange, xMaxRange);
            spawnPosition.y = 0.0f;
            spawnPosition.z = Random.Range(zMinRange, zMaxRange);

            Collider[] hitColliders = Physics.OverlapBox(spawnPosition + new Vector3(0.0f, 0.55f, 0.0f),
                                                         new Vector3(0.5f, 0.5f, 0.5f));
            nCollisions = hitColliders.Length;
        }

        // Spawn the game object
        GameObject spawnedItem = Instantiate(item, spawnPosition, gameObject.transform.rotation) as GameObject;
        numItemsSpawned++;
        spawnedItem.name = itemName;
        spawnedItem.GetComponent<WorldDataReporter>().reportingID = itemName + numItemsSpawned.ToString("D4");
        im.scriptedInput.ReportScriptedEvent(itemName + "Location", new Dictionary<string, object> {
                {"reportingId", spawnedItem.GetComponent<WorldDataReporter>().reportingID},
                { "positionX", spawnPosition.x },
                { "positionZ", spawnPosition.z }
            });

        // Make the parent the spawner so hierarchy doesn't get super messy
        spawnedItem.transform.parent = gameObject.transform;
    }

    public void HideItem(GameObject item)
    {
        item.GetComponent<Renderer>().enabled = false;
    }

    public void HideItems()
    {
        items = GameObject.FindGameObjectsWithTag("Pickups");
        foreach (GameObject item in items)
        {
            HideItem(item);
        }
    }

    public void UnhideItem(GameObject item)
    {
        item.GetComponent<Renderer>().enabled = true;
    }

    public void UnhideItems()
    {
        items = GameObject.FindGameObjectsWithTag("Pickups");
        foreach (GameObject item in items)
        {
            UnhideItem(item);
        }
    }

    public void DestroyItems()
    {
        items = GameObject.FindGameObjectsWithTag("Pickups");
        foreach (GameObject item in items)
        {
            Destroy(item);
        }
    }
}
