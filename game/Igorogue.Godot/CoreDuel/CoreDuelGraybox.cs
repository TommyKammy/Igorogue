using Godot;

using Igorogue.Application.Battle;
using Igorogue.Domain.Board;

namespace Igorogue.Godot.CoreDuel;

/// <summary>
/// One-screen, 480x270 Core Duel graybox. This Control only renders query DTOs
/// and forwards input to <see cref="CoreDuelGameHost"/>.
/// </summary>
public partial class CoreDuelGraybox : Control
{
    private const float LogicalWidth = 480.0f;
    private const float LogicalHeight = 270.0f;
    private const float BoardSpacing = 24.0f;
    private const float PointHitRadius = 10.0f;
    private const int BoardSize = 7;

    private static readonly Vector2 BoardTopLeftPoint = new(132.0f, 34.0f);
    private static readonly Rect2 BoardPanel = new(120.0f, 22.0f, 168.0f, 168.0f);
    private static readonly Rect2 LeftPanel = new(4.0f, 22.0f, 108.0f, 168.0f);
    private static readonly Rect2 RightPanel = new(296.0f, 22.0f, 180.0f, 168.0f);
    private static readonly Rect2 ActionButton = new(350.0f, 202.0f, 126.0f, 60.0f);

    private static readonly Color Background = new("10151c");
    private static readonly Color Panel = new("18222d");
    private static readonly Color PanelBorder = new("314353");
    private static readonly Color BoardWood = new("b88957");
    private static readonly Color BoardGrid = new("3a2b20");
    private static readonly Color Text = new("e7edf3");
    private static readonly Color Muted = new("91a0ad");
    private static readonly Color Accent = new("67d5b5");
    private static readonly Color Warning = new("ff6b6b");
    private static readonly Color Intent = new("ef9f45");
    private static readonly Color BlackStone = new("17202a");
    private static readonly Color WhiteStone = new("e7e0d2");
    private static readonly Color BlackTerritory = new(0.18f, 0.78f, 0.59f, 0.26f);
    private static readonly Color WhiteTerritory = new(0.78f, 0.24f, 0.49f, 0.24f);

    private CoreDuelGameHost? host;
    private CanonicalPoint? hoveredPoint;
    private int framesUntilCapture = -1;
    private bool replayEvidencePrinted;

    public string? CapturePath { get; set; }

    private CoreDuelGameHost Host =>
        host ?? throw new InvalidOperationException("Core Duel graybox was not initialized.");

    public void Initialize(CoreDuelGameHost gameHost)
    {
        if (host is not null)
        {
            throw new InvalidOperationException("Core Duel graybox can only be initialized once.");
        }

        host = gameHost ?? throw new ArgumentNullException(nameof(gameHost));
    }

    public bool PrepareCaptureSelection()
    {
        foreach (var card in Host.Battle.HandCards)
        {
            if (!Host.SelectCard(card.InstanceId) ||
                Host.SelectedCard?.LegalCandidates.FirstOrDefault() is not { } candidate)
            {
                continue;
            }

            hoveredPoint = candidate.Target;
            QueueRedraw();
            return true;
        }

        Host.ClearCardSelection();
        hoveredPoint = null;
        return false;
    }

