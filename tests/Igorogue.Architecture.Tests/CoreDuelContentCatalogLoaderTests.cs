using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Igorogue.Content;
using Igorogue.Domain.Cards;
using Igorogue.Domain.Content;

namespace Igorogue.Architecture.Tests;

public sealed class CoreDuelContentCatalogLoaderTests
{
    private const string ExpectedContentHash =
        "sha256:cd53980e2edd69ad14b3815c800a3c5aab119f21d95d724d083afa2920c15ad6";

    [Fact]
    public void RealGeneratedSnapshotProjectsTheExactCoreDuelCatalog()
    {
        var root = FindRepositoryRoot();
        var manifestPath = Path.Combine(
            root.FullName,
            "build",
            "generated_content",
            "content_manifest.json");

        var catalog = new CoreDuelContentCatalogLoader().Load(manifestPath);

        Assert.Equal(ExpectedContentHash, catalog.ContentHash);
        Assert.Equal(3, catalog.SystemPolicy.BaseQi);
        Assert.Equal(5, catalog.SystemPolicy.BaseDraw);
        Assert.Equal(
            [
                "card_basic_stone",
                "card_contact",
                "card_development",
                "card_extend",
                "card_lure_stone",
                "card_reinforce",
            ],
            catalog.StarterCards.Select(card => card.Id));
        Assert.Equal(
            Enum.GetValues<CardOperationKind>(),
            catalog.StarterCards
                .SelectMany(card => card.Effects.Concat(card.OnCaptured))
                .Select(operation => operation.Kind)
                .Distinct()
                .Order());

        var starterStones = StarterStoneCardPlayCatalog.FromCoreDuelCatalog(catalog);
        Assert.Equal(
            [
                StarterStoneCardProfile.BasicPlacement,
                StarterStoneCardProfile.Extend,
                StarterStoneCardProfile.Contact,
                StarterStoneCardProfile.Lure,
            ],
            starterStones.Definitions
                .Select(definition => definition.Profile)
                .Order());
        Assert.Equal(
            catalog.StarterCards
                .Where(card => card.Type == CardContentType.Stone)
                .Select(card => card.Id),
            starterStones.Definitions.Select(definition => definition.ContentId));

        var extend = catalog.StarterCard("card_extend");
        Assert.Equal(
            [CardOperationKind.PlaceStone, CardOperationKind.DrawIfRealLibertiesAtLeast],
            extend.Effects.Select(operation => operation.Kind));
        var extendDraw = Assert.IsType<DrawIfRealLibertiesAtLeastOperationDefinition>(
            extend.Effects[1]);
        Assert.Equal(3, extendDraw.MinimumRealLiberties);
        Assert.Equal(1, extendDraw.Cards);

        var lure = catalog.StarterCard("card_lure_stone");
        Assert.Equal(
            [CardOperationKind.PlaceStone, CardOperationKind.ReserveDraw],
            lure.Effects.Select(operation => operation.Kind));
        var capturedDraw = Assert.IsType<ReserveDrawOperationDefinition>(
            Assert.Single(lure.OnCaptured));
        Assert.Equal(2, capturedDraw.Cards);

        var development = catalog.StarterCard("card_development");
        var build = Assert.IsType<BuildFacilityOperationDefinition>(
            Assert.Single(development.Effects));
        Assert.Equal("development", build.FacilityContentId);

        var reinforce = catalog.StarterCard("card_reinforce");
        Assert.Equal(
            [CardOperationKind.DrawIfTargetAtari, CardOperationKind.TemporaryLiberty],
            reinforce.Effects.Select(operation => operation.Kind));

        Assert.Equal("enemy_bandit", catalog.Bandit.Id);
        Assert.Equal("FEAT-009", catalog.Bandit.BehaviorSpec);
        Assert.Equal("1.0.0", catalog.Bandit.BehaviorVersion);
        Assert.Equal(1, catalog.Bandit.ActionBudget.NormalActions);
        Assert.Equal(1, catalog.Bandit.ActionBudget.CounterattackBonusActions);
        Assert.Equal(2, catalog.Bandit.ActionBudget.MaxActionsPerEnemyTurn);
        Assert.Equal(2, catalog.Bandit.Parameters.DefenseThreshold);
        Assert.Equal(1, catalog.Bandit.Parameters.OpportunisticCaptureMinStones);
        Assert.Equal(
            [EnemyIntentKind.CaptureBlackKing, EnemyIntentKind.DefendWhiteKing],
            catalog.Bandit.MandatoryOverrides);
        Assert.Equal(
            [
                EnemyIntentKind.CaptureNonKing,
                EnemyIntentKind.PressureBlackKing,
                EnemyIntentKind.AdvanceTowardBlackKing,
            ],
            catalog.Bandit.PlanPriority);
        Assert.Equal(catalog.Bandit.PlanPriority, catalog.Bandit.CounterattackPriority);
        Assert.Equal(EnemyTieBreak.CanonicalYThenX, catalog.Bandit.TieBreak);
        Assert.Equal(
            Enum.GetValues<EnemyIntentKind>(),
            catalog.Bandit.Intents.Select(intent => intent.Kind));

        Assert.DoesNotContain(
            typeof(CoreDuelContentCatalog).GetProperties(),
            property =>
                property.Name.Contains("Deck", StringComparison.Ordinal) ||
                property.Name.Contains("Recipe", StringComparison.Ordinal));
    }

