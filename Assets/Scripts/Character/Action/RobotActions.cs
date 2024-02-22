using Unity.Netcode;
using UnityEngine;

namespace MystroBot
{
    public class RobotActions : NetworkBehaviour
    {
        [SerializeField]
        RobotController _controller;
        [SerializeField] InputReader playerInput;
        [SerializeField] GameObject FireRightClient;
        [SerializeField] GameObject FireRightServer;

        // Start is called before the first frame update
        void Start()
        {
            if (IsLocalPlayer)
            {
                playerInput.inputActions.Player.FireLeft.performed += _ => FireLeft();
                playerInput.inputActions.Player.FireRight.performed += _ => FireRight();
                playerInput.inputActions.Player.Defense.performed += _ => UseDefense();
                playerInput.inputActions.Player.Utility.performed += _ => UseUtility();
                playerInput.inputActions.Player.Dodge.performed += _ => UseDodge();
            }
        }

        private void UseDodge()
        {
            //noop
        }

        private void UseUtility()
        {
            
            //noop
        }

        private void UseDefense()
        {
            //noop
        }

        private void FireRight()
        {
            if(!IsHost) FireRightDudeOnClient();
            if(IsOwner) FireRightserverRpc();
        }
        [ServerRpc]
        private void FireRightserverRpc(ServerRpcParams serverRpcParams = default)
        {
            var ownerClientId = serverRpcParams.Receive.SenderClientId;
            GameObject projectilePrefab = getFireRightPrefab(false);
            NetworkObject projectileNetObject = projectilePrefab.GetComponent<NetworkObject>();
            projectileNetObject.CheckObjectVisibility = (clientId) => {
                if (ownerClientId == clientId) return false;
                return true;
            };
            Vector3 projectileDirection = (_controller.mouseHitPosition-transform.position).normalized;
            GameObject instantiatedProjectile = Instantiate(projectilePrefab, transform.position+projectileDirection*0.2f, transform.rotation);
            instantiatedProjectile.GetComponent<NetworkObject>().Spawn(true);
            instantiatedProjectile.GetComponent<ServerProjectileBheaviour>().Setup(transform.forward, NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject.gameObject);
        }

        private void FireRightDudeOnClient()
        {
            GameObject projectilePrefab = getFireRightPrefab(true);
            Vector3 projectileDirection = (_controller.mouseHitPosition-transform.position).normalized;
            GameObject instantiatedProjectile = Instantiate(projectilePrefab, transform.position+projectileDirection*0.2f, transform.rotation);
            instantiatedProjectile.GetComponent<ClientProjectileBheaviour>().Setup(transform.forward);
        }

        private void FireLeft()
        {
            //noop
        }


        private GameObject getFireRightPrefab(bool client)
        {
            if (client) return FireRightClient;
            return FireRightServer;
        }
    }
}