    public override void _Ready()
    {
        _ = Host;
        Position = Vector2.Zero;
        Size = new Vector2(LogicalWidth, LogicalHeight);
        CustomMinimumSize = Size;
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.All;
        GrabFocus();
        if (!string.IsNullOrWhiteSpace(CapturePath))
        {
            framesUntilCapture = 4;
            SetProcess(true);
        }
        else
        {
            SetProcess(false);
        }

        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _ = delta;
        if (framesUntilCapture < 0)
        {
            return;
        }

        framesUntilCapture--;
        if (framesUntilCapture > 0)
        {
            return;
        }

        framesUntilCapture = -1;
        var image = GetViewport().GetTexture().GetImage();
        var error = image.SavePng(CapturePath!);
        if (error == Error.Ok)
        {
            GD.Print($"IGOROGUE_GRAYBOX_CAPTURE path={CapturePath}");
            GetTree().Quit(0);
        }
        else
        {
            GD.PushError($"IGOROGUE_GRAYBOX_CAPTURE_FAILED error={error} path={CapturePath}");
            GetTree().Quit(1);
        }
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, new Vector2(LogicalWidth, LogicalHeight)), Background);
        DrawHeader();
        DrawPanel(LeftPanel);
        DrawPanel(RightPanel);
        DrawBoard();
        DrawLeftRail();
        DrawIntentRail();
        DrawHand();
        DrawActionButton();
        DrawTerminalOverlay();
    }

    public override void _GuiInput(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventMouseMotion motion:
                UpdateHover(motion.Position);
                break;
            case InputEventMouseButton button when button.Pressed:
                if (button.ButtonIndex == MouseButton.Left)
                {
                    HandlePrimaryClick(button.Position);
                }
                else if (button.ButtonIndex == MouseButton.Right)
                {
                    Host.ClearCardSelection();
                    hoveredPoint = null;
                    QueueRedraw();
                }

                AcceptEvent();
                break;
            case InputEventKey key when key.Pressed && !key.Echo:
                if (key.Keycode == Key.Space)
                {
                    ActivateTurnButton();
                    AcceptEvent();
                }
                else if (key.Keycode is >= Key.Key1 and <= Key.Key7)
                {
                    var index = (int)key.Keycode - (int)Key.Key1;
                    SelectHandCard(index);
                    AcceptEvent();
                }

                break;
        }
    }

    private void DrawHeader()
    {
        DrawText(new Vector2(4.0f, 15.0f), "IGOROGUE  ·  CORE DUEL", 11, Accent);
        var battle = Host.Battle;
        DrawText(
            new Vector2(192.0f, 15.0f),
            $"TURN {battle.PlayerTurnIndex ?? 0}  ·  {ReadableId(battle.PhaseId ?? "unknown")}",
            10,
            Text);
        DrawText(
            new Vector2(412.0f, 15.0f),
            $"R{battle.RestartCount ?? 0}",
            9,
            Muted);
    }

    private void DrawBoard()
    {
        DrawRect(BoardPanel, BoardWood);
        DrawRect(BoardPanel, BoardGrid, false, 2.0f);

        for (var index = 0; index < BoardSize; index++)
        {
            var offset = index * BoardSpacing;
            DrawLine(
                BoardTopLeftPoint + new Vector2(offset, 0.0f),
                BoardTopLeftPoint + new Vector2(offset, BoardSpacing * (BoardSize - 1)),
                BoardGrid,
                1.0f);
            DrawLine(
                BoardTopLeftPoint + new Vector2(0.0f, offset),
                BoardTopLeftPoint + new Vector2(BoardSpacing * (BoardSize - 1), offset),
                BoardGrid,
                1.0f);
        }

        foreach (var point in Host.Battle.BoardPoints)
        {
            DrawTerritory(point);
            DrawFacility(point);
        }

        DrawIntentPoints();
        DrawCandidatePoints();

        var atariPoints = Host.Battle.Groups
            .Where(group => group.IsAtari)
            .SelectMany(group => group.StonePoints)
            .ToHashSet();
        var dangerPoints = KingDangerPoints();
        foreach (var point in Host.Battle.BoardPoints.Where(point => point.Stone is not null))
        {
            DrawStone(point, atariPoints.Contains(point.Point), dangerPoints.Contains(point.Point));
        }

        for (var coordinate = 1; coordinate <= BoardSize; coordinate++)
        {
            DrawText(
                BoardTopLeftPoint + new Vector2((coordinate - 1) * BoardSpacing - 3.0f, 158.0f),
                coordinate.ToString(),
                8,
                Muted);
            DrawText(
                BoardTopLeftPoint + new Vector2(-15.0f, (BoardSize - coordinate) * BoardSpacing + 3.0f),
                coordinate.ToString(),
                8,
                Muted);
        }

        DrawText(new Vector2(123.0f, 188.0f), "(1,1) LEFT / BOTTOM", 8, Text);
        if (hoveredPoint is not null)
        {
            DrawText(new Vector2(238.0f, 188.0f), hoveredPoint.ToString(), 9, Accent);
        }
    }

    private void DrawTerritory(CoreDuelBoardPointPreview point)
    {
        var color = point.TerritoryOwnerId switch
        {
            "black" => BlackTerritory,
            "white" => WhiteTerritory,
            _ => Colors.Transparent,
        };
        if (color.A <= 0.0f)
        {
            return;
        }

        var center = PointToScreen(point.Point);
        DrawRect(new Rect2(center - new Vector2(8.0f, 8.0f), new Vector2(16.0f, 16.0f)), color);
        DrawLine(center + new Vector2(-7.0f, 6.0f), center + new Vector2(6.0f, -7.0f), color.Lightened(0.25f), 1.0f);
    }

    private void DrawFacility(CoreDuelBoardPointPreview point)
    {
        if (point.Facility is null)
        {
            return;
        }

        var center = PointToScreen(point.Point);
        var color = point.Facility.IsActive ? Accent : Muted;
        DrawRect(new Rect2(center - new Vector2(6.0f, 6.0f), new Vector2(12.0f, 12.0f)), color, false, 2.0f);
        DrawText(center + new Vector2(-3.0f, 3.0f), "F", 8, color);
    }

    private void DrawIntentPoints()
    {
        var intent = Host.Battle.DisplayedIntent;
        if (intent is null)
        {
            return;
        }

        if (intent.Target is not null)
        {
            foreach (var point in intent.Target.CurrentStonePoints)
            {
                DrawArc(PointToScreen(point), 11.0f, 0.0f, Mathf.Tau, 20, Intent, 1.0f);
            }
        }

        if (intent.PrimaryPoint is not null)
        {
            DrawDiamond(PointToScreen(intent.PrimaryPoint), 8.0f, Intent, 2.0f);
        }

        foreach (var alternate in intent.AlternatePoints)
        {
            DrawDiamond(PointToScreen(alternate), 5.0f, Intent.Darkened(0.25f), 1.0f);
        }
    }

    private void DrawCandidatePoints()
    {
        if (Host.SelectedCard is null)
        {
            return;
        }

        foreach (var candidate in Host.SelectedCard.LegalCandidates)
        {
            var center = PointToScreen(candidate.Target);
            var captures = candidate.AcceptedResult?.CapturedGroups.Count > 0;
            if (captures)
            {
                DrawLine(center + new Vector2(-6.0f, 6.0f), center + new Vector2(6.0f, -6.0f), Warning, 2.0f);
                DrawLine(center + new Vector2(-4.0f, -6.0f), center + new Vector2(4.0f, 6.0f), Warning, 2.0f);
            }
            else
            {
                DrawCircle(center, 4.0f, Accent);
            }

            if (hoveredPoint?.Equals(candidate.Target) == true)
            {
                DrawArc(center, 10.0f, 0.0f, Mathf.Tau, 24, Text, 2.0f);
            }
        }
    }

    private void DrawStone(
        CoreDuelBoardPointPreview point,
        bool isAtari,
        bool isKingDanger)
    {
        var stone = point.Stone!;
        var center = PointToScreen(point.Point);
        var isBlack = StringComparer.Ordinal.Equals(stone.ColorId, "black");
        DrawCircle(center + new Vector2(1.0f, 1.0f), 9.5f, new Color(0.0f, 0.0f, 0.0f, 0.28f));
        DrawCircle(center, 9.5f, isBlack ? BlackStone : WhiteStone);
        DrawArc(
            center,
            9.5f,
            0.0f,
            Mathf.Tau,
            24,
            isBlack ? new Color("45515e") : new Color("ada79b"),
            1.0f);

        if (stone.IsKing)
        {
            DrawText(center + new Vector2(-3.5f, 3.5f), isBlack ? "K" : "Q", 9, isBlack ? Text : BlackStone);
        }

        if (isAtari)
        {
            DrawArc(center, 12.0f, 0.0f, Mathf.Tau, 24, Warning, 2.0f);
        }

        if (isKingDanger)
        {
            DrawArc(center, 14.0f, 0.0f, Mathf.Tau, 24, Warning, 2.0f);
        }
    }

    private void DrawLeftRail()
    {
        var battle = Host.Battle;
        DrawText(new Vector2(10.0f, 39.0f), "PLAYER", 9, Muted);
        DrawText(new Vector2(10.0f, 59.0f), $"QI  {battle.Qi ?? 0}", 17, Accent);

        var risk = CurrentKingRisk();
        var danger = IsDanger(risk);
        DrawText(new Vector2(10.0f, 78.0f), "KING SAFETY", 8, Muted);
        DrawText(
            new Vector2(10.0f, 94.0f),
            danger ? "DANGER" : $"{risk?.Group?.EffectiveLibertyCount ?? 0} LIBERTIES",
            11,
            danger ? Warning : Text);

        DrawText(new Vector2(10.0f, 117.0f), "DECK", 8, Muted);
        DrawText(
            new Vector2(10.0f, 132.0f),
            $"DRAW {battle.DrawPileCount ?? 0}   DISC {battle.DiscardPileCount ?? 0}",
            9,
            Text);
        DrawText(new Vector2(10.0f, 151.0f), "BOARD KEY", 8, Muted);
        DrawCircle(new Vector2(15.0f, 164.0f), 3.0f, Accent);
        DrawText(new Vector2(23.0f, 167.0f), "legal", 8, Text);
        DrawLine(new Vector2(58.0f, 169.0f), new Vector2(69.0f, 158.0f), Warning, 2.0f);
        DrawText(new Vector2(73.0f, 167.0f), "capture", 8, Text);
        DrawText(new Vector2(10.0f, 183.0f), "red ring = atari / king risk", 8, Muted);
    }

    private void DrawIntentRail()
    {
        var battle = Host.Battle;
        DrawText(new Vector2(304.0f, 39.0f), "BANDIT INTENT", 9, Intent);
        var intent = battle.DisplayedIntent;
        if (intent is null)
        {
            DrawText(new Vector2(304.0f, 56.0f), battle.IsTerminal ? "BATTLE COMPLETE" : "PASS / NONE", 10, Text);
        }
        else
        {
            DrawText(new Vector2(304.0f, 56.0f), ReadableId(intent.IntentId), 10, Text);
            DrawText(
                new Vector2(304.0f, 71.0f),
                $"TARGET  {intent.Target?.Anchor.ToString() ?? "none"}",
                9,
                Text);
            DrawText(
                new Vector2(304.0f, 85.0f),
                $"PRIMARY {intent.PrimaryPoint?.ToString() ?? "pass"}",
                9,
                Intent);
            DrawText(
                new Vector2(304.0f, 99.0f),
                $"ALT     {string.Join(" ", intent.AlternatePoints.Select(point => point.ToString()))}",
                8,
                Muted);
        }

        DrawText(new Vector2(304.0f, 119.0f), "HOVER PREVIEW", 8, Muted);
        var candidate = hoveredPoint is null ? null : Host.CandidateAt(hoveredPoint);
        if (candidate?.AcceptedResult is { } result)
        {
            DrawText(new Vector2(304.0f, 134.0f), $"PLAY {candidate.Target}", 9, Accent);
            DrawText(
                new Vector2(304.0f, 148.0f),
                $"CAPTURE {result.CapturedGroups.Sum(group => group.StoneCount)}  TERR {Signed(result.BlackTerritoryPointDelta)}",
                8,
                result.CapturedGroups.Count > 0 ? Warning : Text);
            DrawText(
                new Vector2(304.0f, 162.0f),
                IsDanger(result.BlackKingRisk) ? "KING DANGER AFTER PLAY" : "KING SAFE AFTER PLAY",
                8,
                IsDanger(result.BlackKingRisk) ? Warning : Text);
        }
        else
        {
            DrawText(
                new Vector2(304.0f, 137.0f),
                Host.SelectedCard is null ? "select a card" : "hover a legal point",
                9,
                Muted);
        }

        DrawText(new Vector2(304.0f, 181.0f), $"STATUS {ReadableId(Host.LastActionReasonId)}", 8, Muted);
    }

    private void DrawHand()
    {
        var hand = Host.Battle.HandCards;
        var visibleSlots = Math.Max(6, hand.Count);
        for (var index = 0; index < visibleSlots; index++)
        {
            var rect = HandCardRect(index, hand.Count);
            if (index >= hand.Count)
            {
                DrawRect(rect, new Color("111820"));
                DrawRect(rect, PanelBorder, false, 1.0f);
                continue;
            }

            var card = hand[index];
            var selected = StringComparer.Ordinal.Equals(
                Host.SelectedCardInstanceId,
                card.InstanceId);
            var definition = Host.CardDefinition(card.ContentId);
            DrawRect(rect, selected ? new Color("25584e") : new Color("202e3a"));
            DrawRect(rect, selected ? Accent : PanelBorder, false, selected ? 2.0f : 1.0f);
            DrawText(
                rect.Position + new Vector2(3.0f, 12.0f),
                $"{index + 1} {CompactLabel(card.ContentId, visibleSlots)}",
                visibleSlots > 6 ? 7 : 8,
                Text);
            DrawText(rect.Position + new Vector2(4.0f, 29.0f), $"{definition.Cost} QI", 11, Accent);
            DrawText(
                rect.Position + new Vector2(4.0f, 45.0f),
                definition.Type.ToString().ToLowerInvariant(),
                7,
                Muted);
        }
    }

    private void DrawActionButton()
    {
        var terminal = Host.Battle.IsTerminal;
        DrawRect(ActionButton, terminal ? new Color("713d3d") : new Color("315568"));
        DrawRect(ActionButton, terminal ? Warning : Accent, false, 2.0f);
        DrawText(
            ActionButton.Position + new Vector2(17.0f, 26.0f),
            terminal ? "RESTART BATTLE" : "END TURN",
            terminal ? 12 : 14,
            Text);
        DrawText(
            ActionButton.Position + new Vector2(17.0f, 44.0f),
            terminal ? "click to restart" : "or press SPACE",
            8,
            Muted);
    }

    private void DrawTerminalOverlay()
    {
        var battle = Host.Battle;
        if (!battle.IsTerminal)
        {
            return;
        }

        var rect = new Rect2(139.0f, 72.0f, 130.0f, 72.0f);
        DrawRect(rect, new Color(0.05f, 0.08f, 0.11f, 0.94f));
        DrawRect(rect, Warning, false, 2.0f);
        DrawText(rect.Position + new Vector2(15.0f, 22.0f), ReadableId(battle.OutcomeId ?? "ended"), 13, Text);
        DrawText(rect.Position + new Vector2(10.0f, 41.0f), ReadableId(battle.EndReasonId ?? "unknown"), 8, Muted);
        var evidenceStatus = Host.ReplayEvidenceStatus;
        DrawText(
            rect.Position + new Vector2(10.0f, 58.0f),
            $"REPLAY V3 {evidenceStatus}",
            8,
            StringComparer.Ordinal.Equals(evidenceStatus, "VERIFIED")
                ? Accent
                : StringComparer.Ordinal.Equals(evidenceStatus, "FAILED")
                    ? Warning
                    : Muted);
        if (Host.ReplayEvidence?.ArtifactSha256 is { Length: >= 10 } hash)
        {
            DrawText(
                rect.Position + new Vector2(10.0f, 68.0f),
                hash[..10],
                7,
                Muted);
        }
    }

    private void HandlePrimaryClick(Vector2 position)
    {
        GrabFocus();
        if (ActionButton.HasPoint(position))
        {
            ActivateTurnButton();
            return;
        }

        for (var index = 0; index < Host.Battle.HandCards.Count; index++)
        {
            if (HandCardRect(index, Host.Battle.HandCards.Count).HasPoint(position))
            {
                SelectHandCard(index);
                return;
            }
        }

        var point = ScreenToPoint(position);
        if (point is not null)
        {
            if (Host.TryPlaySelectedCard(point))
            {
                hoveredPoint = null;
            }

            EmitReplayEvidenceIfReady();
        }

        QueueRedraw();
    }

    private void SelectHandCard(int index)
    {
        if (index < 0 || index >= Host.Battle.HandCards.Count)
        {
            return;
        }

        var card = Host.Battle.HandCards[index];
        if (StringComparer.Ordinal.Equals(Host.SelectedCardInstanceId, card.InstanceId))
        {
            Host.ClearCardSelection();
            hoveredPoint = null;
        }
        else
        {
            Host.SelectCard(card.InstanceId);
        }

        QueueRedraw();
    }

    private void ActivateTurnButton()
    {
        if (Host.Battle.IsTerminal)
        {
            EmitReplayEvidenceIfReady();
            Host.Restart();
        }
        else if (StringComparer.Ordinal.Equals(Host.Battle.PhaseId, "player_action"))
        {
            Host.EndTurnAndResolveEnemy();
            EmitReplayEvidenceIfReady();
        }

        hoveredPoint = null;
        QueueRedraw();
    }

    private void EmitReplayEvidenceIfReady()
    {
        if (replayEvidencePrinted || Host.ReplayEvidence is not { } evidence)
        {
            return;
        }

        replayEvidencePrinted = true;
        GD.Print(evidence.ToConsoleLine());
    }

    private void UpdateHover(Vector2 position)
    {
        var next = ScreenToPoint(position);
        if (!Equals(next, hoveredPoint))
        {
            hoveredPoint = next;
            QueueRedraw();
        }
    }

    private CanonicalPoint? ScreenToPoint(Vector2 position)
    {
        var xIndex = Mathf.RoundToInt((position.X - BoardTopLeftPoint.X) / BoardSpacing);
        var rowFromTop = Mathf.RoundToInt((position.Y - BoardTopLeftPoint.Y) / BoardSpacing);
        if (xIndex is < 0 or >= BoardSize || rowFromTop is < 0 or >= BoardSize)
        {
            return null;
        }

        var expected = BoardTopLeftPoint + new Vector2(xIndex * BoardSpacing, rowFromTop * BoardSpacing);
        if (position.DistanceTo(expected) > PointHitRadius)
        {
            return null;
        }

        var x = xIndex + 1;
        var y = BoardSize - rowFromTop;
        return Host.Battle.BoardPoints
            .Select(boardPoint => boardPoint.Point)
            .First(point => point.X == x && point.Y == y);
    }

    private HashSet<CanonicalPoint> KingDangerPoints()
    {
        var risk = CurrentKingRisk();
        return IsDanger(risk) && risk?.Group is not null
            ? risk.Group.StonePoints.ToHashSet()
            : [];
    }

    private CoreDuelBlackKingRiskPreview? CurrentKingRisk()
    {
        if (hoveredPoint is not null &&
            Host.CandidateAt(hoveredPoint)?.AcceptedResult is { } accepted)
        {
            return accepted.BlackKingRisk;
        }

        return Host.Battle.BlackKingRisk;
    }

    private static bool IsDanger(CoreDuelBlackKingRiskPreview? risk) =>
        risk?.IsCaptured == true ||
        risk?.HasMandatoryLethalOverride == true ||
        risk?.Group?.EffectiveLibertyCount <= 1;

    private static Vector2 PointToScreen(CanonicalPoint point) =>
        BoardTopLeftPoint + new Vector2(
            (point.X - 1) * BoardSpacing,
            (BoardSize - point.Y) * BoardSpacing);

    private static Rect2 HandCardRect(int index, int handCount)
    {
        const float availableWidth = 339.0f;
        const float gap = 3.0f;
        var slotCount = Math.Max(6, handCount);
        var width = (availableWidth - (gap * (slotCount - 1))) / slotCount;
        return new Rect2(4.0f + (index * (width + gap)), 202.0f, width, 60.0f);
    }

    private void DrawPanel(Rect2 rect)
    {
        DrawRect(rect, Panel);
        DrawRect(rect, PanelBorder, false, 1.0f);
    }

    private void DrawDiamond(Vector2 center, float radius, Color color, float width)
    {
        var points = new[]
        {
            center + new Vector2(0.0f, -radius),
            center + new Vector2(radius, 0.0f),
            center + new Vector2(0.0f, radius),
            center + new Vector2(-radius, 0.0f),
            center + new Vector2(0.0f, -radius),
        };
        for (var index = 0; index < points.Length - 1; index++)
        {
            DrawLine(points[index], points[index + 1], color, width);
        }
    }

    private void DrawText(Vector2 baseline, string value, int fontSize, Color color)
    {
        DrawString(
            ThemeDB.FallbackFont,
            baseline,
            value,
            HorizontalAlignment.Left,
            -1.0f,
            fontSize,
            color);
    }

    private static string CompactLabel(string contentId, int visibleSlots)
    {
        var readable = ReadableId(contentId);
        var maximumLength = visibleSlots > 7 ? 4 : visibleSlots > 6 ? 6 : 8;
        return readable.Length <= maximumLength
            ? readable
            : readable[..maximumLength];
    }

    private static string ReadableId(string value)
    {
        var withoutPrefixes = value
            .Replace("bandit_", string.Empty, StringComparison.Ordinal)
            .Replace("card_", string.Empty, StringComparison.Ordinal)
            .Replace("player_", string.Empty, StringComparison.Ordinal);
        return withoutPrefixes.Replace('_', ' ').ToUpperInvariant();
    }

    private static string Signed(int value) => value >= 0 ? $"+{value}" : value.ToString();
}
