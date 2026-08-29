namespace Catalyst.Passives;

/// <summary>Deterministic inputs for one catalysis candidate center.</summary>
public readonly record struct CatalysisCandidateInput(
    int ReactiveTargetCount,
    float ExpectedDamage,
    int EliteCount,
    int BossCount,
    float DistanceToPlayer,
    int DistinctReactionKinds,
    uint CandidateEntityId);

/// <summary>Pure, deterministic catalysis scoring and tie-breaking.</summary>
public static class CatalysisScoring
{
    public const float DiversityBonusPerKind = 0.5f;
    public const float DiversityBonusCap = 1.5f;

    public static float Evaluate(
        in CatalysisCandidateInput input,
        float reactionCountWeight,
        float expectedDamageWeight,
        float eliteScoreBonus,
        float bossScoreBonus,
        float distancePenaltyWeight,
        bool diversityBonusEnabled)
    {
        float score =
            input.ReactiveTargetCount * reactionCountWeight +
            input.ExpectedDamage * expectedDamageWeight +
            input.EliteCount * eliteScoreBonus +
            input.BossCount * bossScoreBonus -
            input.DistanceToPlayer * distancePenaltyWeight;
        if (diversityBonusEnabled)
        {
            score += Math.Min(DiversityBonusCap, input.DistinctReactionKinds * DiversityBonusPerKind);
        }
        return score;
    }

    /// <summary>
    /// Deterministic tie-break: more reactive targets, contains boss, more elites,
    /// closer to player, smaller entity id. Never uses randomness.
    /// </summary>
    /// <returns>True when <paramref name="left"/> wins against <paramref name="right"/>.</returns>
    public static bool PreferLeft(
        in CatalysisCandidateInput left,
        float leftScore,
        in CatalysisCandidateInput right,
        float rightScore)
    {
        if (!leftScore.Equals(rightScore))
        {
            return leftScore > rightScore;
        }
        if (left.ReactiveTargetCount != right.ReactiveTargetCount)
        {
            return left.ReactiveTargetCount > right.ReactiveTargetCount;
        }
        if (left.BossCount != right.BossCount)
        {
            return left.BossCount > right.BossCount;
        }
        if (left.EliteCount != right.EliteCount)
        {
            return left.EliteCount > right.EliteCount;
        }
        if (!left.DistanceToPlayer.Equals(right.DistanceToPlayer))
        {
            return left.DistanceToPlayer < right.DistanceToPlayer;
        }
        return left.CandidateEntityId < right.CandidateEntityId;
    }
}