    [Fact]
    public void CatalogCollectionsAreReadOnlyViews()
    {
        var root = FindRepositoryRoot();
        var catalog = new CoreDuelContentCatalogLoader().Load(Path.Combine(
            root.FullName,
            "build",
            "generated_content",
            "content_manifest.json"));

        Assert.Throws<NotSupportedException>(
            () => ((IList<CardContentDefinition>)catalog.StarterCards).Clear());
        Assert.Throws<NotSupportedException>(
            () => ((IList<CardOperationDefinition>)catalog.StarterCards[0].Effects).Clear());
        Assert.Throws<NotSupportedException>(
            () => ((IList<EnemyIntentDefinition>)catalog.Bandit.Intents).Clear());
    }

    [Fact]
    public void DefinitionEnumerationReversalProducesTheSameCanonicalProjection()
    {
        using var firstFixture = GeneratedContentFixture.Create();
        using var reversedFixture = GeneratedContentFixture.Create();
        reversedFixture.MutateJson(
            "content/cards.json",
            root => ReverseArray(root.AsArray()));
        reversedFixture.MutateJson(
            "content/enemies.json",
            root => ReverseArray(Bandit(root)["intents"]!.AsArray()));

        var loader = new CoreDuelContentCatalogLoader();
        var first = loader.Load(firstFixture.ManifestPath);
        var reversed = loader.Load(reversedFixture.ManifestPath);

        Assert.NotEqual(first.ContentHash, reversed.ContentHash);
        Assert.Equal(ProjectionFingerprint(first), ProjectionFingerprint(reversed));
    }

    [Fact]
    public void DuplicateCardIdIsRejected()
    {
        using var fixture = GeneratedContentFixture.Create();
        fixture.MutateJson(
            "content/cards.json",
            root => root.AsArray().Add(root.AsArray()[0]!.DeepClone()));

        Assert.Throws<InvalidDataException>(() => Load(fixture));
    }

    [Fact]
    public void DuplicateEnemyIdIsRejected()
    {
        using var fixture = GeneratedContentFixture.Create();
        fixture.MutateJson(
            "content/enemies.json",
            root => root.AsArray().Add(Bandit(root).DeepClone()));

        Assert.Throws<InvalidDataException>(() => Load(fixture));
    }

    [Fact]
    public void DuplicateIntentIdIsRejected()
    {
        using var fixture = GeneratedContentFixture.Create();
        fixture.MutateJson(
            "content/enemies.json",
            root =>
            {
                var intents = Bandit(root)["intents"]!.AsArray();
                intents.Add(intents[0]!.DeepClone());
            });

        Assert.Throws<InvalidDataException>(() => Load(fixture));
    }

    [Fact]
    public void InvalidStableCardIdIsRejected()
    {
        using var fixture = GeneratedContentFixture.Create();
        fixture.MutateJson(
            "content/cards.json",
            root => root.AsArray()[0]!["id"] = "card invalid");

        Assert.Throws<InvalidDataException>(() => Load(fixture));
    }

