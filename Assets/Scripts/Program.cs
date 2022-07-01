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
using System.IO;
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

public class SimulationAgent : Agent
{
    private List<string> events = new List<string>();
    private int step = 0;

    public override void Act(Message message)
    {
        try
        {
            Console.WriteLine($"\t{message.Format()}");
            message.Parse(out string eventForLog, out List<string> details);
            events.Add($"{step} {eventForLog} {String.Join((string) " - ", details)}");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        step++;
    }

    public override void ActDefault()
    {
        step++;
    }

    public List<string> GetEvents()
    {
        return events;
    }
}


public class Program : MonoBehaviour
{
    public GameObject agentPrefab;
    public int numberOfAgents;

    public int step;

    EnvironmentMas env;
    public int amountOfDays;
    private int steps_per_day = 20;

    SimulationAgent programAgent;

    async void Start() {
        env = new EnvironmentMas(noTurns: 0, delayAfterTurn: 5, randomOrder: false, parallel: false);

        programAgent = new SimulationAgent();
        env.Add(programAgent, "program");

        for (int i = 0; i < numberOfAgents; i++) {
            
            //GameObject agentObject = Instantiate(agentPrefab, GetRandomPoint(GameObject.Find("Floor").transform.position, 10f), Quaternion.identity);
            GameObject agentObject = Instantiate(agentPrefab, GameObject.Find("GoodsInOut").transform.position, Quaternion.identity);
            agentObject.name = "Agent_" + i;

            SimpleAgent agent = new SimpleAgent();
            agent.worker = agentObject.GetComponent<SimpleWorker>();
            agent.name = "Agent_" + i;

            /* UnityAgent unityAgent = new UnityAgent();
            unityAgent.worker = agentObject.GetComponent<SimpleWorker>();
            unityAgent.name = "unity_" + i;
            
            BDIAgent bdiAgent = new BDIAgent();
            
            bdiAgent._abm = unityAgent;
            
            env.Add(bdiAgent, "bdi_" + i);
            env.Add(unityAgent, "unity_" + i); */
            env.Add(agent, "Agent_" + i);
        }

        FacilityAgent inOutAgent = new FacilityAgent();
        inOutAgent.facility = GameObject.Find("GoodsInOut").GetComponent<Facility>();
        env.Add(inOutAgent, "GoodsInOut");

        FacilityAgent disassemblyAgent = new FacilityAgent();
        disassemblyAgent.facility = GameObject.Find("Disassembly").GetComponent<Facility>();
        env.Add(disassemblyAgent, "Disassembly");

        FacilityAgent storageA = new FacilityAgent();
        storageA.facility = GameObject.Find("StorageA").GetComponent<Facility>();
        env.Add(storageA, "StorageA");

        FacilityAgent storageB = new FacilityAgent();
        storageB.facility = GameObject.Find("StorageB").GetComponent<Facility>();
        env.Add(storageB, "StorageB");

        FacilityAgent recycleA = new FacilityAgent();
        recycleA.facility = GameObject.Find("RecycleA").GetComponent<Facility>();
        env.Add(recycleA, "RecycleA");

        FacilityAgent recycleB = new FacilityAgent();
        recycleB.facility = GameObject.Find("RecycleB").GetComponent<Facility>();
        env.Add(recycleB, "RecycleB");
        
        env.Memory.Add("Size", 15);

        step = 0;
    }

    async void Update()
    {
        if (amountOfDays == -1) {
            if (Input.GetKeyDown("space")) {
                env.RunTurn(step++);
            }
        }
        else {
            if (step == amountOfDays * steps_per_day) {
                File.WriteAllLines("output.txt", programAgent.GetEvents().ToArray());
                Time.timeScale = 0;
                Application.Quit();
            }
            else {
                env.RunTurn(step++);
            }
        }
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
