using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace MystroBot
{
    public class ClientCameraController : NetworkBehaviour
    {
        [SerializeField] CinemachineVirtualCamera playerCamera;
        [SerializeField] AudioListener playerAudioListener;
        [SerializeField] private Transform robot;
        [SerializeField] public Camera _camera;
        [SerializeField] private float cameraHeight = 50f;
        [SerializeField] private float offset = 10f;

        public Vector3 Velocity = Vector3.zero;
        public float SmoothTime = 5f;
        

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                playerAudioListener.enabled = false;
                playerCamera.Priority = 0;
                return;
            }
            playerCamera.Priority = 100;
            playerAudioListener.enabled = true;
            transform.parent = null;
        }
        private void LateUpdate()
        {
            if (robot)
            {
                Vector3 targetPosition = robot.position + Vector3.up * cameraHeight + Vector3.back * offset;
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref Velocity, SmoothTime);
            }
        }
    }
}
