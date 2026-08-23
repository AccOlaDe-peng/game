using Catalyst.App;
using Catalyst.Enemies;
using Godot;

namespace Catalyst.Presentation;

public partial class DamageNumberPresentationSystem : Node3D
{
    private const int Capacity = 32;
    private readonly Label3D[] _labels = new Label3D[Capacity];
    private readonly float[] _ages = new float[Capacity];
    private readonly Vector3[] _origins = new Vector3[Capacity];
    private EnemySystem _enemies = null!;
    private SettingsService _settings = null!;
    private int _next;

    public override void _Ready()
    {
        _enemies = GetNode<EnemySystem>("../../../SimulationRoot/EntitySystem");
        _settings = GetNode<SettingsService>("/root/SettingsService");
        for (int index = 0; index < Capacity; index++)
        {
            Label3D label = new()
            {
                Visible = false,
                FontSize = 34,
                OutlineSize = 8,
                Modulate = new Color(1.0f, 0.86f, 0.36f),
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                NoDepthTest = true
            };
            AddChild(label);
            _labels[index] = label;
            _ages[index] = 1.0f;
        }
        _enemies.DamageApplied += OnDamageApplied;
    }

    public override void _Process(double deltaValue)
    {
        float delta = (float)deltaValue;
        for (int index = 0; index < Capacity; index++)
        {
            if (_ages[index] >= 0.75f)
            {
                _labels[index].Visible = false;
                continue;
            }
            _ages[index] += delta;
            float progress = _ages[index] / 0.75f;
            _labels[index].Position = _origins[index] + Vector3.Up * progress * 1.5f;
            _labels[index].Modulate = new Color(1.0f, 0.86f, 0.36f, 1.0f - progress);
        }
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_enemies))
        {
            _enemies.DamageApplied -= OnDamageApplied;
        }
    }

    private void OnDamageApplied(Vector2 position, float amount)
    {
        if (!_settings.Current.ShowDamageNumbers || amount <= 0.0f)
        {
            return;
        }
        int index = _next++ % Capacity;
        _ages[index] = 0.0f;
        _origins[index] = new Vector3(position.X, 1.4f, position.Y);
        _labels[index].Text = Mathf.RoundToInt(amount).ToString();
        _labels[index].Position = _origins[index];
        _labels[index].Visible = true;
    }
}
