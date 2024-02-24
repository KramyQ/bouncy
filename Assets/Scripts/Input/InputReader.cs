using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MystroBot
{
    [CreateAssetMenu(fileName = "InputReader" ,menuName = "MystroBot/Input Reader")]
    public class InputReader : ScriptableObject, RobotInputActions.IPlayerActions, IMove
    {
        public Vector2 Move => inputActions.Player.Move.ReadValue<Vector2>();
        public RobotInputActions inputActions;

        private void OnEnable()
        {
            if (inputActions == null)
            {
                inputActions = new RobotInputActions();
                inputActions.Player.SetCallbacks(this);
            }
        }
        
        public void Enable() {
            inputActions.Enable();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            // noop
        }

        public void OnFireLeft(InputAction.CallbackContext context)
        {
            // noop
        }

        public void OnFireRight(InputAction.CallbackContext context)
        {
            //noop
        }

        public void OnUtility(InputAction.CallbackContext context)
        {
            // noop
        }

        public void OnDefense(InputAction.CallbackContext context)
        {
            // noop
        }

        public void OnDodge(InputAction.CallbackContext context)
        {
            // noop
        }
    }
}
