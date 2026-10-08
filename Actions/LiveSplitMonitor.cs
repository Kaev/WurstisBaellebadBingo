using System.Collections.Generic;

#if EXTERNAL_EDITOR
public class LiveSplitMonitor : CPHInlineBase
#else
public class CPHInline
#endif
{
    private const string BetsKeyPrefix   = "BBB_bets_";
    private const string ActiveAttemptKey = "BBB_active_attempt";

    public bool Execute()
    {
        // Streamer.bot injects the raw WebSocket message as args["message"].
        // LiveSplit replies with just the attempt number, e.g. "42".
        var raw = args.TryGetValue("message", out var v) ? v?.ToString().Trim() ?? "" : "";

        if (!int.TryParse(raw, out int newAttempt) || newAttempt <= 0)
        {
            // Not an attempt-count reply: could be a timer phase or split name.
            HandleStateMessage(raw);
            return true;
        }

        var activeAttempt = CPH.GetGlobalVar<int>(ActiveAttemptKey, false);

        if (newAttempt == activeAttempt)
            return true; // Attempt unchanged

        CPH.SetGlobalVar(ActiveAttemptKey, newAttempt, false);
        
        CPH.LogInfo($"[BBB] Attempt changed: {activeAttempt} -> {newAttempt}");

        var betters = GetBetters(newAttempt);
        if (betters.Count == 0) {
            CPH.SendMessage($"[BBB] Versuch {newAttempt} ist aktiv! Niemand hat auf diesen Versuch gesetzt!");
        } 
        else {
            var mentions = BuildMentions(betters, maxChars: 400);
            CPH.SendMessage($"[BBB] Versuch {newAttempt} ist aktiv! {mentions} - ihr habt auf diesen Versuch gesetzt!");
        }

        CPH.LogInfo($"[BBB] Announced attempt {newAttempt} for {betters.Count} viewer(s).");

        return true;
    }

    private const string PhaseKey = "BBB_last_phase";
    private const string DeckReachedKey = "BBB_deck_reached";
    private const string DeckCheckSplitName = "Deck-Check";
    private const int TimeoutDuration = 10; // In Seconds

    private void HandleStateMessage(string msg)
    {
        var lastPhase = CPH.GetGlobalVar<string>(PhaseKey, false) ?? "";

        switch (msg)
        {
            case "Running":
            case "Paused":
                if (lastPhase != "Running" && lastPhase != "Paused")
                    CPH.SetGlobalVar(DeckReachedKey, false, false); // new run started
                CPH.SetGlobalVar(PhaseKey, msg, false);
                return;

            case "NotRunning": // reset
            case "Ended":      // finished
                CPH.SetGlobalVar(PhaseKey, msg, false);
                if (lastPhase != "Running" && lastPhase != "Paused")
                    return; // no run was in progress
                var deckReached = CPH.GetGlobalVar<bool>(DeckReachedKey, false);
                CPH.SetGlobalVar(DeckReachedKey, false, false);
                ResolveBets(msg == "Ended" || deckReached);
                return;
        }

        // Anything else is treated as the current split name.
        if ((lastPhase == "Running" || lastPhase == "Paused")
            && msg.Equals(DeckCheckSplitName, System.StringComparison.OrdinalIgnoreCase))
        {
            CPH.SetGlobalVar(DeckReachedKey, true, false);
        }
    }

    private void ResolveBets(bool good)
    {
        var attempt = CPH.GetGlobalVar<int>(ActiveAttemptKey, false);
        if (attempt <= 0)
            return;

        var key = $"{BetsKeyPrefix}{attempt}";
        var bets = CPH.GetGlobalVar<string>(key, false);
        CPH.UnsetGlobalVar(key, false);
        if (string.IsNullOrEmpty(bets))
            return;

        foreach (var entry in bets.Split(','))
        {
            var parts = entry.Split('|');
            if (parts.Length < 3)
                continue;
            var username = parts[0];
            CPH.TwitchRedemptionFulfill(parts[1], parts[2]);
            if (good)
            {
                CPH.SendMessage($"[BBB] Glückwunsch, {username}! Die Wette auf Run {attempt} war erfolgreich!");
            }
            else
            {
                CPH.SendMessage($"[BBB] @{username}, leider war die Wette auf den Run {attempt} nicht erfolgreich. Viel Glück beim nächsten Mal!");
                CPH.TwitchTimeoutUser(username, TimeoutDuration, $"Wette auf Run {attempt} verloren");
            }
        }
    }

    private static string BuildMentions(List<string> users, int maxChars)
    {
        var sb = new System.Text.StringBuilder();
        foreach (string u in users)
        {
            var mention = $"@{u} ";
            if (sb.Length + mention.Length > maxChars)
            {
                int remaining = users.Count - users.IndexOf(u);
                sb.Append($"(+{remaining} weitere)");
                break;
            }
            sb.Append(mention);
        }
        return sb.ToString().TrimEnd();
    }

    private List<string> GetBetters(int attempt)
    {
        var csv = CPH.GetGlobalVar<string>($"{BetsKeyPrefix}{attempt}", false);
        if (string.IsNullOrEmpty(csv)) 
            return [];

        var result = new List<string>();
        foreach (var entry in csv.Split(','))
        {
            var parts = entry.Split('|');
            if (parts.Length > 0 && !string.IsNullOrEmpty(parts[0]))
                result.Add(parts[0]);
        }
        return result;
    }
}
