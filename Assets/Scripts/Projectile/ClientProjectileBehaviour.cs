using UnityEngine;

public class ClientProjectileBheaviour : MonoBehaviour
{
    private Vector3 shootDir;
    [SerializeField]
    float projectileSpeed = 5f;
    [SerializeField]
    float lifeTime = 3f;
    [SerializeField]
    float explosionLifeTime = 2f;

    public float damage = 20f;

    public void Setup(Vector3 shootDirection)
    {
        shootDir = shootDirection;
        Destroy(gameObject, lifeTime);
    }
    private void Update()
    {
        transform.position += shootDir * (projectileSpeed*0.1f*Time.deltaTime);
    }
}