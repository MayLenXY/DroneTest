using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private UISettings settingsUI;
    [SerializeField] private LayerMask obstacleLayer;

    [SerializeField] public GameObject deliveryEffectPrefab;

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

            var drone = droneGO.GetComponent<DroneController>();
            drone.baseTransform = baseTransform;
            drone.speed = settingsUI ? settingsUI.DroneSpeed : 2f;
        }

    }

    private void SpawnResource()
    {
        for (int attempts = 0; attempts < 10; attempts++)
        {
            Vector2 spawnPosition = Random.insideUnitCircle * spawnRadius;

            Collider2D hit = Physics2D.OverlapCircle(spawnPosition, 0.3f, obstacleLayer);

            if (hit == null)
            {
                GameObject newResourceGO = Instantiate(resourcePrefab, spawnPosition, Quaternion.identity);
                ResourceBehavior newResource = newResourceGO.GetComponent<ResourceBehavior>();

                if (newResource != null)
                    RegisterResource(newResource);
                else
                    Debug.LogError("Созданный ресурс не имеет компонента ResourceBehavior!");

                return;
            }
        }
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

    public void ToggleDrawPath(bool state)
    {
        foreach (var drone in FindObjectsOfType<DroneController>())
        {
            drone.SetDrawPath(state);
        }
    }
}
