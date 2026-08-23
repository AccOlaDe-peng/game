using Godot;

namespace Catalyst.Player;

public partial class PlayerProgression : Node
{
    public event Action<int>? LevelGained;
    public event Action<int, int, int>? ExperienceChanged;

    public int Level { get; private set; } = 1;
    public int Experience { get; private set; }
    public int ExperienceRequired { get; private set; } = GetExperienceRequired(1);

    public void AddExperience(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Experience += amount;
        while (Experience >= ExperienceRequired)
        {
            Experience -= ExperienceRequired;
            Level++;
            ExperienceRequired = GetExperienceRequired(Level);
            LevelGained?.Invoke(Level);
        }

        ExperienceChanged?.Invoke(Level, Experience, ExperienceRequired);
    }

    public static int GetExperienceRequired(int level)
    {
        int safeLevel = Math.Max(1, level);
        return 6 + (safeLevel - 1) * 4;
    }
}