    [Fact]
    public void UnknownStarterCandidateIdIsRejected()
    {
        using var fixture = GeneratedContentFixture.Create();
        fixture.MutateJson(
            "content/cards.json",
            root => StarterCard(root, "card_basic_stone")["id"] =
                "card_unapproved_starter");

        Assert.Throws<InvalidDataException>(() => Load(fixture));
    }

    [Theory]
    [InlineData("card_basic_stone")]
    [InlineData("card_extend")]
    [InlineData("card_contact")]
    [InlineData("card_lure_stone")]
    public void MissingStoneStarterPlacementTagsAreRejected(string cardId)
    {
        using var fixture = GeneratedContentFixture.Create();
        fixture.MutateJson(
            "content/cards.json",
            root => StarterCard(root, cardId).Remove("placement_tags"));

        Assert.Throws<InvalidDataException>(() => Load(fixture));
    }

    [Fact]
    public void EmptyStoneStarterPlacementTagsAreRejected()
    {
        using var fixture = GeneratedContentFixture.Create();
        fixture.MutateJson(
            "content/cards.json",
            root => StarterCard(root, "card_basic_stone")["placement_tags"] =
                new JsonArray());

        Assert.Throws<InvalidDataException>(() => Load(fixture));
    }

    [Theory]
    [InlineData("card_reinforce")]
    [InlineData("card_development")]
    public void MissingTargetedStarterTargetIsRejected(string cardId)
    {
        using var fixture = GeneratedContentFixture.Create();
        fixture.MutateJson(
            "content/cards.json",
            root => StarterCard(root, cardId).Remove("target"));

        Assert.Throws<InvalidDataException>(() => Load(fixture));
    }

    [Fact]
    public void ReversedReinforceOperationsAreRejected()
    {
        using var fixture = GeneratedContentFixture.Create();
        fixture.MutateJson(
            "content/cards.json",
            root => ReverseArray(
                StarterCard(root, "card_reinforce")["effects"]!.AsArray()));

        Assert.Throws<InvalidDataException>(() => Load(fixture));
    }

    [Fact]
    public void MissingRequiredGeneratedFileIsRejected()
    {
        using var fixture = GeneratedContentFixture.Create();
        fixture.RemoveGeneratedFile("content/cards.json");

        Assert.Throws<InvalidDataException>(() => Load(fixture));
    }

    [Fact]
    public void TamperedRequiredGeneratedFileIsRejected()
    {
        using var fixture = GeneratedContentFixture.Create();
        fixture.TamperWithoutRefreshingManifest("content/cards.json", "[]\n");

        Assert.Throws<InvalidDataException>(() => Load(fixture));
    }

    [Fact]
    public void TypedCatalogParsesTheExactBytesAuthenticatedByTheSnapshot()
    {
        using var fixture = GeneratedContentFixture.Create();
        var snapshot = new ContentManifestLoader().Load(fixture.ManifestPath);
        fixture.TamperWithoutRefreshingManifest("content/cards.json", "[]\n");

        var catalog = new CoreDuelContentCatalogLoader().Load(snapshot);

        Assert.Equal(ExpectedContentHash, catalog.ContentHash);
        Assert.Equal(6, catalog.StarterCards.Count);
    }

    [Fact]
    public void UnknownStarterOperationIsRejected()
    {
        using var fixture = GeneratedContentFixture.Create();
        fixture.MutateJson(
            "content/cards.json",
            root => StarterCard(root, "card_basic_stone")["effects"]![0]!["op"] =
                "unsupported_operation");

        Assert.Throws<InvalidDataException>(() => Load(fixture));
    }

    [Fact]
    public void UnsupportedStarterOperationShapeIsRejected()
    {
        using var fixture = GeneratedContentFixture.Create();
        fixture.MutateJson(
            "content/cards.json",
            root => StarterCard(root, "card_basic_stone")["effects"]![0]!["extra"] = 1);

        Assert.Throws<InvalidDataException>(() => Load(fixture));
    }

    [Fact]
    public void DanglingFacilityReferenceIsRejected()
    {
        using var fixture = GeneratedContentFixture.Create();
        fixture.MutateJson(
            "content/cards.json",
            root => StarterCard(root, "card_development")["effects"]![0]!["facility"] =
                "unknown_facility");

        Assert.Throws<InvalidDataException>(() => Load(fixture));
    }

