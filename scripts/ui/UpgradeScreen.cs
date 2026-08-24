using Catalyst.Upgrades;
using Godot;

namespace Catalyst.UI;

public partial class UpgradeScreen : Control
{
    private const string IconBasePath = "res://assets/art/icons/white/";

    private readonly Button[] _choiceButtons = new Button[3];
    private UpgradeSystem? _system;
    private Button _rerollButton = null!;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _choiceButtons[0] = GetNode<Button>("%Choice1");
        _choiceButtons[1] = GetNode<Button>("%Choice2");
        _choiceButtons[2] = GetNode<Button>("%Choice3");
        _rerollButton = GetNode<Button>("%RerollButton");

        for (int index = 0; index < _choiceButtons.Length; index++)
        {
            int capturedIndex = index;
            _choiceButtons[index].Pressed += () => _system?.SelectChoice(capturedIndex);
        }

        _rerollButton.Pressed += () => _system?.Reroll();
        Visible = false;
    }

    public void Bind(UpgradeSystem system)
    {
        _system = system;
        _system.ChoicesPresented += OnChoicesPresented;
        _system.ChoicesClosed += OnChoicesClosed;
    }

    public override void _ExitTree()
    {
        if (_system is not null)
        {
            _system.ChoicesPresented -= OnChoicesPresented;
            _system.ChoicesClosed -= OnChoicesClosed;
        }
    }

    private void OnChoicesPresented(IReadOnlyList<UpgradeChoice> choices, int rerollsRemaining)
    {
        Visible = true;
        for (int index = 0; index < _choiceButtons.Length; index++)
        {
            bool hasChoice = index < choices.Count;
            _choiceButtons[index].Visible = hasChoice;
            if (hasChoice)
            {
                UpgradeChoice choice = choices[index];
                _choiceButtons[index].Text = $"\n\n{choice.DisplayName}\n\n{choice.Description}";
                SetChoiceIcon(_choiceButtons[index], choice);
            }
        }

        _rerollButton.Text = $"刷新（剩余 {rerollsRemaining} 次）";
        _rerollButton.Disabled = rerollsRemaining <= 0;
        if (choices.Count > 0)
        {
            _choiceButtons[0].GrabFocus();
        }
    }

    private void OnChoicesClosed()
    {
        Visible = false;
    }

    private static void SetChoiceIcon(Button button, UpgradeChoice choice)
    {
        string iconName = choice.Kind switch
        {
            UpgradeChoiceKind.AcquireSpell => "plus",
            UpgradeChoiceKind.UpgradeSpell => "arrowUp",
            UpgradeChoiceKind.InstallWeaponCard => "target",
            UpgradeChoiceKind.InstallStandardCard => "checkmark",
            _ => "star"
        };
        Texture2D? texture = ResourceLoader.Load<Texture2D>($"{IconBasePath}{iconName}.png");
        if (button.GetNodeOrNull<TextureRect>("Icon") is TextureRect icon)
        {
            icon.Texture = texture;
        }
    }
}
