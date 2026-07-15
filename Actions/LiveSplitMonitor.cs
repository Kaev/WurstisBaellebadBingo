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
            // Not an attempt-count reply (could be another message type) - ignore.
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
