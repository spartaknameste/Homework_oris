namespace AliasGame.Shared.Protocol.Packets;

public class XPacketUpdateSettings
{
    [XField(0)]
    public int RoundTimeSeconds;

    [XField(1)]
    public int TotalRounds;

    [XField(2)]
    public int ScoreToWin;

    [XField(3)]
    public int LastWordTimeSeconds; 
    [XField(4)]
    public bool AllowManualScoreChange;

    [XField(5)]
    public bool AllowHostPassTurn;

    [XField(6)]
    public int CategoryId; 
    [XField(7)]
    public int SkipPenalty; }

public class XPacketSettingsChanged
{
    [XField(0)]
    public string SettingsJson = string.Empty; }

public class XPacketGetSettings
{
    [XField(0)]
    public int LobbyId;
}

public class XPacketSettingsResponse
{
    [XField(0)]
    public string SettingsJson = string.Empty;
}
