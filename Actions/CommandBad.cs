#if EXTERNAL_EDITOR
public class CommandBad : CPHInlineBase
#else
public class CPHInline
#endif
{
    private const string BetsKeyPrefix   = "BBB_bets_";
    private const string ActiveAttemptKey = "BBB_active_attempt";
    private const int TimeoutDuration = 10; // In Seconds

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
            var username = parts[0];
            var rewardId = parts[1];
            var redemptionId = parts[2];
            CPH.TwitchRedemptionFulfill(rewardId, redemptionId);
            CPH.SendMessage($"[BBB] @{username}, leider war die Wette auf den Run {activeAttempt} nicht erfolgreich. Viel Glück beim nächsten Mal!");
            CPH.TwitchTimeoutUser(username, TimeoutDuration, $"Wette auf Run {activeAttempt} verloren");
        }

        CPH.UnsetGlobalVar($"{BetsKeyPrefix}{activeAttempt}", false);
        return true;
    }
}
