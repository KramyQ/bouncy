using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class ServerProjectileBheaviour : NetworkBehaviour
{
    private Vector3 shootDir;
    [SerializeField]
    float projectileSpeed = 5f;
    [SerializeField]
    float lifeTime = 3f;
    [SerializeField]
    float explosionLifeTime = 2f;

    [SerializeField] private GameObject explosion;

    public float damage = 20f;

    public void Setup(Vector3 shootDirection, GameObject ownerPrefab)
    {
        Physics.IgnoreCollision(gameObject.GetComponentInChildren<Collider>(), ownerPrefab.GetComponent<Collider>());
        shootDir = shootDirection;
        Destroy(gameObject, lifeTime);
    }
    private void Update()
    {
        transform.position += shootDir * (projectileSpeed*0.1f*Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (IsServer)
        {
            GameObject _explosion = Instantiate(explosion, transform.position, transform.rotation);
            _explosion.GetComponent<NetworkObject>().Spawn(true);
            Destroy(_explosion, explosionLifeTime);
        }
    }


}