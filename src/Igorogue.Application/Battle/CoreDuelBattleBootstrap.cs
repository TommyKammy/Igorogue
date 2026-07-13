using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

using Igorogue.Application.Replay;
using Igorogue.Domain.Cards;
using Igorogue.Domain.Content;
using Igorogue.Domain.Determinism;

namespace Igorogue.Application.Battle;

/// <summary>
/// Immutable composition input for one restartable Core Duel. Content parsing stays
/// outside Application; this type binds the already-typed catalog to the exact
/// authoritative initial snapshot and replay identity.
/// </summary>
public sealed class CoreDuelBattleBootstrap
{
    public const string EncodingVersion = "core-duel-battle-bootstrap-v1";

    private readonly ReadOnlyCollection<BattleCardInstance> physicalDeckRecipeView;

    private CoreDuelBattleBootstrap(
        BattleAuthoritativeInitialSnapshot initialSnapshot,
        CoreDuelContentCatalog catalog,
        ReplayMetadata metadata,
        BattleCardInstance[] physicalDeckRecipe,
        StarterStoneCardPlayCatalog stoneDefinitions,
        StarterReinforceCardPlayDefinition reinforceDefinition,
        StarterDevelopmentCardPlayDefinition developmentDefinition)
    {
        InitialSnapshot = initialSnapshot;
        Catalog = catalog;
        Metadata = metadata;
        physicalDeckRecipeView = Array.AsReadOnly(physicalDeckRecipe);
        StoneDefinitions = stoneDefinitions;
        ReinforceDefinition = reinforceDefinition;
        DevelopmentDefinition = developmentDefinition;
        CanonicalText = CreateCanonicalText();
        Checksum = DeterministicChecksum.Sha256Hex(CanonicalText);
    }

    public BattleAuthoritativeInitialSnapshot InitialSnapshot { get; }

    public CoreDuelContentCatalog Catalog { get; }

    public ReplayMetadata Metadata { get; }

    public IReadOnlyList<BattleCardInstance> PhysicalDeckRecipe => physicalDeckRecipeView;

    public StarterStoneCardPlayCatalog StoneDefinitions { get; }

    public StarterReinforceCardPlayDefinition ReinforceDefinition { get; }

    public StarterDevelopmentCardPlayDefinition DevelopmentDefinition { get; }

    public string CanonicalText { get; }

    public string Checksum { get; }

    public static CoreDuelBattleBootstrap Create(
        BattleAuthoritativeInitialSnapshot initialSnapshot,
        CoreDuelContentCatalog catalog,
        ReplayMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(initialSnapshot);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(metadata);
        if (!StringComparer.Ordinal.Equals(catalog.ContentHash, metadata.ContentHash))
        {
            throw new ArgumentException(
                "Core Duel catalog content hash must match replay metadata.",
                nameof(catalog));
        }

        if (initialSnapshot.PlayerTurnIndex != 1)
        {
            throw new ArgumentException(
                "A restartable Core Duel bootstrap must begin at player turn one.",
                nameof(initialSnapshot));
        }

        if (initialSnapshot.ClosedWindowResources.DeferredPlayerChoices.Count != 0)
        {
            throw new ArgumentException(
                "A fresh Core Duel cannot begin with unresolved deferred player choices.",
                nameof(initialSnapshot));
        }

        var physicalRecipe = ExpandPhysicalRecipe(catalog.StartingDeck);
        var stoneDefinitions = StarterStoneCardPlayCatalog.FromCoreDuelCatalog(catalog);
        var reinforceDefinition =
            StarterReinforceCardPlayDefinition.FromCoreDuelCatalog(catalog);
        var developmentDefinition =
            StarterDevelopmentCardPlayDefinition.FromCoreDuelCatalog(catalog);
        return new CoreDuelBattleBootstrap(
            initialSnapshot,
            catalog,
            metadata,
            physicalRecipe,
            stoneDefinitions,
            reinforceDefinition,
            developmentDefinition);
    }

    public string ToCanonicalText() => CanonicalText;

    private static BattleCardInstance[] ExpandPhysicalRecipe(StartingDeckRecipe recipe)
    {
        var physical = new List<BattleCardInstance>(recipe.TotalCards);
        foreach (var entry in recipe.Entries)
        {
            for (var ordinal = 1; ordinal <= entry.Count; ordinal++)
            {
                physical.Add(new BattleCardInstance(
                    $"deck.{recipe.Id}.{entry.CardId}." +
                    ordinal.ToString("D4", CultureInfo.InvariantCulture),
                    entry.CardId));
            }
        }

        if (physical.Count != recipe.TotalCards)
        {
            throw new InvalidOperationException(
                "Expanded starting-deck size does not match its typed recipe total.");
        }

        return physical.ToArray();
    }

    private string CreateCanonicalText()
    {
        var lines = new List<string>
        {
            EncodingVersion,
            $"metadata={EncodeStableText(Metadata.ToCanonicalText())}",
            $"initial_snapshot={EncodeStableText(InitialSnapshot.ToCanonicalText())}",
            $"content_hash={Catalog.ContentHash}",
            $"starting_deck={EncodeStableText(Catalog.StartingDeck.ToCanonicalText())}",
            $"system_base_qi={Catalog.SystemPolicy.BaseQi.ToString(CultureInfo.InvariantCulture)}",
            $"system_base_draw={Catalog.SystemPolicy.BaseDraw.ToString(CultureInfo.InvariantCulture)}",
            $"bandit={EncodeStableText(Catalog.Bandit.ToCanonicalText())}",
            $"stone_definitions={EncodeStableText(StoneDefinitions.ToCanonicalText())}",
            $"reinforce_definition={EncodeStableText(ReinforceDefinition.ToCanonicalText())}",
            $"development_definition={EncodeStableText(DevelopmentDefinition.ToCanonicalText())}",
        };
        lines.Add(
            $"physical_card_count={physicalDeckRecipeView.Count.ToString(CultureInfo.InvariantCulture)}");
        foreach (var card in physicalDeckRecipeView)
        {
            lines.Add(
                $"physical_card={EncodeStableText(card.InstanceId)}:{EncodeStableText(card.ContentId)}");
        }

        return string.Join('\n', lines);
    }

    private static string EncodeStableText(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
}
