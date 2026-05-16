using CloudWeaverVoyage.Presentation;

Console.WriteLine("=== Epic #17 Story 001: Feedback Request Router Core ===");

var failed = 0;
var total = 0;

Run("AC-1: supported event creates request without domain write", test_supported_event_creates_request_without_domain_write);
Run("AC-2: request field contract is complete and payload readonly", test_request_field_contract_is_complete_and_payload_readonly);
Run("AC-3: priority score selects deterministically with fifo tie", test_priority_score_selects_deterministically_with_fifo_tie);
Run("AC-4: coalescing keeps latest status within window", test_coalescing_keeps_latest_status_within_window);
Run("AC-5: idle queue has no per-frame work", test_idle_queue_has_no_per_frame_work);
Run("AC-6: diagnostics expose route coalesce skip and output decisions", test_diagnostics_expose_route_coalesce_skip_and_output_decisions);
Run("REG-1: default time path respects elapsed coalesce window", test_default_time_path_respects_elapsed_coalesce_window);
Run("REG-2: invalid payload shape is diagnosed and not queued", test_invalid_payload_shape_is_diagnosed_and_not_queued);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 001 validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 001 validation passed: {total}/{total} checks passed.");
return 0;

void Run(string label, Func<bool> test)
{
    total++;
    try
    {
        if (test())
        {
            Console.WriteLine($"[PASS] {label}");
            return;
        }
    }
    catch (Exception ex)
    {
        failed++;
        Console.Error.WriteLine($"[FAIL] {label}: {ex.GetType().Name}: {ex.Message}");
        return;
    }

    failed++;
    Console.Error.WriteLine($"[FAIL] {label}");
}

static bool test_supported_event_creates_request_without_domain_write()
{
    var feedback = CreateFeedback();
    var domainWrites = 0;
    var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["route_id"] = "route.sky-reef-arc-01",
        ["route_name"] = "Sky Reef",
        ["domain_write_sentinel"] = new Action(() => domainWrites++),
    };

    var result = feedback.RouteSemanticEvent("ui_route_selected", payload, nowSeconds: 1.0d);

    return result.Accepted
        && result.Request is not null
        && result.Request.EventId == "ui_route_selected"
        && feedback.PendingRequests.Count == 1
        && domainWrites == 0;
}

static bool test_request_field_contract_is_complete_and_payload_readonly()
{
    var feedback = CreateFeedback();
    var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["route_id"] = "route.sky-reef-arc-01",
        ["source_detail"] = "chart",
        ["visual_cue_id"] = "visual.override.route",
        ["audio_cue_id"] = "audio.override.route",
        ["caption_text"] = "caption.route",
        ["status_text"] = "status.route",
        ["coalesce_key"] = "route.sky-reef-arc-01:selected",
    };

    var request = feedback.RouteSemanticEvent("ui_route_selected", payload).Request
        ?? throw new InvalidOperationException("Request was not created.");
    payload["route_id"] = "route.changed";

    return request.EventId == "ui_route_selected"
        && request.SourceSystem == "ui-hud-chart-interface"
        && request.Priority == FeedbackPriority.Minor
        && request.CoalesceKey == "route.sky-reef-arc-01:selected"
        && request.VisualCueId == "visual.override.route"
        && request.AudioCueId == "audio.override.route"
        && request.CaptionText == "caption.route"
        && request.StatusText == "status.route"
        && Convert.ToString(request.Payload["route_id"]) == "route.sky-reef-arc-01"
        && request.Payload.ContainsKey("source_detail");
}

static bool test_priority_score_selects_deterministically_with_fifo_tie()
{
    var priorityFeedback = CreateFeedback();
    priorityFeedback.RouteSemanticEvent(
        "ui_panel_opened",
        new Dictionary<string, object?>(StringComparer.Ordinal) { ["panel_id"] = "S11_partner_sniff" },
        nowSeconds: 1.0d);
    priorityFeedback.RouteSemanticEvent(
        "ui_departure_confirmed",
        new Dictionary<string, object?>(StringComparer.Ordinal) { ["route_id"] = "route.sky-reef-arc-01" },
        nowSeconds: 1.1d);
    var highest = priorityFeedback.ProcessFrame().SelectedRequest;

    var tieFeedback = CreateFeedback();
    tieFeedback.RouteSemanticEvent(
        "ui_route_selected",
        new Dictionary<string, object?>(StringComparer.Ordinal) { ["route_id"] = "route.alpha" },
        nowSeconds: 2.0d);
    tieFeedback.RouteSemanticEvent(
        "ui_item_transferred",
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["item_id"] = "resource.basic_supply",
            ["from_pool"] = "CARRIED",
            ["to_pool"] = "STORAGE",
            ["quantity"] = 1,
        },
        nowSeconds: 2.1d);
    var fifo = tieFeedback.ProcessFrame().SelectedRequest;

    return highest is not null
        && highest.EventId == "ui_departure_confirmed"
        && FeedbackManager.CalculatePriorityScore(FeedbackPriority.Critical) == FeedbackManager.CriticalBasePriorityScore
        && FeedbackManager.CalculatePriorityScore(FeedbackPriority.Minor, urgencyBonus: 25, noveltyBonus: 10, cooldownPenalty: 50) == 15
        && fifo is not null
        && fifo.EventId == "ui_route_selected";
}

