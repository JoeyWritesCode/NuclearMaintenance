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

public class WorldAction
{
    private string identifier;
    private List<string> parameters;
    private string state;

    private int i = 0;

    public WorldAction(string _identifier, List<string> _parameters, string _state) {
        identifier = _identifier;
        parameters = _parameters;
        state = _state;
    }

    public void SetState(string newState) {
        state = newState;
    }

    public string GetState() {
        return state;
    }

    public List<string> GetParameters() {
        return parameters;
    }

    public string GetIdentifier() {
        return identifier;
    }
}

public class Percept 
{
    public string identifier;
    public List<string> value;

    public Percept(string _identifier, List<string> _value) {
        identifier = _identifier;
        value = _value;
    }
}


public class Program : MonoBehaviour
{
    public GameObject agentPrefab;
    public int numberOfAgents;

    public int step;

    private EnvironmentMas env;

    async void Start() {
        env = new EnvironmentMas(noTurns: 0, delayAfterTurn: 5, randomOrder: false, parallel: false);

        for (int i = 0; i < numberOfAgents; i++) {
            
            GameObject agentObject = Instantiate(agentPrefab, GetRandomPoint(GameObject.Find("Floor").transform.position, 10f), Quaternion.identity);
            agentObject.name = "Worker_" + i;

            SimpleAgent agent = new SimpleAgent();
            agent.worker = agentObject.GetComponent<SimpleWorker>();

            FacilityAgent disassemblyAgent = new FacilityAgent();

            /* UnityAgent unityAgent = new UnityAgent();
            unityAgent.worker = agentObject.GetComponent<SimpleWorker>();
            unityAgent.name = "unity_" + i;
            
            BDIAgent bdiAgent = new BDIAgent();
            
            bdiAgent._abm = unityAgent;
            
            env.Add(bdiAgent, "bdi_" + i);
            env.Add(unityAgent, "unity_" + i); */
            env.Add(agent, "Agent_" + i);
            env.Add(disassemblyAgent, "Disassembly");
        }
        
        env.Memory.Add("Size", 15);

        step = 0;
    }

    async void Update()
    {
        // Update every 
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
