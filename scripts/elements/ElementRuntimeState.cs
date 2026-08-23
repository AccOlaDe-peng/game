namespace Catalyst.Elements;

public struct ElementRuntimeState
{
    public ElementState Fire;
    public ElementState Frost;
    public ElementState Lightning;
    public ElementState Water;
    public ElementState Wind;
    public ElementState Earth;
    public ElementState Mark;
    public float ThermalCooldown;
    public float PlasmaCooldown;
    public float ConductiveCooldown;
    public float FrozenRemaining;
    public float StaggerRemaining;
    public float SteamScaldRemaining;
}
