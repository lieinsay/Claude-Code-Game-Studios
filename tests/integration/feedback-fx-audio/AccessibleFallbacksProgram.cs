using CloudWeaverVoyage.Presentation;

Console.WriteLine("=== Epic #17 Story 003: Accessible Fallbacks, Subtitles and Missing Assets ===");

var failed = 0;
var total = 0;

Run("AC-1: missing visual asset skips visual only and preserves text fallback", test_missing_visual_asset_skips_visual_only_and_preserves_text_fallback);
Run("AC-2: missing audio asset skips audio only and preserves caption fallback", test_missing_audio_asset_skips_audio_only_and_preserves_caption_fallback);
Run("AC-3: muted or unavailable audio keeps visible equivalent", test_muted_or_unavailable_audio_keeps_visible_equivalent);
Run("AC-4: caption text requests subtitle with bounded duration", test_caption_text_requests_subtitle_with_bounded_duration);
Run("AC-5: save and load completion remain text-readable without assets", test_save_and_load_completion_remain_text_readable_without_assets);
Run("AC-6: unavailable caption layer falls back to status text", test_unavailable_caption_layer_falls_back_to_status_text);
Run("AC-7: color-only visual fallback is rejected or labeled", test_color_only_visual_fallback_is_rejected_or_labeled);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 003 validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 003 validation passed: {total}/{total} checks passed.");
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

static bool test_missing_visual_asset_skips_visual_only_and_preserves_text_fallback()
{
    var feedback = CreateFeedback();
    FeedbackRequest? selected = null;
    feedback.FeedbackOutputSelected += request => selected = request;
    feedback.MarkVisualCueMissing("visual.chart.route_selected");
    feedback.RouteSemanticEvent("ui_route_selected", RoutePayload("route.alpha"), nowSeconds: 1.0d);
    feedback.RouteSemanticEvent("ui_route_selected", RoutePayload("route.beta"), nowSeconds: 2.0d);

    feedback.ProcessFrame();
    feedback.ProcessFrame();

    return !HasOutput(feedback, FeedbackOutputChannel.Visual, "visual.chart.route_selected")
        && HasOutput(feedback, FeedbackOutputChannel.Audio, "audio.chart.route_selected")
        && selected is not null
        && selected.VisualCueId is null
        && selected.AudioCueId == "audio.chart.route_selected"
        && HasTextOutput(feedback, FeedbackOutputChannel.Status, "feedback.route_selected")
        && HasTextOutput(feedback, FeedbackOutputChannel.Subtitle, "feedback.route_selected.caption")
        && feedback.Diagnostics.Count(item =>
            item.Decision == FeedbackOutputDecision.VisualSkippedMissingAsset
            && item.FallbackReason == "missing_visual_asset") == 1;
}

static bool test_missing_audio_asset_skips_audio_only_and_preserves_caption_fallback()
{
    var feedback = CreateFeedback();
    FeedbackRequest? selected = null;
    feedback.FeedbackOutputSelected += request => selected = request;
    feedback.MarkAudioCueMissing("audio.chart.route_selected");
    feedback.RouteSemanticEvent("ui_route_selected", RoutePayload("route.audio-missing"));

    feedback.ProcessFrame();

    return HasOutput(feedback, FeedbackOutputChannel.Visual, "visual.chart.route_selected")
        && !HasOutput(feedback, FeedbackOutputChannel.Audio, "audio.chart.route_selected")
        && selected is not null
        && selected.VisualCueId == "visual.chart.route_selected"
        && selected.AudioCueId is null
        && HasTextOutput(feedback, FeedbackOutputChannel.Status, "feedback.route_selected")
        && HasTextOutput(feedback, FeedbackOutputChannel.Subtitle, "feedback.route_selected.caption")
        && feedback.Diagnostics.Any(item =>
            item.Decision == FeedbackOutputDecision.AudioSkippedMissingAsset
            && item.FallbackReason == "missing_audio_asset");
}

