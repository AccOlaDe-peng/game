using Catalyst.Run;
using Catalyst.Player;
using Catalyst.Elements;
using Catalyst.Spells;
using Catalyst.Events;
using Catalyst.Enemies;
using Catalyst.App;
using Catalyst.Boss;
using Godot;

namespace Catalyst.UI;

public partial class RunHud : Control
{
    [Signal]
    public delegate void PauseRequestedEventHandler();

    [Signal]
    public delegate void ReturnRequestedEventHandler();

    [Signal]
    public delegate void RestartRequestedEventHandler();

    private RunController? _controller;
    private Label _timeLabel = null!;
    private Label _stateLabel = null!;
    private Label _seedLabel = null!;
    private Label _healthLabel = null!;
    private ProgressBar _healthBar = null!;
    private Label _levelLabel = null!;
    private ProgressBar _experienceBar = null!;
    private Control _pausePanel = null!;
    private Label _pauseTitle = null!;
    private Button _resumeButton = null!;
    private PlayerHealth? _health;
    private PlayerProgression? _progression;
    private CatalyzeAbility? _catalyze;
    private ElementSystem? _elements;
    private ProgressBar _catalyzeBar = null!;
    private Label _catalyzeLabel = null!;
    private Label _reactionLabel = null!;
    private Label _spellLabel = null!;
    private SpellSystem? _spells;
    private RunEventSystem? _events;
    private Label _objectiveLabel = null!;
    private ProgressBar _objectiveBar = null!;
    private EnemySystem? _enemies;
    private Label _eliteLabel = null!;
    private ProgressBar _eliteBar = null!;
    private double _eliteRefreshRemaining;
    private double _reactionMessageRemaining;
    private ProgressBar _bossStaggerBar = null!;
    private BossController? _boss;
    private Label _inputHint = null!;
    private bool _usingGamepad;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _timeLabel = GetNode<Label>("%TimeLabel");
        _stateLabel = GetNode<Label>("%StateLabel");
        _seedLabel = GetNode<Label>("%SeedLabel");
        _healthLabel = GetNode<Label>("%HealthLabel");
        _healthBar = GetNode<ProgressBar>("%HealthBar");
        _levelLabel = GetNode<Label>("%LevelLabel");
        _experienceBar = GetNode<ProgressBar>("%ExperienceBar");
        _catalyzeBar = GetNode<ProgressBar>("%CatalyzeBar");
        _catalyzeLabel = GetNode<Label>("%CatalyzeLabel");
        _reactionLabel = GetNode<Label>("%ReactionLabel");
        _spellLabel = GetNode<Label>("%SpellHint");
        _objectiveLabel = GetNode<Label>("%ObjectiveLabel");
        _objectiveBar = GetNode<ProgressBar>("%ObjectiveBar");
        _eliteLabel = GetNode<Label>("%EliteLabel");
        _eliteBar = GetNode<ProgressBar>("%EliteBar");
        _bossStaggerBar = GetNode<ProgressBar>("%BossStaggerBar");
        _pausePanel = GetNode<Control>("%PausePanel");
        _pauseTitle = GetNode<Label>("%PauseTitle");
        _resumeButton = GetNode<Button>("%ResumeButton");
        _inputHint = GetNode<Label>("%Hint");

