using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnItems : MonoBehaviour
{
    public float xMinRange = 1.5f;
    public float xMaxRange = 31.5f;
    public float zMinRange = 1.5f;
    public float zMaxRange = 31.5f;
    public GameObject spawnItem; // what prefab to spawn
    protected InterfaceManager im;
    private GameObject[] items;
    private int goldSpawnNumber = 0;

    void Awake()
    {
        GameObject mgr = GameObject.Find("InterfaceManager");
        if (mgr)
        {
            im = (InterfaceManager)mgr.GetComponent("InterfaceManager");
        }
    }

    public void Spawn(int nItems)
    {
        Vector3[] spawnPositions = new Vector3[nItems];
        im.scriptedInput.ReportScriptedEvent("goldSpawned", new Dictionary<string, object> { { "nItems", nItems } });

        for (int iPos = 0; iPos < nItems; iPos++)
        {
            // Randomly generate a spawn position that doesn't collide with other objects
            int nCollisions = 1;
            while (nCollisions > 0)
            {
                spawnPositions[iPos].x = Random.Range(xMinRange, xMaxRange);
                spawnPositions[iPos].y = 0.0f;
                spawnPositions[iPos].z = Random.Range(zMinRange, zMaxRange);

                Collider[] hitColliders = Physics.OverlapBox(spawnPositions[iPos] + new Vector3(0.0f, 0.55f, 0.0f), 
                                                             new Vector3(0.5f, 0.5f, 0.5f));
                nCollisions = hitColliders.Length;
            }

            // Spawn the game object
            GameObject spawnedItem = Instantiate(spawnItem, spawnPositions[iPos], gameObject.transform.rotation) as GameObject;
            goldSpawnNumber++;
            spawnedItem.name = "gold";
            spawnedItem.GetComponent<WorldDataReporter>().reportingID = "gold" + goldSpawnNumber.ToString("D4");
            im.scriptedInput.ReportScriptedEvent("goldLocation", new Dictionary<string, object> { 
                {"reportingId", spawnedItem.GetComponent<WorldDataReporter>().reportingID}, 
                { "positionX", spawnPositions[iPos].x }, 
                { "positionZ", spawnPositions[iPos].z } 
            });


            // Make the parent the spawner so hierarchy doesn't get super messy
            spawnedItem.transform.parent = gameObject.transform;
        }
    }

    public void HideItems()
    {
        Renderer rend;

        items = GameObject.FindGameObjectsWithTag("Pickups");
        foreach (GameObject item in items)
        {
            rend = item.GetComponent<Renderer>();
            rend.enabled = false;
        }
    }

    public void UnhideItems()
    {
        Renderer rend;

        items = GameObject.FindGameObjectsWithTag("Pickups");
        foreach (GameObject item in items)
        {
            rend = item.GetComponent<Renderer>();
            rend.enabled = true;
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