    [Fact]
    public void DefaultFacilityPolicyKeyIsNotABuildableContentReference()
    {
        using var fixture = GeneratedContentFixture.Create();
        fixture.MutateJson(
            "content/cards.json",
            root => StarterCard(root, "card_development")["effects"]![0]!["facility"] =
                "default");

        Assert.Throws<InvalidDataException>(() => Load(fixture));
    }

    [Theory]
    [InlineData("base_qi")]
    [InlineData("base_draw")]
    public void InvalidCoreDuelSystemPolicyIsRejected(string propertyName)
    {
        using var fixture = GeneratedContentFixture.Create();
        fixture.MutateJson(
            "balance/system.json",
            root => root[propertyName] = 0);

        Assert.Throws<InvalidDataException>(() => Load(fixture));
    }

    [Fact]
    public void UnknownBanditPlacementModeIsRejected()
    {
        using var fixture = GeneratedContentFixture.Create();
        fixture.MutateJson(
            "content/enemies.json",
            root => Bandit(root)["placement_permissions"]!.AsArray().Add("white_unknown"));

        Assert.Throws<InvalidDataException>(() => Load(fixture));
    }

    [Fact]
    public void DuplicateBanditPriorityEntryIsRejected()
    {
        using var fixture = GeneratedContentFixture.Create();
        fixture.MutateJson(
            "content/enemies.json",
            root =>
            {
                var priority = Bandit(root)["plan_priority"]!.AsArray();
                priority.Add(priority[0]!.DeepClone());
            });

        Assert.Throws<InvalidDataException>(() => Load(fixture));
    }

    [Theory]
    [InlineData("plan_priority")]
    [InlineData("counterattack_priority")]
    public void MandatoryBanditOverrideCannotAlsoAppearInPriority(string priorityName)
    {
        using var fixture = GeneratedContentFixture.Create();
        fixture.MutateJson(
            "content/enemies.json",
            root => Bandit(root)[priorityName]!.AsArray().Add("capture_black_king"));

        Assert.Throws<InvalidDataException>(() => Load(fixture));
    }

    [Fact]
    public void DanglingBanditIntentReferenceIsRejected()
    {
        using var fixture = GeneratedContentFixture.Create();
        fixture.MutateJson(
            "content/enemies.json",
            root =>
            {
                var intents = Bandit(root)["intents"]!.AsArray();
                var advance = intents.Single(node =>
                    string.Equals(
                        node!["id"]!.GetValue<string>(),
                        "advance_toward_black_king",
                        StringComparison.Ordinal));
                intents.Remove(advance);
            });

        Assert.Throws<InvalidDataException>(() => Load(fixture));
    }

    [Fact]
    public void CyclicBanditFallbackIsRejected()
    {
        using var fixture = GeneratedContentFixture.Create();
        fixture.MutateJson(
            "content/enemies.json",
            root =>
            {
                var advance = Bandit(root)["intents"]!.AsArray()
                    .Select(node => node!.AsObject())
                    .Single(intent => string.Equals(
                        intent["id"]!.GetValue<string>(),
                        "advance_toward_black_king",
                        StringComparison.Ordinal));
                advance["fallback"]!.AsArray().Add("capture_non_king");
            });

        Assert.Throws<InvalidDataException>(() => Load(fixture));
    }

    [Fact]
    public void InvalidBanditTieBreakIsRejected()
    {
        using var fixture = GeneratedContentFixture.Create();
        fixture.MutateJson(
            "content/enemies.json",
            root => Bandit(root)["tie_break"] = "source_order");

        Assert.Throws<InvalidDataException>(() => Load(fixture));
    }

    private static CoreDuelContentCatalog Load(GeneratedContentFixture fixture) =>
        new CoreDuelContentCatalogLoader().Load(fixture.ManifestPath);

    private static JsonObject StarterCard(JsonNode root, string id) => root.AsArray()
        .Select(node => node!.AsObject())
        .Single(card => string.Equals(card["id"]!.GetValue<string>(), id, StringComparison.Ordinal));

    private static JsonObject Bandit(JsonNode root) => root.AsArray()
        .Select(node => node!.AsObject())
        .Single(enemy => string.Equals(
            enemy["id"]!.GetValue<string>(),
            "enemy_bandit",
            StringComparison.Ordinal));

