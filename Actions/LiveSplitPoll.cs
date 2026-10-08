#if EXTERNAL_EDITOR
public class LiveSplitPoll : CPHInlineBase
#else
public class CPHInline
#endif
{
    // Index of the custom WebSocket client connection in Streamer.bot (Servers/Clients -> WebSocket Clients).
    private const int LiveSplitConnection = 0;

    public bool Execute()
    {
        // Replies arrive in the "Custom WebSocket Client -> Message" trigger (LiveSplitMonitor).
        CPH.WebsocketSend("getcurrenttimerphase", LiveSplitConnection);
        CPH.WebsocketSend("getcurrentsplitname", LiveSplitConnection);
        return true;
    }
}

