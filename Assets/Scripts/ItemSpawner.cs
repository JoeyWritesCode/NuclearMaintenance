using UnityEngine;
using UnityEngine.AI;

public class ItemSpawner : MonoBehaviour
{
    Camera cam;
    public GameObject itemPrefab;

    void Start()
    {
        cam = UnityEngine.Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit)) {
                GameObject item = Instantiate(itemPrefab, hit.point + new Vector3(0, 1, 0), Quaternion.identity);
                item.GetComponent<Renderer>().material.color = new Color(255, 0, 0);
            }
        }

        if (Input.GetMouseButtonDown(1)) {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit)) {
                GameObject item = Instantiate(itemPrefab, hit.point + new Vector3(0, 1, 0), Quaternion.identity);
                item.GetComponent<Renderer>().material.color = new Color(0, 0, 255);
            }
        }
    }
}
