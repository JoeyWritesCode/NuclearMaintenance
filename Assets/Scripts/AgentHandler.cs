using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentHandler : MonoBehaviour
{
    private UnityAgent _self;

    // Update is called once per frame
    public void SetAgent(UnityAgent agent)
    {
        _self = agent;
    }
}
