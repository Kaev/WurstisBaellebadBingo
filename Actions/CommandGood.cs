#if EXTERNAL_EDITOR
public class CommandGood : CPHInlineBase
#else
public class CPHInline
#endif
{
    private const string BetsKeyPrefix   = "BBB_bets_";
    private const string ActiveAttemptKey = "BBB_active_attempt";

    public bool Execute()
    {
        var activeAttempt = CPH.GetGlobalVar<int>(ActiveAttemptKey, false);
        if (activeAttempt <= 0)
        {
            CPH.SendMessage($"[BBB] Es gibt derzeit keinen aktiven Run. Bitte warte, bis der nächste Run gestartet wird.");
            return false;
        }

        var attemptBetters = CPH.GetGlobalVar<string>($"{BetsKeyPrefix}{activeAttempt}", false);
        if (string.IsNullOrEmpty(attemptBetters))
        {
            CPH.SendMessage($"[BBB] Es gab keine Wetten auf den Run {activeAttempt}.");
            CPH.UnsetGlobalVar($"{BetsKeyPrefix}{activeAttempt}", false);
            return true;
        }

        foreach (var entry in attemptBetters.Split(','))
        {
            var parts = entry.Split('|');
            if (parts.Length < 3)
                continue;
            CPH.TwitchRedemptionFulfill(parts[1], parts[2]);
        }

        CPH.SendMessage($"[BBB] Glück gehabt! Die Wetten auf Run {activeAttempt} waren erfolgreich. Herzlichen Glückwunsch an die Gewinner!");
        CPH.UnsetGlobalVar($"{BetsKeyPrefix}{activeAttempt}", false);
        return true;
    }
}
