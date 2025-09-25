using System;
using UnityEngine;

namespace cowsins
{
    // Implemented by PlayerMovement and required by PlayerDependencies
    public interface IPlayerMovementStateProvider
    {
        PlayerOrientation Orientation { get; }
        float CurrentSpeed { get; }
        float RunSpeed { get; }
        float WalkSpeed { get; }
        float CrouchSpeed { get; }
        bool Grounded { get; }
        bool IsCrouching { get; }
        bool IsClimbing { get; }
        bool WallRunning { get; }
        bool Dashing { get; }
        bool CanShootWhileDashing { get; }
        bool DamageProtectionWhileDashing { get; }
        float NormalFOV { get; }
        float WallRunningFOV { get; }
        float FadeFOVAmount { get; }
    }

    public interface IPlayerMovementActionsProvider
    {
        void TeleportPlayer(Vector3 position, Quaternion rotation, bool resetStamina, bool resetDashes);
    }

    public interface IPlayerMovementEventsProvider
    {
        void AddJumpListener(Action callback);
        void RemoveJumpListener(Action callback);
        void AddLandListener(Action callback);
        void RemoveLandListener(Action callback);
        void AddCrouchListener(Action callback);
        void RemoveCrouchListener(Action callback);
        void AddUncrouchListener(Action callback);
        void RemoveUncrouchListener(Action callback);
    }
}