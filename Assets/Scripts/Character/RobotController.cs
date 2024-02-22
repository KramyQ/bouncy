using System;
using System.Collections.Generic;
using Cinemachine;
using Kart;
using TMPro;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Utilities;

namespace MystroBot
{
    // Network variables should be value objects
        public struct InputPayload : INetworkSerializable
        {
            public int tick;
            public DateTime timestamp;
            public ulong networkObjectId;
            public Vector3 inputVector;
            public Vector3 position;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref tick);
                serializer.SerializeValue(ref timestamp);
                serializer.SerializeValue(ref networkObjectId);
                serializer.SerializeValue(ref inputVector);
                serializer.SerializeValue(ref position);
            }
        }

        public struct StatePayload : INetworkSerializable
        {
            public int tick;
            public ulong networkObjectId;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 velocity;
            public Vector3 angularVelocity;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref tick);
                serializer.SerializeValue(ref networkObjectId);
                serializer.SerializeValue(ref position);
                serializer.SerializeValue(ref rotation);
                serializer.SerializeValue(ref velocity);
                serializer.SerializeValue(ref angularVelocity);
            }
        }
        public class RobotController : NetworkBehaviour
        {

        [Header("Movement Attributes")]
        [SerializeField] float maxSpeed;
        [SerializeField] float accel;

        [Header("Breathing Attributes")] 
        [SerializeField] float breathingAmplitude = 10f;
        [SerializeField] float breathingFrequency = 1f;
        [SerializeField] private Transform robotModel;
        
        [Header("Leaning Attributes")] 
        [SerializeField] float leaningSpeed = 10f;
        [SerializeField] float leaningAmplitude= 10f;
        
        
        public Vector3 Velocity = Vector3.zero;
        private float robotModelOriginalLocalPosY;
        private float targetDown;
        private float targetUp;
        
        
        // CAMERA
        [Header("Camera Attributes")]
        [SerializeField] ClientCameraController clientCameraController;
        [SerializeField] private float cameraRotationSpeed = 10f;
        public Vector3 mouseHitPosition = new Vector3(0, 0, 0);
            
        private Quaternion m_targetRotation = new Quaternion(0,0,0,0);
        
        [SerializeField] float gravity = Physics.gravity.y;
        [SerializeField] float lateralGScale = 10f; // Scaling factor for lateral G forces;

        [Header("Refs")] [SerializeField] InputReader playerInput;

        Rigidbody rb;

        Vector3 RobotVelocity;

        float originalY;
        float adjustedY;
        float yDiff;
        Vector3 syncPosition;

        RaycastHit hit;

        public bool IsGrounded = true;
        public bool hoveringDown = false;
        public Vector3 velocity => RobotVelocity;
        public float MaxSpeed => maxSpeed;

        // Netcode general
        NetworkTimer networkTimer;
        const float k_serverTickRate = 60f; // 60 FPS
        const int k_bufferSize = 1024;

        // Netcode client specific
        CircularBuffer<StatePayload> clientStateBuffer;
        CircularBuffer<InputPayload> clientInputBuffer;
        StatePayload lastServerState;
        StatePayload lastProcessedState;

        ClientNetworkTransform clientNetworkTransform;

        // Netcode server specific
        CircularBuffer<StatePayload> serverStateBuffer;
        Queue<InputPayload> serverInputQueue;

        [Header("Netcode")] [SerializeField] float reconciliationCooldownTime = 1f;
        [SerializeField] float reconciliationThreshold = 10f;
        CountdownTimer reconciliationTimer;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            clientNetworkTransform = GetComponent<ClientNetworkTransform>();
            playerInput.Enable();
            
            // Breathing
            robotModelOriginalLocalPosY = robotModel.transform.localPosition.y;
            targetDown = robotModelOriginalLocalPosY - breathingAmplitude * 0.5f;
            targetUp = robotModelOriginalLocalPosY + breathingAmplitude * 0.5f;




                networkTimer = new NetworkTimer(k_serverTickRate);
            clientStateBuffer = new CircularBuffer<StatePayload>(k_bufferSize);
            clientInputBuffer = new CircularBuffer<InputPayload>(k_bufferSize);

            serverStateBuffer = new CircularBuffer<StatePayload>(k_bufferSize);
            serverInputQueue = new Queue<InputPayload>();

            reconciliationTimer = new CountdownTimer(reconciliationCooldownTime);

            reconciliationTimer.OnTimerStart += () => {};
            
        }

        void SwitchAuthorityMode(AuthorityMode mode)
        {
            clientNetworkTransform.authorityMode = mode;
            bool shouldSync = mode == AuthorityMode.Client;
            clientNetworkTransform.SyncPositionX = shouldSync;
            clientNetworkTransform.SyncPositionY = shouldSync;
            clientNetworkTransform.SyncPositionZ = shouldSync;
        }

        void Update()
        {
            networkTimer.Update(Time.deltaTime);
            reconciliationTimer.Tick(Time.deltaTime);

            /*playerText.SetText(
                $"Owner: {IsOwner} NetworkObjectId: {NetworkObjectId} Velocity: {RobotVelocity.magnitude:F1}");*/
        }

        void FixedUpdate()
        {
            while (networkTimer.ShouldTick())
            {
                HandleClientTick();
                HandleServerTick();
            }
        }

        void HandleServerTick()
        {
            if (!IsServer) return;

            var bufferIndex = -1;
            InputPayload inputPayload = default;
            while (serverInputQueue.Count > 0)
            {
                inputPayload = serverInputQueue.Dequeue();

                bufferIndex = inputPayload.tick % k_bufferSize;

                StatePayload statePayload = ProcessMovement(inputPayload);
                serverStateBuffer.Add(statePayload, bufferIndex);
            }

            if (bufferIndex == -1) return;
            SendToClientRpc(serverStateBuffer.Get(bufferIndex));
        }

        static float CalculateLatencyInMillis(InputPayload inputPayload) =>
            (DateTime.Now - inputPayload.timestamp).Milliseconds / 1000f;
        

        [ClientRpc]
        void SendToClientRpc(StatePayload statePayload)
        {
            /*clientRpcText.SetText(
                $"Received state from server Tick {statePayload.tick} Server POS: {statePayload.position}");*/
            if (!IsOwner) return;
            lastServerState = statePayload;
        }

        void HandleClientTick()
        {
            if (!IsClient || !IsOwner) return;

            var currentTick = networkTimer.CurrentTick;
            var bufferIndex = currentTick % k_bufferSize;

            InputPayload inputPayload = new InputPayload()
            {
                tick = currentTick,
                timestamp = DateTime.Now,
                networkObjectId = NetworkObjectId,
                inputVector = playerInput.Move,
                position = transform.position
            };

            clientInputBuffer.Add(inputPayload, bufferIndex);
            SendToServerRpc(inputPayload);

            StatePayload statePayload = ProcessMovement(inputPayload);
            clientStateBuffer.Add(statePayload, bufferIndex);

            HandleServerReconciliation();
        }

        bool ShouldReconcile()
        {
            bool isNewServerState = !lastServerState.Equals(default);
            bool isLastStateUndefinedOrDifferent = lastProcessedState.Equals(default)
                                                   || !lastProcessedState.Equals(lastServerState);

            return isNewServerState && isLastStateUndefinedOrDifferent && !reconciliationTimer.IsRunning;
        }

        void HandleServerReconciliation()
        {
            if (!ShouldReconcile()) return;
            if (!ShouldReconcile()) return;

            float positionError;
            int bufferIndex;

            bufferIndex = lastServerState.tick % k_bufferSize;
            if (bufferIndex - 1 < 0) return; // Not enough information to reconcile

            StatePayload
                rewindState =
                    IsHost
                        ? serverStateBuffer.Get(bufferIndex - 1)
                        : lastServerState; // Host RPCs execute immediately, so we can use the last server state
            StatePayload clientState =
                IsHost ? clientStateBuffer.Get(bufferIndex - 1) : clientStateBuffer.Get(bufferIndex);
            positionError = Vector3.Distance(rewindState.position, clientState.position);

            if (positionError > reconciliationThreshold)
            {
                ReconcileState(rewindState);
                reconciliationTimer.Start();
            }

            lastProcessedState = rewindState;
        }

        void ReconcileState(StatePayload rewindState)
        {
            transform.position = rewindState.position;
            transform.rotation = rewindState.rotation;
            rb.velocity = rewindState.velocity;
            rb.angularVelocity = rewindState.angularVelocity;

            if (!rewindState.Equals(lastServerState)) return;

            clientStateBuffer.Add(rewindState, rewindState.tick % k_bufferSize);

            // Replay all inputs from the rewind state to the current state
            int tickToReplay = lastServerState.tick;

            while (tickToReplay < networkTimer.CurrentTick)
            {
                int bufferIndex = tickToReplay % k_bufferSize;
                StatePayload statePayload = ProcessMovement(clientInputBuffer.Get(bufferIndex));
                clientStateBuffer.Add(statePayload, bufferIndex);
                tickToReplay++;
            }
        }

        [ServerRpc]
        void SendToServerRpc(InputPayload input)
        {
            /*serverRpcText.SetText($"Received input from client Tick: {input.tick} Client POS: {input.position}");*/
            serverInputQueue.Enqueue(input);
        }

        StatePayload ProcessMovement(InputPayload input)
        {
            Move(input.inputVector);
            RotateCharacterToMouseCursor();
            RobotBreathing();
            /*LeanTowardVelocity();*/

            return new StatePayload()
            {
                tick = input.tick,
                networkObjectId = NetworkObjectId,
                position = transform.position,
                rotation = transform.rotation,
                velocity = rb.velocity,
                angularVelocity = rb.angularVelocity
            };
        }

        /*
        void LeanTowardVelocity()
        {
            if (!shouldLean()) return;

            Vector3 direction = rb.velocity.normalized;
            Vector3 rotationAxis = Vector3.Cross(Vector3.up, direction);
            transform.RotateAround(transform.position, rotationAxis, 1*leaningSpeed);
        }

        bool shouldLean()
        {
            Vector3 rotation = transform.eulerAngles;
            return math.abs(rotation.x) < leaningAmplitude || math.abs(rotation.z) < leaningAmplitude;
        }
        */
        
        void RotateCharacterToMouseCursor()
        {
            Ray ray = clientCameraController._camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hitInfo, maxDistance: 1000f))
            {
                Quaternion rot = transform.rotation;
                mouseHitPosition = hitInfo.point;
                mouseHitPosition.y = transform.position.y;
                Quaternion targetRotation = Quaternion.LookRotation(mouseHitPosition - transform.position);
                targetRotation.x = rot.x;
                targetRotation.z = rot.z;
                m_targetRotation = targetRotation;
            }
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, m_targetRotation, cameraRotationSpeed).normalized);
        }

        void RobotBreathing()
        {
            float currentY = robotModel.transform.localPosition.y;
            if (hoveringDown)
            {
                if (targetDown < currentY)
                {
                    Vector3 targetPosition = robotModel.transform.localPosition;
                    targetPosition.y = targetDown - 1;
                    robotModel.transform.localPosition = Vector3.SmoothDamp(robotModel.transform.localPosition, targetPosition, ref Velocity, breathingFrequency);
                }
                else
                {
                    hoveringDown = !hoveringDown;
                }
            }
            else
            {
                if (targetUp > currentY)
                {
                    Vector3 targetPosition = robotModel.transform.localPosition;
                    targetPosition.y = targetUp + 1;
                    robotModel.transform.localPosition = Vector3.SmoothDamp(robotModel.transform.localPosition, targetPosition, ref Velocity, breathingFrequency);
                }
                else
                {
                    hoveringDown = !hoveringDown;
                }
            }
        }

        void Move(Vector2 inputVector)
        {
            float verticalInput = AdjustInput(playerInput.Move.y);
            float horizontalInput = AdjustInput(playerInput.Move.x);

            if (Mathf.Abs(verticalInput) + Mathf.Abs(horizontalInput) > 1)
            {
                verticalInput = verticalInput * 0.7f;
                horizontalInput = horizontalInput * 0.7f;
            }
            
            RobotVelocity = transform.InverseTransformDirection(rb.velocity);
            HandleGroundedMovement(verticalInput, horizontalInput);
      
        }

        void HandleGroundedMovement(float verticalInput, float horizontalInput)
        {
            // Acceleration Logic per server delta time
            float verticalTargetSpeed = verticalInput * maxSpeed;
            float horizontalTargetSpeed = horizontalInput * maxSpeed;

            Vector3 newVelocity = new Vector3(horizontalTargetSpeed, 0, verticalTargetSpeed);
            rb.MovePosition(rb.position + newVelocity * Time.deltaTime);
        }

        float AdjustInput(float input)
        {
            return input switch
            {
                >= .7f => 1f,
                <= -.7f => -1f,
                _ => input
            };
        }
    }
}