    private static void ReverseArray(JsonArray array)
    {
        var reversed = array.Select(node => node!.DeepClone()).Reverse().ToArray();
        array.Clear();
        foreach (var node in reversed)
        {
            array.Add(node);
        }
    }

    private static string ProjectionFingerprint(CoreDuelContentCatalog catalog)
    {
        var builder = new StringBuilder();
        builder.Append(catalog.SystemPolicy.BaseQi).Append('/')
            .Append(catalog.SystemPolicy.BaseDraw).AppendLine();
        foreach (var card in catalog.StarterCards)
        {
            builder.Append(card.Id).Append('|')
                .Append(card.Rarity).Append('|')
                .Append(card.Cost).Append('|')
                .Append(card.Type).Append('|')
                .Append(card.Target).Append('|')
                .AppendJoin(',', card.PlacementTags).Append('|');
            AppendOperations(builder, card.Effects);
            builder.Append('|');
            AppendOperations(builder, card.OnCaptured);
            builder.AppendLine();
        }

        var bandit = catalog.Bandit;
        builder.Append(bandit.Id).Append('|')
            .Append(bandit.BehaviorSpec).Append('|')
            .Append(bandit.BehaviorVersion).Append('|')
            .Append(bandit.ActionBudget.NormalActions).Append('|')
            .Append(bandit.ActionBudget.CounterattackBonusActions).Append('|')
            .Append(bandit.ActionBudget.MaxActionsPerEnemyTurn).Append('|')
            .Append(bandit.Parameters.DefenseThreshold).Append('|')
            .Append(bandit.Parameters.OpportunisticCaptureMinStones).Append('|')
            .AppendJoin(',', bandit.PlacementPermissions).Append('|')
            .AppendJoin(',', bandit.MandatoryOverrides).Append('|')
            .AppendJoin(',', bandit.PlanPriority).Append('|')
            .AppendJoin(',', bandit.CounterattackPriority).Append('|')
            .Append(bandit.TieBreak).AppendLine();
        foreach (var intent in bandit.Intents)
        {
            builder.Append(intent.Kind).Append('|')
                .Append(intent.CandidateRule).Append('|')
                .AppendJoin(',', intent.PlacementModes).Append('|')
                .Append(intent.ScoreProfile).Append('|')
                .AppendJoin(',', intent.Fallback).AppendLine();
        }

        return builder.ToString();
    }

    private static void AppendOperations(
        StringBuilder builder,
        IEnumerable<CardOperationDefinition> operations)
    {
        foreach (var operation in operations)
        {
            builder.Append(operation.Kind).Append(':');
            builder.Append(operation switch
            {
                PlaceStoneOperationDefinition value => value.StoneKind,
                DrawIfRealLibertiesAtLeastOperationDefinition value =>
                    $"{value.MinimumRealLiberties},{value.Cards}",
                GainQiIfEnemyAtariOperationDefinition value => value.Amount,
                TemporaryLibertyOperationDefinition value =>
                    $"{value.Amount},{value.DurationKind},{value.Timing},{value.Stacking}",
                DrawIfTargetAtariOperationDefinition value => value.Cards,
                BuildFacilityOperationDefinition value => value.FacilityContentId,
                ReserveDrawOperationDefinition value => value.Cards,
                _ => throw new InvalidOperationException(
                    $"Unknown test operation type {operation.GetType().Name}."),
            }).Append(';');
        }
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Igorogue.sln")))
        {
            current = current.Parent;
        }

        return current ?? throw new DirectoryNotFoundException("Repository root not found.");
    }

    private sealed class GeneratedContentFixture : IDisposable
    {
        private GeneratedContentFixture(string root)
        {
            Root = root;
            ManifestPath = Path.Combine(root, "content_manifest.json");
        }

        public string Root { get; }

        public string ManifestPath { get; }

