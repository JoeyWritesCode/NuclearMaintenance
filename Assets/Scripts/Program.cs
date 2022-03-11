/**************************************************************************
 *                                                                        *
 *  Description: Simple example of using the ActressMas framework         *
 *  Website:     https://github.com/florinleon/ActressMas                 *
 *  Copyright:   (c) 2018, Florin Leon                                    *
 *                                                                        *
 *  This program is free software; you can redistribute it and/or modify  *
 *  it under the terms of the GNU General Public License as published by  *
 *  the Free Software Foundation. This program is distributed in the      *
 *  hope that it will be useful, but WITHOUT ANY WARRANTY; without even   *
 *  the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR   *
 *  PURPOSE. See the GNU General Public License for more details.         *
 *                                                                        *
 **************************************************************************/

using ActressMas;

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AI;


public class Program : MonoBehaviour
{
    public GameObject agentPrefab;
    public int numberOfAgents;

    public int step;

    private EnvironmentMas env;

    void Start() {
        env = new EnvironmentMas(noTurns: 0, delayAfterTurn: 250, randomOrder: false, parallel: false);

        for (int i = 0; i < numberOfAgents; i++) {
            
            GameObject agentObject = Instantiate(agentPrefab, GetRandomPoint(GameObject.Find("Floor").transform.position, 10f), Quaternion.identity);
            agentObject.name = "unity_" + i;

            var bdiAgent = new BDIAgent(agentObject.name);
            env.Add(bdiAgent, "bdi_" + i);

            var unityAgent = new UnityAgent();
            env.Add(unityAgent, agentObject.name);
        }
        
        env.Memory.Add("Size", 15);

        step = 0;
    }

    void Update()
    {
        env.RunTurn(step++);
    }

    public Vector3 GetRandomPoint(Vector3 center, float maxDistance) {
        // Get Random Point inside Sphere which position is center, radius is maxDistance
        Vector3 randomPos = UnityEngine.Random.insideUnitSphere * maxDistance + center;

        NavMeshHit hit; // NavMesh Sampling Info Container

        // from randomPos find a nearest point on NavMesh surface in range of maxDistance
        NavMesh.SamplePosition(randomPos, out hit, maxDistance, NavMesh.AllAreas);

        return hit.position;
    }
}

public class MonitorAgent : Agent
{
    public override void Act(Message m)
    {
        Debug.Log($"{Name} has received {m.Format()}");
    }
}
