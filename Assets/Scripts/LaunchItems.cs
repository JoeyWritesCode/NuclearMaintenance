using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaunchItems : MonoBehaviour
{

    public float spawnTime;
    public float launchSpeed = 10;
    float counter;

    public Rigidbody item;
    bool launched = false;

    void Update() 
    {
        if (launched == false) {
            launched = true;
            StartCoroutine(launch());
        }
    }

    IEnumerator launch()
    {
        Rigidbody itemClone = (Rigidbody) Instantiate(item, transform.position, transform.rotation);
        itemClone.velocity = transform.forward * launchSpeed;
        yield return new WaitForSeconds(spawnTime);
        launched = false;
    }
}
