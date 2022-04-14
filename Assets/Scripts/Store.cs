using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Store : MonoBehaviour
{
    public string itemStored;
    public List<GameObject> inventory = new List<GameObject>();

    void Start()
    {
        inventory = new List<GameObject>();
    }

    public void Add(GameObject _object) {
        inventory.Add(_object);
    }

    public GameObject Remove() {
        if (inventory.Count > 0) {
            GameObject _object = inventory[0];
            _object.SetActiveRecursively(true);
            inventory.RemoveAt(0);
            return _object;
        }
        else
            return null;
    }
}
