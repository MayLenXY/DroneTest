using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private UISettings settingsUI;

    public Transform redBase, blueBase;
    public GameObject resourcePrefab, dronePrefab;
    public float spawnInterval = 3f, spawnRadius = 5f;
    public int redDroneCount = 3, blueDroneCount = 3;

    private List<ResourceBehavior> resources = new();
    private List<DroneController> available = new();
    private List<DroneController> busy = new();

    private int deliveredRed = 0;
    private int deliveredBlue = 0;

    void Start()
    {

    }

    public void StartSimulation()
    {
        int redCount = settingsUI ? settingsUI.RedCount : redDroneCount;
        int blueCount = settingsUI ? settingsUI.BlueCount : blueDroneCount;

        SpawnDrones(redBase, redCount, Color.red);
        SpawnDrones(blueBase, blueCount, Color.blue);
        InvokeRepeating(nameof(SpawnResource), 1f, spawnInterval);
        StartCoroutine(TaskLoop());
    }

    public void DroneIsAvailable(DroneController drone)
    {
        if (!available.Contains(drone))
        {
            available.Add(drone);
            busy.Remove(drone);
        }
    }

    public void RegisterResource(ResourceBehavior res)
    {
        if (!resources.Contains(res))
            resources.Add(res);
    }

    public void ResourceDelivered(Transform baseTransform)
    {
        if (baseTransform == redBase)
            deliveredRed++;
        else if (baseTransform == blueBase)
            deliveredBlue++;

        Debug.Log($"[GameManager] Доставлено — Красная база: {deliveredRed}, Синяя база: {deliveredBlue}");
    }

    public int GetRedDelivered() => deliveredRed;
    public int GetBlueDelivered() => deliveredBlue;

    private void SpawnDrones(Transform baseTransform, int count, Color color)
    {
        for (int i = 0; i < count; i++)
        {
            var pos = baseTransform.position + (Vector3)(Random.insideUnitCircle);
            var droneGO = Instantiate(dronePrefab, pos, Quaternion.identity);
            droneGO.GetComponent<SpriteRenderer>().color = color;
            droneGO.GetComponent<DroneController>().baseTransform = baseTransform;
        }
    }

    private void SpawnResource()
    {
        var pos = (Vector2)Random.insideUnitCircle * spawnRadius;
        var go = Instantiate(resourcePrefab, pos, Quaternion.identity);
        var res = go.GetComponent<ResourceBehavior>();
        if (res) RegisterResource(res);
    }

    private IEnumerator TaskLoop()
    {
        while (true)
        {
            if (available.Count > 0 && resources.Count > 0)
            {
                DroneController bestDrone = null;
                ResourceBehavior bestRes = null;
                float minDist = float.MaxValue;

                foreach (var drone in available)
                {
                    foreach (var res in resources)
                    {
                        if (res == null || res.isCollected || res.isReserved) continue;

                        float dist = Vector3.Distance(drone.transform.position, res.transform.position);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            bestDrone = drone;
                            bestRes = res;
                        }
                    }
                }

                if (bestDrone && bestRes)
                {
                    bestRes.isReserved = true;
                    bestDrone.AssignResource(bestRes);
                    available.Remove(bestDrone);
                    busy.Add(bestDrone);
                    resources.Remove(bestRes);
                }
            }

            yield return new WaitForSeconds(0.5f);
        }
    }
}
