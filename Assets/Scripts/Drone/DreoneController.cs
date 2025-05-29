using System.Collections;
using UnityEngine;

public class DroneController : MonoBehaviour
{
    [Header("Навигация")]
    [SerializeField] private float avoidanceRadius = 1f;
    [SerializeField] private float avoidanceForce = 1f;
    [SerializeField] private LayerMask droneLayer;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Параметры")]
    public float speed = 2f;
    public Transform baseTransform;

    private Transform target;
    private ResourceBehavior targetResource;
    private GameManager gameManager;

    private bool isCarrying, isBusy, justDelivered;

    private LineRenderer lineRenderer;
    private bool drawPath = false;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        if (!gameManager) { Debug.LogError("GameManager не найден"); enabled = false; return; }

        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = false;
        }

        gameManager.DroneIsAvailable(this);
        RequestTask();
    }

    void Update()
    {
        if (isBusy && target)
        {
            MoveToTarget(target.position);

            if (!isCarrying && !targetResource)
            {
                ClearTarget();
                RequestTask();
            }

            // Отрисовка пути
            if (drawPath && lineRenderer != null)
            {
                lineRenderer.enabled = true;
                lineRenderer.SetPosition(0, transform.position);
                lineRenderer.SetPosition(1, target.position);
            }
        }
        else if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    private void MoveToTarget(Vector3 destination)
    {
        Vector3 direction = (destination - transform.position).normalized;

        // Избежание других дронов
        Collider2D[] nearbyDrones = Physics2D.OverlapCircleAll(transform.position, avoidanceRadius, droneLayer);
        Vector3 avoidance = Vector3.zero;

        foreach (var drone in nearbyDrones)
        {
            if (drone.gameObject != gameObject)
            {
                Vector3 away = transform.position - drone.transform.position;
                avoidance += away.normalized / Mathf.Max(away.magnitude, 0.01f);
            }
        }

        // Обход препятствий
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, 0.5f, obstacleLayer);
        if (hit.collider != null)
        {
            direction = Vector3.Cross(direction, Vector3.forward).normalized;
        }

        Vector3 finalDir = (direction + avoidance * avoidanceForce).normalized;
        transform.position += finalDir * speed * Time.deltaTime;
    }

    public void SetDrawPath(bool value)
    {
        drawPath = value;
        if (lineRenderer != null)
            lineRenderer.enabled = value;
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
            if (gameManager != null)
            {
                gameManager.ResourceDelivered(baseTransform);

                // ?? ЭФФЕКТ
                if (gameManager.deliveryEffectPrefab != null)
                {
                    Instantiate(gameManager.deliveryEffectPrefab, baseTransform.position, Quaternion.identity);
                }
            }

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
        yield return new WaitForSeconds(2f);

        res.Collect();
        Debug.Log($"[{gameObject.name}] Ресурс {res.gameObject.name} собран.");

        isCarrying = true;
        target = baseTransform;
        targetResource = null;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, avoidanceRadius);
    }
}