static bool test_muted_or_unavailable_audio_keeps_visible_equivalent()
{
    var muted = CreateFeedback();
    muted.IsAudioMuted = true;
    muted.RouteSemanticEvent("ui_route_selected", RoutePayload("route.muted"));
    muted.ProcessFrame();

    var unavailable = CreateFeedback();
    unavailable.IsAudioDeviceAvailable = false;
    unavailable.RouteSemanticEvent("ui_departure_confirmed", DeparturePayload("route.unavailable"));
    unavailable.ProcessFrame();

    var audioOnly = CreateFeedback();
    audioOnly.RouteSemanticEvent(
        "ui_route_selected",
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["route_id"] = "route.audio-only",
            ["status_text"] = string.Empty,
            ["caption_text"] = string.Empty,
        });
    audioOnly.ProcessFrame();

    return !HasOutput(muted, FeedbackOutputChannel.Audio, "audio.chart.route_selected")
        && HasTextOutput(muted, FeedbackOutputChannel.Subtitle, "feedback.route_selected.caption")
        && HasTextOutput(muted, FeedbackOutputChannel.Status, "feedback.route_selected")
        && muted.Diagnostics.Any(item =>
            item.Decision == FeedbackOutputDecision.AudioSkippedUnavailable
            && item.FallbackReason == "audio_muted")
        && !HasOutput(unavailable, FeedbackOutputChannel.Audio, "audio.chart.departure_confirmed")
        && HasTextOutput(unavailable, FeedbackOutputChannel.Subtitle, "feedback.departure_confirmed.caption")
        && HasTextOutput(unavailable, FeedbackOutputChannel.Status, "feedback.departure_confirmed")
        && unavailable.Diagnostics.Any(item =>
            item.Decision == FeedbackOutputDecision.AudioSkippedUnavailable
            && item.FallbackReason == "audio_device_unavailable")
        && !HasOutput(audioOnly, FeedbackOutputChannel.Audio, "audio.chart.route_selected")
        && audioOnly.Diagnostics.Any(item =>
            item.Decision == FeedbackOutputDecision.SkippedInvalidPayload
            && item.FallbackReason == "audio_without_available_visible_fallback");
}

static bool test_caption_text_requests_subtitle_with_bounded_duration()
{
    var feedback = CreateFeedback();
    var subtitles = new List<FeedbackSubtitleRequest>();
    feedback.SubtitleRequested += subtitles.Add;
    feedback.RouteSemanticEvent(
        "ui_route_selected",
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["route_id"] = "route.caption",
            ["caption_text"] = "Readable caption text",
            ["status_text"] = "feedback.route_selected",
        });

    feedback.ProcessFrame();
    var subtitle = subtitles.Single();

    return subtitle.EventId == "ui_route_selected"
        && subtitle.Priority == FeedbackPriority.Minor
        && subtitle.CaptionText == "Readable caption text"
        && NearlyEqual(subtitle.DurationSeconds, FeedbackManager.CalculateCaptionDurationSeconds("Readable caption text"))
        && NearlyEqual(FeedbackManager.CalculateCaptionDurationSeconds("short"), 2.0d)
        && NearlyEqual(FeedbackManager.CalculateCaptionDurationSeconds(new string('x', 200)), 6.0d)
        && feedback.Diagnostics.Any(item => item.Decision == FeedbackOutputDecision.SubtitleRequested);
}

static bool test_save_and_load_completion_remain_text_readable_without_assets()
{
    var feedback = CreateFeedback();
    feedback.MarkVisualCueMissing("visual.session.save");
    feedback.MarkAudioCueMissing("audio.session.save");
    feedback.MarkVisualCueMissing("visual.session.load");
    feedback.MarkAudioCueMissing("audio.session.load");

    feedback.RouteSemanticEvent("ui_save_completed", nowSeconds: 1.0d, sourceSystem: "Persistence");
    feedback.RouteSemanticEvent("ui_load_completed", nowSeconds: 2.0d, sourceSystem: "Persistence");
    feedback.ProcessFrame();
    feedback.ProcessFrame();

    return !HasOutput(feedback, FeedbackOutputChannel.Visual, "visual.session.save")
        && !HasOutput(feedback, FeedbackOutputChannel.Audio, "audio.session.save")
        && !HasOutput(feedback, FeedbackOutputChannel.Visual, "visual.session.load")
        && !HasOutput(feedback, FeedbackOutputChannel.Audio, "audio.session.load")
        && HasTextOutput(feedback, FeedbackOutputChannel.Status, "feedback.save_completed")
        && HasTextOutput(feedback, FeedbackOutputChannel.Status, "feedback.load_completed")
        && HasTextOutput(feedback, FeedbackOutputChannel.Subtitle, "feedback.save_completed.caption")
        && HasTextOutput(feedback, FeedbackOutputChannel.Subtitle, "feedback.load_completed.caption");
}

