using Godot;

namespace Catalyst.App;

public static class InputBootstrap
{
    public const string MoveLeft = "move_left";
    public const string MoveRight = "move_right";
    public const string MoveForward = "move_forward";
    public const string MoveBack = "move_back";
    public const string AimLeft = "aim_left";
    public const string AimRight = "aim_right";
    public const string AimUp = "aim_up";
    public const string AimDown = "aim_down";
    public const string Dodge = "dodge";
    public const string Catalyze = "catalyze";
    public const string Pause = "pause";

    public static void EnsureDefaultBindings()
    {
        EnsureAction(MoveLeft, Key.A, JoyAxis.LeftX, -1.0f);
        EnsureAction(MoveRight, Key.D, JoyAxis.LeftX, 1.0f);
        EnsureAction(MoveForward, Key.W, JoyAxis.LeftY, -1.0f);
        EnsureAction(MoveBack, Key.S, JoyAxis.LeftY, 1.0f);
        EnsureAction(AimLeft, Key.None, JoyAxis.RightX, -1.0f);
        EnsureAction(AimRight, Key.None, JoyAxis.RightX, 1.0f);
        EnsureAction(AimUp, Key.None, JoyAxis.RightY, -1.0f);
        EnsureAction(AimDown, Key.None, JoyAxis.RightY, 1.0f);
        EnsureButtonAction(Dodge, Key.Space, JoyButton.A);
        EnsureTriggerAction(Catalyze, MouseButton.Right, JoyAxis.TriggerRight);
        EnsureButtonAction(Pause, Key.Escape, JoyButton.Start);
    }

    private static void EnsureAction(string action, Key key, JoyAxis axis, float axisValue)
    {
        EnsureActionExists(action);
        if (InputMap.ActionGetEvents(action).Count > 0)
        {
            return;
        }

        if (key != Key.None)
        {
            InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
        }

        InputMap.ActionAddEvent(action, new InputEventJoypadMotion
        {
            Axis = axis,
            AxisValue = axisValue
        });
    }

    private static void EnsureButtonAction(string action, Key key, JoyButton button)
    {
        EnsureActionExists(action);
        if (InputMap.ActionGetEvents(action).Count > 0)
        {
            return;
        }

        InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
        InputMap.ActionAddEvent(action, new InputEventJoypadButton { ButtonIndex = button });
    }

    private static void EnsureTriggerAction(string action, MouseButton mouseButton, JoyAxis axis)
    {
        EnsureActionExists(action);
        if (InputMap.ActionGetEvents(action).Count > 0)
        {
            return;
        }

        InputMap.ActionAddEvent(action, new InputEventMouseButton { ButtonIndex = mouseButton });
        InputMap.ActionAddEvent(action, new InputEventJoypadMotion
        {
            Axis = axis,
            AxisValue = 1.0f
        });
    }

    private static void EnsureActionExists(string action)
    {
        if (!InputMap.HasAction(action))
        {
            InputMap.AddAction(action, 0.2f);
        }
    }
}
