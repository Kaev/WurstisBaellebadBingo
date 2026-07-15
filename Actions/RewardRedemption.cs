using System;
using System.Linq;

#if EXTERNAL_EDITOR
public class RewardRedemption : CPHInlineBase
#else
public class CPHInline
#endif
{
    private const string BetsKeyPrefix   = "BBB_bets_";
    private const string ActiveAttemptKey = "BBB_active_attempt";

    public bool Execute()
    {
        var username = Arg("user");
        var redemptionId = Arg("redemptionId");
        var rewardId = Arg("rewardId");

        // Check if the user has entered a number
        int userBetAttempt = ResolveAttempt();
        if (userBetAttempt <= 0)
        {
            CPH.SendMessage("[BBB] Ungültige Eingabe! Bitte gib einen gültigen Run ein.");
            CPH.TwitchRedemptionCancel(rewardId, redemptionId);
            return false;
        }

        // Check if the user is trying to bet on a valid attempt number
        var activeAttempt = CPH.GetGlobalVar<int>(ActiveAttemptKey, false);
        if (userBetAttempt > activeAttempt + 30 || userBetAttempt <= activeAttempt)
        {
            CPH.SendMessage($"[BBB] Ungültige Run-Nummer: {userBetAttempt}. Bitte wähle eine Run-Nummer zwischen {activeAttempt + 1} und {activeAttempt + 30}.");
            CPH.TwitchRedemptionCancel(rewardId, redemptionId);
            return false;
        }

        var attemptBetters = CPH.GetGlobalVar<string>($"{BetsKeyPrefix}{userBetAttempt}", false);
        
        // If there are no betters yet, just set the current user as the only better
        if (string.IsNullOrEmpty(attemptBetters))
        {
            CPH.SetGlobalVar($"{BetsKeyPrefix}{userBetAttempt}", $"{username}|{rewardId}|{redemptionId}", false);
            CPH.SendMessage($"[BBB] @{username}, du hast erfolgreich auf Run {userBetAttempt} gesetzt!");
            return true;
        }

        // Return channel points when the user has already bet on this attempt
        if (attemptBetters.Split(',').Any(e => e.Split('|')[0].Equals(username, StringComparison.OrdinalIgnoreCase)))
        {
            CPH.SendMessage($"[BBB] @{username}, du hast bereits auf Run {userBetAttempt} gesetzt!");
            CPH.TwitchRedemptionCancel(rewardId, redemptionId);
            return false;
        }

        // Add the user to the list of betters for this attempt
        CPH.SetGlobalVar($"{BetsKeyPrefix}{userBetAttempt}", string.Join(",", attemptBetters, $"{username}|{rewardId}|{redemptionId}"), false);
        CPH.SendMessage($"[BBB] @{username}, du hast erfolgreich auf Run {userBetAttempt} gesetzt!");
        return true;
    }

    private int ResolveAttempt()
    {
        string raw = Arg("rawInput").Trim();
        if (!string.IsNullOrEmpty(raw) && int.TryParse(raw, out int explicit_) && explicit_ > 0)
            return explicit_;
        return 0;
    }

    private string Arg(string key) =>
        args.TryGetValue(key, out var v) ? v?.ToString() ?? string.Empty : string.Empty;
}
