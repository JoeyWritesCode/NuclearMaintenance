using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Store : MonoBehaviour
{
    public Item itemStored;
    public List<GameObject> inventory = new List<GameObject>();

    void Start()
    {
        inventory = new List<GameObject>();
    }

    public void Add(Item _item) {
        if (itemStored.itemName == _item.itemName) {
            inventory.Add(_item.gameObject);
            _item.gameObject.SetActiveRecursively(false);
        }
        else {
            throw new KeyNotFoundException($"{_item.itemName}s are not stored here. The accept item is {itemStored.itemName}");
        }
    }

    // Might need looking into...
    public Item Remove() {
        if (inventory.Count > 0) {
            GameObject _object = inventory[0];
            _object.SetActiveRecursively(true);
            inventory.RemoveAt(0);
            return _object.GetComponent<Item>();
        }
        else
            return null;
    }
}
