using UnityEngine;

namespace MystroBot
{
    public interface IMove
    {
        Vector2 Move { get; }
        void Enable();
    }
}