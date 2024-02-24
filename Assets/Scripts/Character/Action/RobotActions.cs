using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MystroBot
{
    public class RobotActions : NetworkBehaviour
    {
        [SerializeField]
        RobotController _controller;
        [SerializeField] InputReader playerInput;
        [SerializeField] GameObject FireRightClient;
        [SerializeField] GameObject FireRightServer;
        [SerializeField] private GameObject robotUI;
        [SerializeField] private RobotState robotState;

        public override void OnNetworkSpawn()
        {
            if (IsLocalPlayer && IsOwner)
            {
                playerInput.inputActions.Player.FireLeft.performed += FireLeft;
                playerInput.inputActions.Player.FireRight.performed += FireRight;
                playerInput.inputActions.Player.Defense.performed += UseDefense;
                playerInput.inputActions.Player.Utility.performed += UseUtility;
                playerInput.inputActions.Player.Dodge.performed += UseDodge;
                robotUI.SetActive(true);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsLocalPlayer && IsOwner)
            {
                playerInput.inputActions.Player.FireLeft.performed -= FireLeft;
                playerInput.inputActions.Player.FireRight.performed -= FireRight;
                playerInput.inputActions.Player.Defense.performed -= UseDefense;
                playerInput.inputActions.Player.Utility.performed -= UseUtility;
                playerInput.inputActions.Player.Dodge.performed -= UseDodge;
            }
        }

        private void UseDodge(InputAction.CallbackContext callbackContext)
        {
            IncreaseHealthserverRpc();
        }

        private void UseUtility(InputAction.CallbackContext callbackContext)
        {
            
            //noop
        }

        private void UseDefense(InputAction.CallbackContext callbackContext)
        {
            //noop
        }

        private void FireRight(InputAction.CallbackContext callbackContext)
        {
            string projectileIdentifier = System.Guid.NewGuid().ToString();
            if(!IsHost){ FireRightDudeOnClient(projectileIdentifier);}
            if(IsOwner){ FireRightserverRpc(projectileIdentifier, _controller.mouseHitPosition);}
        }
        [ServerRpc] private void FireRightserverRpc(string projectileIdentifier, Vector3 mousePosition, ServerRpcParams serverRpcParams = default)
        {
            var ownerClientId = serverRpcParams.Receive.SenderClientId;
            GameObject projectilePrefab = getFireRightPrefab(false);
            Vector3 projectileDirection = (mousePosition-transform.position).normalized;

            GameObject instantiatedProjectile = Instantiate(projectilePrefab, transform.position+projectileDirection*0.2f, transform.rotation);
            instantiatedProjectile.GetComponent<ServerProjectileBheaviour>().Setup(projectileDirection, NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject.gameObject, projectileIdentifier);
            instantiatedProjectile.GetComponent<NetworkObject>().SpawnWithOwnership(ownerClientId);
        }
        
    [ServerRpc]  private void IncreaseHealthserverRpc(ServerRpcParams serverRpcParams = default)
    {
        robotState.currentHealth.Value += 10;
    }


        private void FireRightDudeOnClient(string projectileIdentifier)
        {
            GameObject projectilePrefab = getFireRightPrefab(true);
            Vector3 projectileDirection = (_controller.mouseHitPosition-transform.position).normalized;
            GameObject instantiatedProjectile = Instantiate(projectilePrefab, transform.position+projectileDirection*0.2f, transform.rotation);
            instantiatedProjectile.GetComponent<ClientProjectileBehaviour>().Setup(projectileDirection, projectileIdentifier);
        }

        private void FireLeft(InputAction.CallbackContext callbackContext)
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
