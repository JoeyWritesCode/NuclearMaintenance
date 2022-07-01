using ActressMas;

using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AI;

public class TestingProgram : MonoBehaviour
{

    public GameObject agentPrefab;

    public int step;

    EnvironmentMas env;

    TestingAgent testingAgent;

    async void Start() {
        env = new EnvironmentMas(noTurns: 0, delayAfterTurn: 5, randomOrder: false, parallel: false);

        testingAgent = new TestingAgent();
        env.Add(testingAgent, "program");

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
        if (step == amountOfDays * steps_per_day) {
            File.WriteAllLines("output.txt", programAgent.GetEvents().ToArray());
            Time.timeScale = 0;
            Application.Quit();
        }
        else 
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