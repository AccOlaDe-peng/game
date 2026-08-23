namespace Catalyst.Spells;

public static class WeaponFieldRules
{
    public const float Duration = 2.4f;
    public const float PulseInterval = 0.45f;
    public const int MaximumFields = 16;

    public static int MaximumPulses => (int)MathF.Ceiling(Duration / PulseInterval);
}
