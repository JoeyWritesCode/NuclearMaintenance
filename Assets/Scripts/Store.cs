using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Store : MonoBehaviour
{
    public Item itemStored;
    public List<GameObject> inventory;

    public void Add(Item _item) {
        inventory.Add(_item.gameObject);
        Debug.Log(inventory.Count);
        _item.gameObject.SetActiveRecursively(false);        
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
