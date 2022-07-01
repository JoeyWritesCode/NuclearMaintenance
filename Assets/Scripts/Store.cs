using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Store : MonoBehaviour
{
    public Item itemStored;
    private List<GameObject> inventory= new List<GameObject>();
    private List<Item> itemInventory;

    void Start() {
        itemInventory = new List<Item>();
    }

    public void Add(Item _item) {
        inventory.Add(_item.gameObject);
        itemInventory.Add(_item);    
    }

    // Might need looking into...
    public Item Pop() {
        if (inventory.Count > 0) {
            GameObject _object = Instantiate(inventory[0]);
            inventory.RemoveAt(0);
            return _object.GetComponent<Item>();
        }
        else
            Debug.Log("This store is empty");
            return null;
    }

    public void Remove(Item item) {
        item.gameObject.SetActiveRecursively(true);
        inventory.Remove(item.gameObject);
    }

    // getting a particular item
    /* public Item GetItem(string name) {
        Debug.Log($"Let's grab this {name}. Occupancy {itemInventory.Count}");
        for (int i = 0; i < inventory.Count; i++) {
            Debug.Log(inventory[i].name);
            if (inventory[i].name == name) {
                Debug.Log("there she is!");
                return inventory[i].GetComponent<Item>();
            }
        }
        return null;
    } */
}
