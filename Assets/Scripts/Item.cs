using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    private Dictionary<string, float> itemMaintences;
    private float days_per_step = 0.2F;
    private float remaining_time;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    void decrementProcessTime()
    {
        remaining_time -= days_per_step;
    }

    public bool isProcessed()
    {
        return remaining_time <= 0;
    }

    public float GetRemainingTime()
    {
        return remaining_time;
    }
}
