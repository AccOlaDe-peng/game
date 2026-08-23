using Godot;

namespace Catalyst.Tests;

public partial class ArtPreviewRunner : Node
{
    private int _framesRemaining = 45;

    public override void _Process(double delta)
    {
        _framesRemaining--;
        if (_framesRemaining > 0)
        {
            return;
        }

        string outputDirectory = ProjectSettings.GlobalizePath("res://artifacts");
        DirAccess.MakeDirRecursiveAbsolute(outputDirectory);
        string outputPath = outputDirectory.PathJoin("art_preview.png");
        PrintAnimationState(GetTree().CurrentScene);
        Error result = GetViewport().GetTexture().GetImage().SavePng(outputPath);
        GD.Print(result == Error.Ok
            ? $"[Tests] ART_PREVIEW_OK {outputPath}"
            : $"[Tests] ART_PREVIEW_FAILED {result}");
        GetTree().Quit(result == Error.Ok ? 0 : 1);
    }

    private static void PrintAnimationState(Node? node)
    {
        if (node is null)
        {
            return;
        }

        if (node is AnimationPlayer player)
        {
            GD.Print($"[Tests] AnimationPlayer {player.GetPath()} current={player.CurrentAnimation} " +
                $"playing={player.IsPlaying()} animations={string.Join(',', player.GetAnimationList())}");
        }

        foreach (Node child in node.GetChildren())
        {
            PrintAnimationState(child);
        }
    }
}
