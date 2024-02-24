using System;
using MystroBot;
using Unity.Collections;
using Unity.Netcode;
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

    private NetworkVariable<FixedString128Bytes> projectileIdentifier = new();

    [SerializeField] private GameObject explosion;

    public float damage = 20f;

    public static Action<string> OnServerProjectileDestroyed;

    public override void OnNetworkSpawn()
    {
        if (!IsHost && NetworkManager.Singleton.LocalClient.ClientId == OwnerClientId)
        {
            gameObject.SetActive(false);
        }
    }

    public void Setup(Vector3 shootDirection, GameObject ownerPrefab, string projectileIdentifier)
    {
        this.projectileIdentifier.Value = projectileIdentifier;
        shootDir = shootDirection;
        Destroy(gameObject, lifeTime);
    }
    private void Update()
    {
        transform.position += shootDir * (projectileSpeed*0.1f*Time.deltaTime);
    }

    private void OnDestroy()
    {
        OnServerProjectileDestroyed?.Invoke(projectileIdentifier.Value.ToString());
        if (IsServer)
        {
            GameObject _explosion = Instantiate(explosion, transform.position, transform.rotation);
            _explosion.GetComponent<NetworkObject>().Spawn(true);
            Destroy(_explosion, explosionLifeTime);
        }
    }

    void ApplyEffects(IsDamageable target)
    {
        Debug.Log("Apply effect Server only");
        target.dealDamage(damage);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsServer)
        {
            IsDamageable robot = other.gameObject.GetComponent<IsDamageable>();
            if (robot != null && OwnerClientId != robot.getOwnerIdIfOwned())
            {
                ApplyEffects(robot);
                Destroy(gameObject);
            }
        }
    }
    
}