using UnityEngine;

public class ResourceBehavior : MonoBehaviour
{
    public bool isCollected = false;
    public bool isReserved = false;

    void Start()
    {
        var manager = FindObjectOfType<GameManager>();
        if (manager != null) manager.RegisterResource(this);
    }

    public void Collect()
    {
        isCollected = true;
        Destroy(gameObject);
    }
}
