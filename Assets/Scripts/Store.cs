using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Store : MonoBehaviour
{
    private List<string> inventory;

    // Start is called before the first frame update
    void Start()
    {
        inventory = new List<string>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Add(string itemName) {
        Debug.Log($"A delicious {itemName}!");
        inventory.Add(itemName);
    }

    public void Remove(string itemName) {
        Debug.Log($"Begone, {itemName}!");
        inventory.Remove(itemName);
    }

    public string Pop() {
        string item = inventory[0];
        inventory.RemoveAt(0);
        return item;
    }
}