        public static GeneratedContentFixture Create()
        {
            var source = Path.Combine(
                FindRepositoryRoot().FullName,
                "build",
                "generated_content");
            var root = Path.Combine(Path.GetTempPath(), $"igorogue-core-content-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            CopyDirectory(source, root);
            return new GeneratedContentFixture(root);
        }

        public void MutateJson(string relativePath, Action<JsonNode> mutation)
        {
            var path = ContentPath(relativePath);
            var root = JsonNode.Parse(File.ReadAllText(path, Encoding.UTF8))
                ?? throw new InvalidDataException($"Fixture JSON is empty: {relativePath}.");
            mutation(root);
            WriteCanonicalJson(path, root);
            RefreshManifest();
        }

        public void RemoveGeneratedFile(string relativePath)
        {
            File.Delete(ContentPath(relativePath));
            RefreshManifest();
        }

        public void TamperWithoutRefreshingManifest(string relativePath, string content) =>
            File.WriteAllText(ContentPath(relativePath), content, new UTF8Encoding(false));

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }

        private string ContentPath(string relativePath) => Path.Combine(
            new[] { Root, "files" }.Concat(relativePath.Split('/')).ToArray());

        private void RefreshManifest()
        {
            var filesRoot = Path.Combine(Root, "files");
            var files = Directory.EnumerateFiles(filesRoot, "*.json", SearchOption.AllDirectories)
                .Select(path =>
                {
                    var relative = Path.GetRelativePath(filesRoot, path)
                        .Replace(Path.DirectorySeparatorChar, '/');
                    var content = File.ReadAllBytes(path);
                    return new FixtureFile(relative, content);
                })
                .OrderBy(file => file.Path, StringComparer.Ordinal)
                .ToArray();

            using var aggregate = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            foreach (var file in files)
            {
                AppendAggregateFile(aggregate, file.Path, file.Content);
            }

            var manifest = new JsonObject
            {
                ["schema_version"] = 1,
                ["content_hash"] =
                    $"sha256:{Convert.ToHexString(aggregate.GetHashAndReset()).ToLowerInvariant()}",
                ["files"] = new JsonArray(files.Select(file =>
                    (JsonNode)new JsonObject
                    {
                        ["path"] = file.Path,
                        ["sha256"] = file.Sha256,
                        ["bytes"] = file.Content.LongLength,
                    }).ToArray()),
            };
            WriteCanonicalJson(ManifestPath, manifest);
        }

        private static void CopyDirectory(string source, string destination)
        {
            foreach (var directory in Directory.EnumerateDirectories(
                         source,
                         "*",
                         SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(directory.Replace(source, destination, StringComparison.Ordinal));
            }

            foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                File.Copy(
                    file,
                    file.Replace(source, destination, StringComparison.Ordinal));
            }
        }

        private static void WriteCanonicalJson(string path, JsonNode root)
        {
            using var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
            {
                WriteCanonicalNode(writer, root);
            }

            buffer.WriteByte((byte)'\n');
            File.WriteAllBytes(path, buffer.ToArray());
        }

        private static void WriteCanonicalNode(Utf8JsonWriter writer, JsonNode? node)
        {
            switch (node)
            {
                case null:
                    writer.WriteNullValue();
                    return;
                case JsonObject value:
                    writer.WriteStartObject();
                    foreach (var property in value.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                    {
                        writer.WritePropertyName(property.Key);
                        WriteCanonicalNode(writer, property.Value);
                    }

                    writer.WriteEndObject();
                    return;
                case JsonArray value:
                    writer.WriteStartArray();
                    foreach (var item in value)
                    {
                        WriteCanonicalNode(writer, item);
                    }

                    writer.WriteEndArray();
                    return;
                default:
                    node.WriteTo(writer);
                    return;
            }
        }

        private static void AppendAggregateFile(
            IncrementalHash aggregate,
            string relativePath,
            ReadOnlySpan<byte> content)
        {
            var pathBytes = Encoding.UTF8.GetBytes(relativePath);
            Span<byte> pathLength = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(pathLength, checked((uint)pathBytes.Length));
            aggregate.AppendData(pathLength);
            aggregate.AppendData(pathBytes);

            Span<byte> contentLength = stackalloc byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64LittleEndian(contentLength, checked((ulong)content.Length));
            aggregate.AppendData(contentLength);
            aggregate.AppendData(content);
        }

        private sealed record FixtureFile(string Path, byte[] Content)
        {
            public string Sha256 =>
                $"sha256:{Convert.ToHexString(SHA256.HashData(Content)).ToLowerInvariant()}";
        }
    }
}
