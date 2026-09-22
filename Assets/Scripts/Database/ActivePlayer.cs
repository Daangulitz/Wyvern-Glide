[System.Serializable]
public class ActivePlayer
{
    public int sessionId;
    public int playerId;
    public int gameId;
    public string name;
    public GameInfo game;
}

[System.Serializable]
public class GameInfo
{
    public int id;
    public string name;
    public bool usesScore;
}