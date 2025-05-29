using System.Collections;
using UnityEngine;

public class DroneController : MonoBehaviour
{
    public float speed = 2f;
    public Transform baseTransform;

    private Transform target;
    private ResourceBehavior targetResource;
    private GameManager gameManager;

    private bool isCarrying, isBusy, justDelivered;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        if (!gameManager) { Debug.LogError("GameManager не найден"); enabled = false; return; }

        gameManager.DroneIsAvailable(this);
        RequestTask();
    }

    void Update()
    {
        if (isBusy && target)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

            if (!isCarrying && !targetResource)
            {
                ClearTarget();
                RequestTask();
            }
        }
    }

    public void AssignResource(ResourceBehavior resource)
    {
        if (!resource) { ClearTarget(); return; }

        target = resource.transform;
        targetResource = resource;
        isBusy = true;
        isCarrying = false;
    }

    private void RequestTask()
    {
        isBusy = false;
        gameManager.DroneIsAvailable(this);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isCarrying && other.CompareTag("Resource"))
        {
            var res = other.GetComponent<ResourceBehavior>();
            if (res && res == targetResource && !res.isCollected)
                StartCoroutine(Collect(res));
        }

        if (isCarrying && other.transform == baseTransform && !justDelivered)
        {
            justDelivered = true;
            gameManager?.ResourceDelivered(baseTransform);
            ClearTarget();
            StartCoroutine(ResetDelivery());
            RequestTask();
        }
    }

    private IEnumerator Collect(ResourceBehavior res)
    {
        if (res == null || res.isCollected)
        {
            ClearTarget();
            RequestTask();
            yield break;
        }

        Debug.Log($"[{gameObject.name}] Начинаю добычу ресурса {res.gameObject.name}...");
        yield return new WaitForSeconds(2f); // ? Задержка "добычи"

        res.Collect();
        Debug.Log($"[{gameObject.name}] Ресурс {res.gameObject.name} собран.");

        isCarrying = true;
        target = baseTransform;
        targetResource = null;

        yield return null;
    }

    private IEnumerator ResetDelivery()
    {
        yield return new WaitForSeconds(0.5f);
        justDelivered = false;
    }

    private void ClearTarget()
    {
        isCarrying = false;
        isBusy = false;
        target = null;
        targetResource = null;
    }

    public bool IsBusy() => isBusy;
}