static bool test_coalescing_keeps_latest_status_within_window()
{
    var feedback = CreateFeedback();
    feedback.RouteSemanticEvent(
        "ui_route_selected",
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["route_id"] = "route.sky-reef-arc-01",
            ["coalesce_key"] = "route.selected",
            ["status_text"] = "status.old",
        },
        nowSeconds: 10.0d);
    var second = feedback.RouteSemanticEvent(
        "ui_route_selected",
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["route_id"] = "route.sky-reef-arc-01",
            ["coalesce_key"] = "route.selected",
            ["status_text"] = "status.latest",
        },
        nowSeconds: 10.2d);
    feedback.RouteSemanticEvent(
        "ui_route_selected",
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["route_id"] = "route.sky-reef-arc-01",
            ["coalesce_key"] = "route.selected",
            ["status_text"] = "status.outside_window",
        },
        nowSeconds: 10.6d);

    return second.Diagnostic.Decision == FeedbackOutputDecision.Coalesced
        && feedback.PendingRequests.Count == 2
        && feedback.PendingRequests[0].StatusText == "status.latest"
        && feedback.PendingRequests[1].StatusText == "status.outside_window";
}

static bool test_idle_queue_has_no_per_frame_work()
{
    var feedback = CreateFeedback();
    var idle = feedback.ProcessFrame();
    var before = feedback.FrameWorkCount;

    feedback.RouteSemanticEvent(
        "ui_panel_opened",
        new Dictionary<string, object?>(StringComparer.Ordinal) { ["panel_id"] = "S4_chart" });
    var active = feedback.ProcessFrame();
    var afterActive = feedback.FrameWorkCount;
    var idleAfterDrain = feedback.ProcessFrame();

    return idle.ImmediateReturn
        && !idle.IteratedQueue
        && before == 0
        && active.ProcessedOutputCount == 1
        && afterActive == 1
        && idleAfterDrain.ImmediateReturn
        && !idleAfterDrain.IteratedQueue
        && feedback.FrameWorkCount == afterActive;
}

static bool test_diagnostics_expose_route_coalesce_skip_and_output_decisions()
{
    var feedback = CreateFeedback();
    feedback.RouteSemanticEvent("unsupported_event");
    feedback.RouteSemanticEvent(
        "ui_route_selected",
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["route_id"] = "route.sky-reef-arc-01",
            ["coalesce_key"] = "route.selected",
        },
        nowSeconds: 1.0d);
    feedback.RouteSemanticEvent(
        "ui_route_selected",
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["route_id"] = "route.sky-reef-arc-01",
            ["coalesce_key"] = "route.selected",
            ["status_text"] = "status.latest",
        },
        nowSeconds: 1.1d);
    feedback.ProcessFrame();

    var diagnostics = feedback.Diagnostics;
    return diagnostics.Any(item =>
            item.EventId == "unsupported_event"
            && item.Decision == FeedbackOutputDecision.SkippedUnsupported)
        && diagnostics.Any(item =>
            item.EventId == "ui_route_selected"
            && item.Priority == FeedbackPriority.Minor
            && item.CoalesceKey == "route.selected"
            && item.Decision == FeedbackOutputDecision.Routed)
        && diagnostics.Any(item =>
            item.EventId == "ui_route_selected"
            && item.Decision == FeedbackOutputDecision.Coalesced
            && item.Coalesced
            && item.StatusText == "status.latest")
        && diagnostics.Any(item =>
            item.EventId == "ui_route_selected"
            && item.Decision == FeedbackOutputDecision.OutputSelected
            && item.PriorityScore == FeedbackManager.MinorBasePriorityScore);
}

static bool test_default_time_path_respects_elapsed_coalesce_window()
{
    var feedback = CreateFeedbackWithClock(20.0d, 21.0d);
    var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["route_id"] = "route.sky-reef-arc-01",
        ["coalesce_key"] = "route.selected",
    };

    feedback.RouteSemanticEvent("ui_route_selected", payload);
    feedback.RouteSemanticEvent("ui_route_selected", payload);

    return feedback.PendingRequests.Count == 2
        && feedback.Diagnostics.Count(item => item.Decision == FeedbackOutputDecision.Routed) == 2;
}

static bool test_invalid_payload_shape_is_diagnosed_and_not_queued()
{
    var feedback = CreateFeedback();
    var missingRoute = feedback.RouteSemanticEvent(
        "ui_route_selected",
        new Dictionary<string, object?>(StringComparer.Ordinal) { ["route_name"] = "Sky Reef" });
    var badQuantity = feedback.RouteSemanticEvent(
        "ui_item_transferred",
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["item_id"] = "resource.basic_supply",
            ["from_pool"] = "CARRIED",
            ["to_pool"] = "STORAGE",
            ["quantity"] = "1",
        });

    return !missingRoute.Accepted
        && !badQuantity.Accepted
        && feedback.PendingRequests.Count == 0
        && feedback.Diagnostics.Count(item => item.Decision == FeedbackOutputDecision.SkippedInvalidPayload) == 2;
}

static FeedbackManager CreateFeedback()
{
    var feedback = new FeedbackManager();
    feedback.Initialize();
    return feedback;
}

static FeedbackManager CreateFeedbackWithClock(params double[] clockValues)
{
    var index = 0;
    var feedback = new FeedbackManager(() =>
    {
        var value = clockValues[Math.Min(index, clockValues.Length - 1)];
        index++;
        return value;
    });
    feedback.Initialize();
    return feedback;
}
