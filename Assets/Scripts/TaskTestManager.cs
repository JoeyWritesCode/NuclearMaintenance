using ActressMas;

using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AI;


public class TaskTestManager : MonoBehaviour
{

    public Item InputItem;
    private Item currentItem;

    /* ----------------------------------- a fancy Task class ----------------------------------- */
    [System.Serializable]
    public class Task
    {
        public GameObject thisTasksObject;
        public string thisTasksProcessType;

        public Task(GameObject _thisTasksObject, string _thisTasksProcessType) {
            thisTasksObject = _thisTasksObject;
            thisTasksProcessType = _thisTasksProcessType;
        }
    }

    public List<Task> TestCase;

    private int testIndex;

    void Start()
    {
        testIndex = 0;
        currentItem = Instantiate(InputItem).GetComponent<Item>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("space")) {
            if (TestCase.Count > 0) {                
                Debug.Log($"{currentItem.GetProcessObject()} vs {TestCase[testIndex].thisTasksObject}");
                Debug.Log($"{currentItem.GetProcessType()} vs {TestCase[testIndex].thisTasksProcessType}");
                testIndex++;
                //AssertTaskSpecification(TestCase, currentItem.GetTaskSpecification());
                AssertTaskSpecification(TestCase, TestCase);
            }
            else {
                Debug.Log(currentItem.GetProcessType());
            }
            currentItem.complete();
        }
    }

    public bool AssertTaskSpecification(List<Task> specOne, List<Task> specTwo) {
        string results = "";
        for(int i = 0; i < specOne.Count; i++) {
            if (specOne[i] == specTwo[i]) {
                results += "T";
            }
            else {
                results += "F";
                results += $": {specTwo[i]}";
                Debug.Log(results);
                return false;
            }
        }
        return true;
    }
}


