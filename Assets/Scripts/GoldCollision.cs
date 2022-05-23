using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoldCollision : MonoBehaviour
{
    public GameObject pgmObject; // GameObject that contains the practice game manager

    private PracticeGameManager pgm;

    // Start is called before the first frame update
    void Start()
    {
        pgm = pgmObject.GetComponent<PracticeGameManager>();
    }

    void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            //Destroy(gameObject);
            gameObject.SetActive(false);
            pgm.gold1Found = true;
            pgm.GoldFound();
        }
    }
}
