using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Store : MonoBehaviour
{
    public string itemStored;
    public int occupancy;

    public void Add() {
        Debug.Log($"A delicious {itemStored}!");
        occupancy++;
    }

    public string Remove() {
        Debug.Log($"Begone, {itemStored}!");
        occupancy--;
        return itemStored;
    }
}
