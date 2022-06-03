using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private System.Random rng = new System.Random();

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
        var indices = Enumerable.Range(0, gemObjects.Length).ToList();
        indices.Shuffle(rng);
        for (int i = 0; i < nItems; i++)
        {
            SpawnItem(gemObjects[indices[i]]);
        }
    }

    public GameObject SpawnItem(GameObject item)
    {
        Vector3 spawnPosition = new Vector3();
        string itemName = char.ToLowerInvariant(item.name[0]) + item.name.Substring(1);

        // Randomly generate a spawn position that doesn't collide with other objects
        int nCollisions = 1;
        while (nCollisions > 0)
        {
            spawnPosition.x = Random.Range(xMinRange, xMaxRange);
            spawnPosition.y = item.transform.GetComponent<BoxCollider>().size.y;
            spawnPosition.z = Random.Range(zMinRange, zMaxRange);

            Collider[] hitColliders = Physics.OverlapBox(spawnPosition + new Vector3(0.0f, 0.55f, 0.0f),
                                                         new Vector3(0.5f, 0.5f, 0.5f));
            nCollisions = hitColliders.Length;
        }

        // Spawn the game object
        GameObject spawnedItem = Instantiate(item, spawnPosition, item.transform.rotation);
        spawnedItem.name = item.name;
        spawnedItem.transform.localScale = spawnedItem.transform.localScale * 2f;
        spawnedItem.GetComponent<WorldDataReporter>().reportingID = itemName + numItemsSpawned.ToString("D4");
        spawnedItem.AddComponent<PickupItem>();

        // Make the parent the spawner so hierarchy doesn't get super messy
        spawnedItem.transform.parent = gameObject.transform;

        // Misc
        numItemsSpawned++;
        im.scriptedInput.ReportScriptedEvent(itemName + "Location", new Dictionary<string, object> {
                {"reportingId", spawnedItem.GetComponent<WorldDataReporter>().reportingID},
                { "positionX", spawnPosition.x },
                { "positionZ", spawnPosition.z }
            });

        return spawnedItem;
    }

    public void HideItem(GameObject item)
    {
        item.GetComponentInChildren<Renderer>().enabled = false;
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
        item.GetComponentInChildren<Renderer>().enabled = true;
    }

    public void UnhideItems()
    {
        items = GameObject.FindGameObjectsWithTag("Pickups");
        foreach (GameObject item in items)
        {
            UnhideItem(item);
        }
    }

    public bool isItemHidden(GameObject item) {
        return !item.GetComponentInChildren<Renderer>().enabled;
    }

    public void DestroyItems()
    {
        items = GameObject.FindGameObjectsWithTag("Pickups");
        foreach (GameObject item in items)
        {
            Destroy(item);
        }
    }

    public GameObject[] GetItems()
    {
        //items = GameObject.FindGameObjectsWithTag("Pickups");
        return GameObject.FindGameObjectsWithTag("Pickups");
    }

    public GameObject[] GetVisibleItems()
    {
        //items = GameObject.FindGameObjectsWithTag("Pickups");
        return GameObject.FindGameObjectsWithTag("Pickups")
            .Where(x => x.GetComponentInChildren<Renderer>().enabled)
            .ToArray();
    }
}
