#if EXTERNAL_EDITOR
public class CommandQuit : CPHInlineBase
#else
public class CPHInline
#endif
{
    private const string BetsKeyPrefix    = "BBB_bets_";
    private const string ActiveAttemptKey = "BBB_active_attempt";

    public bool Execute()
    {
        var activeAttempt = CPH.GetGlobalVar<int>(ActiveAttemptKey, false);

        for (int i = activeAttempt + 1; i <= activeAttempt + 30; i++)
        {
            var betters = CPH.GetGlobalVar<string>($"{BetsKeyPrefix}{i}", false);
            if (string.IsNullOrEmpty(betters))
                continue;

            foreach (var entry in betters.Split(','))
            {
                var parts = entry.Split('|');
                if (parts.Length < 3)
                    continue;

                var user = parts[0];
                var rewardId = parts[1];
                var redemptionId = parts[2];
                CPH.TwitchRedemptionCancel(rewardId, redemptionId);
                CPH.SendMessage($"[BBB] @{user} erhält den Wetteinsatz für Run {i} zurück.");
            }

            CPH.UnsetGlobalVar($"{BetsKeyPrefix}{i}", false);
        }

        return true;
    }
}
