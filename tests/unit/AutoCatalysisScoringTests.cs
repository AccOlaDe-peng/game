using Catalyst.Elements;
using Catalyst.Passives;
using NUnit.Framework;

namespace Catalyst.Tests.Unit;

public sealed class AutoCatalysisScoringTests
{
    private static CatalysisCandidateInput Input(
        int targets = 2,
        float damage = 4f,
        int elites = 0,
        int bosses = 0,
        float distance = 1f,
        int kinds = 1,
        uint id = 1) =>
        new(targets, damage, elites, bosses, distance, kinds, id);

    [Test]
    public void ScoreFollowsDocumentedFormula()
    {
        CatalysisCandidateInput input = Input(targets: 3, damage: 10f, elites: 1, bosses: 0, distance: 2f);
        float score = CatalysisScoring.Evaluate(input,
            reactionCountWeight: 1.0f,
            expectedDamageWeight: 0.02f,
            eliteScoreBonus: 1.5f,
            bossScoreBonus: 3.0f,
            distancePenaltyWeight: 0.04f,
            diversityBonusEnabled: false);
        Assert.That(score, Is.EqualTo(3f * 1.0f + 10f * 0.02f + 1.5f - 2f * 0.04f).Within(1e-4));
    }

    [Test]
    public void DiversityBonusIsCapped()
    {
        CatalysisCandidateInput input = Input(kinds: 5);
        float score = CatalysisScoring.Evaluate(input, 1f, 0.02f, 1.5f, 3f, 0.04f, true);
        Assert.That(score, Is.EqualTo(CatalysisScoring.Evaluate(input, 1f, 0.02f, 1.5f, 3f, 0.04f, true)));
        float withBonus = score - CatalysisScoring.Evaluate(input, 1f, 0.02f, 1.5f, 3f, 0.04f, false);
        Assert.That(withBonus, Is.EqualTo(CatalysisScoring.DiversityBonusCap).Within(1e-4));
    }

    [Test]
    public void EqualScoresResolveByDeterministicTieBreak()
    {
        CatalysisCandidateInput moreTargets = Input(targets: 4, id: 9);
        CatalysisCandidateInput fewerTargets = Input(targets: 2, id: 1);
        float score = CatalysisScoring.Evaluate(moreTargets, 1f, 0.02f, 1.5f, 3f, 0.04f, false);
        float score2 = CatalysisScoring.Evaluate(fewerTargets, 1f, 0.02f, 1.5f, 3f, 0.04f, false);
        Assert.That(CatalysisScoring.PreferLeft(moreTargets, score, fewerTargets, score2), Is.True);
        Assert.That(CatalysisScoring.PreferLeft(fewerTargets, score2, moreTargets, score), Is.False);
    }

    [Test]
    public void FullyEqualCandidatesPreferSmallerEntityId()
    {
        CatalysisCandidateInput smallId = Input(id: 1);
        CatalysisCandidateInput largeId = Input(id: 2);
        float score = CatalysisScoring.Evaluate(smallId, 1f, 0.02f, 1.5f, 3f, 0.04f, false);
        Assert.That(CatalysisScoring.PreferLeft(smallId, score, largeId, score), Is.True);
        Assert.That(CatalysisScoring.PreferLeft(largeId, score, smallId, score), Is.False);
    }

    [Test]
    public void PreviewNeverReactsOnEmptyStates()
    {
        Assert.That(ElementSystem.TryPreviewCatalysis(default).CanReact, Is.False);
        ElementRuntimeState waterOnly = default;
        waterOnly.Water = new ElementState { Stacks = 2, RemainingDuration = 2f };
        Assert.That(ElementSystem.TryPreviewCatalysis(waterOnly).CanReact, Is.False);

        ElementRuntimeState steam = default;
        steam.Water = new ElementState { Stacks = 2, RemainingDuration = 2f };
        steam.Fire = new ElementState { Stacks = 2, RemainingDuration = 2f };
        ReactionPreview preview = ElementSystem.TryPreviewCatalysis(steam);
        Assert.Multiple(() =>
        {
            Assert.That(preview.CanReact, Is.True);
            Assert.That(preview.Reaction, Is.EqualTo(ReactionKind.SteamShock));
        });
    }
}
