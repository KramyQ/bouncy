using System;
using Unity.Collections;
using UnityEngine;

public class ClientProjectileBehaviour : MonoBehaviour
{
    private Vector3 shootDir;
    [SerializeField]
    float projectileSpeed = 5f;
    [SerializeField]
    float lifeTime = 3f;
    [SerializeField]
    float explosionLifeTime = 2f;

    private void OnEnable()
    {
        ServerProjectileBheaviour.OnServerProjectileDestroyed += OnProjectileDestroyed;
    }

    private void OnDisable()
    {
        ServerProjectileBheaviour.OnServerProjectileDestroyed -= OnProjectileDestroyed;
    }

    private string projectileIdentifier;

    public float damage = 20f;

    public void Setup(Vector3 shootDirection, string projectileIdentifier)
    {
        this.projectileIdentifier = projectileIdentifier;
        shootDir = shootDirection;
        Destroy(gameObject, lifeTime);
    }
    private void Update()
    {
        transform.position += shootDir * (projectileSpeed*0.1f*Time.deltaTime);
    } 
    void OnProjectileDestroyed(string projectileIdentifier)
    {
        if (projectileIdentifier == this.projectileIdentifier)
        {
            Destroy(gameObject);
        }
    }
}