        GetNode<Button>("%PauseButton").Pressed += () => EmitSignal(SignalName.PauseRequested);
        GetNode<Button>("%ResumeButton").Pressed += () => EmitSignal(SignalName.PauseRequested);
        GetNode<Button>("%ReturnButton").Pressed += () => EmitSignal(SignalName.ReturnRequested);
        GetNode<Button>("%RestartButton").Pressed += () =>
            GetNode<ConfirmationDialog>("%RestartConfirmation").PopupCentered();
        GetNode<ConfirmationDialog>("%RestartConfirmation").Confirmed += () =>
            EmitSignal(SignalName.RestartRequested);
        GetNode<Button>("%SettingsButton").Pressed += () =>
            GetNode<SettingsScreen>("../SettingsScreen").Open();
        SetButtonIcon(GetNode<Button>("%PauseButton"), "pause");
        SetButtonIcon(GetNode<Button>("%ResumeButton"), "forward");
        SetButtonIcon(GetNode<Button>("%SettingsButton"), "gear");
        SetButtonIcon(GetNode<Button>("%RestartButton"), "rewind");
        SetButtonIcon(GetNode<Button>("%ReturnButton"), "home");
        RefreshInputHint();
    }

    private static void SetButtonIcon(Button button, string iconName)
    {
        Texture2D? texture = ResourceLoader.Load<Texture2D>(
            $"res://assets/art/icons/white/{iconName}.png");
        if (texture is not null)
        {
            Image image = texture.GetImage();
            image.Resize(22, 22, Image.Interpolation.Bilinear);
            button.Icon = ImageTexture.CreateFromImage(image);
        }
    }

    public override void _Input(InputEvent @event)
    {
        bool gamepad = @event is InputEventJoypadButton ||
            (@event is InputEventJoypadMotion motion && Math.Abs(motion.AxisValue) > 0.35f);
        bool keyboard = @event is InputEventKey or InputEventMouseButton or InputEventMouseMotion;
        if ((gamepad && !_usingGamepad) || (keyboard && _usingGamepad))
        {
            _usingGamepad = gamepad;
            RefreshInputHint();
        }
    }

    public override void _Process(double delta)
    {
        if (_controller is null)
        {
            return;
        }

        TimeSpan time = TimeSpan.FromSeconds(_controller.ElapsedSeconds);
        _timeLabel.Text = $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
        if (_catalyze is not null)
        {
            _catalyzeBar.MaxValue = _catalyze.Cooldown;
            _catalyzeBar.Value = _catalyze.Cooldown - _catalyze.CooldownRemaining;
            _catalyzeLabel.Text = _catalyze.CooldownRemaining <= 0.0f
                ? "元素催化：就绪"
                : $"元素催化：{_catalyze.CooldownRemaining:0.0}s";
        }

        _reactionMessageRemaining = Math.Max(0.0, _reactionMessageRemaining - delta);
        _reactionLabel.Visible = _reactionMessageRemaining > 0.0;

        _eliteRefreshRemaining -= delta;
        if (_eliteRefreshRemaining <= 0.0 && _enemies is not null)
        {
            _eliteRefreshRemaining = 0.2;
            RefreshEliteBar();
        }
    }

    public void Bind(
        RunController controller,
        PlayerHealth health,
        PlayerProgression progression,
        CatalyzeAbility catalyze,
        ElementSystem elements,
        SpellSystem spells,
        RunEventSystem events,
        EnemySystem enemies,
        BossController boss)
    {
        _controller = controller;
        _health = health;
        _progression = progression;
        _catalyze = catalyze;
        _elements = elements;
        _spells = spells;
        _events = events;
        _enemies = enemies;
        _boss = boss;
        _controller.StateChanged += OnStateChanged;
        _health.HealthChanged += OnHealthChanged;
        _progression.ExperienceChanged += OnExperienceChanged;
        _elements.ReactionTriggered += OnReactionTriggered;
        _spells.LoadoutChanged += RefreshSpellLoadout;
        _events.ObjectiveChanged += OnObjectiveChanged;
        _boss.StaggerChanged += OnBossStaggerChanged;
        _seedLabel.Text = $"Seed: {_controller.RunSeed}";
        OnHealthChanged(_health.CurrentHealth, _health.MaxHealth);
        OnExperienceChanged(
            _progression.Level,
            _progression.Experience,
            _progression.ExperienceRequired);
        OnStateChanged((int)_controller.State);
        RefreshSpellLoadout();
    }

    public override void _ExitTree()
    {
        if (_controller is not null)
        {
            _controller.StateChanged -= OnStateChanged;
        }

        if (_health is not null)
        {
            _health.HealthChanged -= OnHealthChanged;
        }

        if (_progression is not null)
        {
            _progression.ExperienceChanged -= OnExperienceChanged;
        }

        if (_elements is not null)
        {
            _elements.ReactionTriggered -= OnReactionTriggered;
        }
        if (_spells is not null)
        {
            _spells.LoadoutChanged -= RefreshSpellLoadout;
        }
        if (_events is not null)
        {
            _events.ObjectiveChanged -= OnObjectiveChanged;
        }
        if (_boss is not null)
        {
            _boss.StaggerChanged -= OnBossStaggerChanged;
        }
    }

    private void OnStateChanged(int stateValue)
    {
        RunState state = (RunState)stateValue;
        _stateLabel.Text = state.ToString();
        _pausePanel.Visible = state == RunState.Paused;
        _pauseTitle.Text = "暂停";
        _resumeButton.Visible = true;
        if (_pausePanel.Visible)
        {
            _resumeButton.GrabFocus();
        }
    }

    private void OnHealthChanged(float current, float maximum)
    {
        _healthBar.MaxValue = maximum;
        _healthBar.Value = current;
        _healthLabel.Text = $"生命 {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}";
    }

    private void OnExperienceChanged(int level, int experience, int required)
    {
        _levelLabel.Text = $"等级 {level}";
        _experienceBar.MaxValue = required;
        _experienceBar.Value = experience;
    }

    private void OnReactionTriggered(ReactionKind kind, Vector2 position, float damage)
    {
        bool assist = GetNode<SettingsService>("/root/SettingsService").Current.ElementIconAssist;
        _reactionLabel.Text = kind switch
        {
            ReactionKind.SteamShock => $"{(assist ? "[火+水] " : string.Empty)}蒸汽热冲击  {damage:0}",
            ReactionKind.Conduction => $"{(assist ? "[水+雷] " : string.Empty)}传导闪电  {damage:0}",
            _ => string.Empty
        };
        _reactionMessageRemaining = 1.4;
        _reactionLabel.Visible = true;
    }

    private void RefreshSpellLoadout()
    {
        if (_spells is null)
        {
            return;
        }
        _spellLabel.Text = string.Join("  ·  ", _spells.Loadout.Select(runtime =>
        {
            string branch = runtime.SelectedBranch == SpellBranch.None
                ? string.Empty
                : runtime.SelectedBranch.ToString();
            return $"{runtime.Definition.DisplayName} {runtime.Level}{branch}";
        }));
    }

    private void OnObjectiveChanged(string text, float progress, bool active)
    {
        _objectiveLabel.Text = text;
        _objectiveLabel.Visible = !string.IsNullOrEmpty(text);
        _objectiveBar.Visible = active;
        _objectiveBar.Value = progress * 100.0f;
    }

    private void RefreshEliteBar()
    {
        if (_enemies is null || !_enemies.TryGetPriorityElite(out EnemyState elite))
        {
            _eliteLabel.Visible = false;
            _eliteBar.Visible = false;
            _bossStaggerBar.Visible = false;
            return;
        }
        _eliteLabel.Visible = true;
        _eliteBar.Visible = true;
        _eliteLabel.Text = elite.Archetype switch
        {
            EnemyArchetype.EliteCharger => "冲锋精英",
            EnemyArchetype.BossAshenColossus => "灰烬巨像 · 元素反应可使其失衡",
            _ => "元素守卫"
        };
        _eliteBar.MaxValue = elite.MaxHealth;
        _eliteBar.Value = elite.Health;
        _bossStaggerBar.Visible = elite.Archetype == EnemyArchetype.BossAshenColossus;
        if (_bossStaggerBar.Visible && _boss is not null)
        {
            _bossStaggerBar.MaxValue = _boss.StaggerThreshold;
            _bossStaggerBar.Value = _boss.Stagger;
        }
    }

    private void OnBossStaggerChanged(float current, float maximum)
    {
        _bossStaggerBar.MaxValue = maximum;
        _bossStaggerBar.Value = current;
    }

    private void RefreshInputHint()
    {
        _inputHint.Text = _usingGamepad
            ? "左摇杆移动  ·  A 闪避  ·  RT 元素催化  ·  菜单键暂停"
            : "WASD 移动  ·  Space 闪避  ·  鼠标右键元素催化  ·  Esc 暂停";
    }
}
