using Catalyst.Menu;
using Catalyst.Run;
using Godot;

namespace Catalyst.App;

public partial class AppRoot : Node
{
    [Export]
    public PackedScene MainMenuScene { get; set; } = null!;

    [Export]
    public PackedScene RunScene { get; set; } = null!;

    private SceneRouter _router = null!;

    public override void _Ready()
    {
        InputBootstrap.EnsureDefaultBindings();
        _router = GetNode<SceneRouter>("SceneRouter");
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        GetTree().Paused = false;
        MainMenu menu = _router.Show<MainMenu>(MainMenuScene);
        menu.StartRequested += ShowRun;
        menu.QuitRequested += OnQuitRequested;
    }

    public void ShowRun(string characterId = "")
    {
        SaveService saves = GetNode<SaveService>("/root/SaveService");
        if (!string.IsNullOrEmpty(characterId)) saves.SelectCharacter(characterId);
        GetTree().Paused = false;
        RunRoot run = _router.Show<RunRoot>(RunScene);
        run.ReturnToMenuRequested += ShowMainMenu;
        run.RestartRequested += () => ShowRun(saves.ActiveCharacter.Id);
    }

    private void OnQuitRequested()
    {
        GetTree().Quit();
    }
}
