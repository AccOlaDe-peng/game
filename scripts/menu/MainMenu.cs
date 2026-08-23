using Godot;
using Catalyst.App;
using Catalyst.UI;
using Catalyst.Meta;

namespace Catalyst.Menu;

public partial class MainMenu : Control
{
    [Signal]
    public delegate void StartRequestedEventHandler(string characterId);

    [Signal]
    public delegate void QuitRequestedEventHandler();

    public override void _Ready()
    {
        Button startButton = GetNode<Button>("%StartButton");
        Button quitButton = GetNode<Button>("%QuitButton");
        Button settingsButton = GetNode<Button>("%SettingsButton");
        SettingsScreen settings = GetNode<SettingsScreen>("SettingsScreen");
        SaveService saves = GetNode<SaveService>("/root/SaveService");
        ProfileData profile = saves.Profile;
        string selectedId = profile.SelectedCharacterId;
        Label selectionLabel = GetNode<Label>("%SelectionLabel");
        Label detailsLabel = GetNode<Label>("%CharacterDetailsLabel");

        void Select(string id)
        {
            if (!saves.IsCharacterUnlocked(id)) return;
            selectedId = id;
            CharacterDefinition character = CharacterCatalog.Get(id);
            selectionLabel.Text = $"已选择：{character.DisplayName} · {character.StartingWeaponName}";
            detailsLabel.Text = $"{character.Role}\n{character.Description}\n{character.ActiveAbility}\n{character.PassiveAbility}";
        }

        ConfigureCharacterButton(GetNode<Button>("%ElementalistButton"), CharacterCatalog.Get(CharacterCatalog.ElementalistId), saves, Select);
        ConfigureCharacterButton(GetNode<Button>("%HunterButton"), CharacterCatalog.Get(CharacterCatalog.EchoHunterId), saves, Select);
        ConfigureCharacterButton(GetNode<Button>("%EngineerButton"), CharacterCatalog.Get(CharacterCatalog.RiftEngineerId), saves, Select);
        Select(selectedId);
        startButton.Pressed += () =>
        {
            if (saves.SelectCharacter(selectedId)) EmitSignal(SignalName.StartRequested, selectedId);
        };
        quitButton.Pressed += () => EmitSignal(SignalName.QuitRequested);
        settingsButton.Pressed += settings.Open;
        settings.Closed += settingsButton.GrabFocus;
        GetNode<Label>("%RecordLabel").Text = profile.BestSurvivalTime <= 0.0
            ? $"记忆碎片 {profile.MemoryShards} · 尚无挑战记录"
            : $"最佳生存 {TimeSpan.FromSeconds(profile.BestSurvivalTime):mm\\:ss}  ·  最多击杀 {profile.BestKillCount}" +
              (profile.BossDefeated ? "  ·  灰烬巨像已击败" : string.Empty) + $"  ·  记忆碎片 {profile.MemoryShards}";
        startButton.GrabFocus();
    }

    private static void ConfigureCharacterButton(Button button, CharacterDefinition character, SaveService saves, Action<string> select)
    {
        bool unlocked = saves.IsCharacterUnlocked(character.Id);
        button.Text = unlocked ? $"{character.DisplayName}\n{character.StartingWeaponName}" : $"🔒 {character.DisplayName}\n{character.UnlockHint}";
        button.Disabled = !unlocked;
        button.Pressed += () => select(character.Id);
    }
}