static bool test_unavailable_caption_layer_falls_back_to_status_text()
{
    var feedback = CreateFeedback();
    var subtitleCount = 0;
    feedback.IsCaptionLayerAvailable = false;
    feedback.SubtitleRequested += _ => subtitleCount++;
    feedback.RouteSemanticEvent("ui_route_selected", RoutePayload("route.no-caption-layer"));

    feedback.ProcessFrame();

    return subtitleCount == 0
        && !feedback.PresentationOutputs.Any(item => item.Channel == FeedbackOutputChannel.Subtitle)
        && feedback.PresentationOutputs.Any(item =>
            item.Channel == FeedbackOutputChannel.Status
            && item.Text == "feedback.route_selected"
            && item.FallbackReason == "caption_layer_unavailable")
        && feedback.Diagnostics.Any(item =>
            item.Decision == FeedbackOutputDecision.StatusFallbackRequested
            && item.FallbackReason == "caption_layer_unavailable")
        && CaptionOnlyWithoutLayerSkipsAudio();
}

static bool test_color_only_visual_fallback_is_rejected_or_labeled()
{
    var labeled = CreateFeedback();
    labeled.MarkColorOnlyVisualCue("visual.chart.route_selected");
    labeled.RouteSemanticEvent("ui_route_selected", RoutePayload("route.color-labeled"));
    labeled.ProcessFrame();

    var rejected = CreateFeedback();
    rejected.MarkColorOnlyVisualCue("visual.panel_context");
    rejected.RouteSemanticEvent(
        "ui_panel_opened",
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["panel_id"] = "S4_chart",
            ["status_text"] = string.Empty,
        });
    rejected.ProcessFrame();

    return labeled.PresentationOutputs.Any(item =>
            item.Channel == FeedbackOutputChannel.Visual
            && item.CueId == "visual.chart.route_selected"
            && item.Decision == FeedbackOutputDecision.AccessibilityFallbackApplied
            && item.Text == "feedback.route_selected")
        && labeled.Diagnostics.Any(item =>
            item.Decision == FeedbackOutputDecision.AccessibilityFallbackApplied
            && item.FallbackReason == "color_only_visual_labeled")
        && !rejected.PresentationOutputs.Any(item =>
            item.Channel == FeedbackOutputChannel.Visual
            && item.CueId == "visual.panel_context")
        && rejected.Diagnostics.Any(item =>
            item.Decision == FeedbackOutputDecision.ColorOnlyFallbackRejected
            && item.FallbackReason == "color_only_visual_cue");
}

static FeedbackManager CreateFeedback()
{
    var feedback = new FeedbackManager();
    feedback.Initialize();
    return feedback;
}

static Dictionary<string, object?> RoutePayload(string routeId)
{
    return new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["route_id"] = routeId,
        ["status_text"] = "feedback.route_selected",
        ["caption_text"] = "feedback.route_selected.caption",
    };
}

static Dictionary<string, object?> DeparturePayload(string routeId)
{
    return new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["route_id"] = routeId,
        ["status_text"] = "feedback.departure_confirmed",
        ["caption_text"] = "feedback.departure_confirmed.caption",
    };
}

static bool HasOutput(FeedbackManager feedback, FeedbackOutputChannel channel, string cueId)
{
    return feedback.PresentationOutputs.Any(item => item.Channel == channel && item.CueId == cueId);
}

static bool HasTextOutput(FeedbackManager feedback, FeedbackOutputChannel channel, string text)
{
    return feedback.PresentationOutputs.Any(item => item.Channel == channel && item.Text == text);
}

static bool NearlyEqual(double left, double right)
{
    return Math.Abs(left - right) < 0.0001d;
}

static bool CaptionOnlyWithoutLayerSkipsAudio()
{
    var feedback = CreateFeedback();
    FeedbackRequest? selected = null;
    feedback.IsCaptionLayerAvailable = false;
    feedback.FeedbackOutputSelected += request => selected = request;
    feedback.RouteSemanticEvent(
        "ui_route_selected",
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["route_id"] = "route.caption-only-no-layer",
            ["caption_text"] = "caption.only",
            ["status_text"] = string.Empty,
        });

    feedback.ProcessFrame();

    return selected is not null
        && selected.AudioCueId is null
        && !HasOutput(feedback, FeedbackOutputChannel.Audio, "audio.chart.route_selected")
        && feedback.Diagnostics.Any(item =>
            item.Decision == FeedbackOutputDecision.SkippedInvalidPayload
            && item.FallbackReason == "audio_without_available_visible_fallback");
}
