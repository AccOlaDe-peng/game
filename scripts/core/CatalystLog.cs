using Godot;

namespace Catalyst.Core;

public static class CatalystLog
{
    public static void Info(string category, string message) =>
        GD.Print($"[{category}] {message}");

    public static void Warning(string category, string message) =>
        GD.PushWarning($"[{category}] {message}");

    public static void Error(string category, string message) =>
        GD.PushError($"[{category}] {message}");
}
