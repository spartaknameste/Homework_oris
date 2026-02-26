namespace AliasGame.Shared.Protocol.Packets;

public class XPacketAdminLogin
{
    [XField(0)]
    public string Username = string.Empty;

    [XField(1)]
    public string PasswordHash = string.Empty;
}

public class XPacketAdminLoginResponse
{
    [XField(0)]
    public bool Success;

    [XField(1)]
    public string Message = string.Empty;

    [XField(2)]
    public string AdminToken = string.Empty;
}

public class XPacketGetWords
{
    [XField(0)]
    public int CategoryId; 
    [XField(1)]
    public int PageNumber;

    [XField(2)]
    public int PageSize;

    [XField(3)]
    public string SearchQuery = string.Empty;
}

public class XPacketWordsResponse
{
    [XField(0)]
    public string WordsJson = string.Empty;

    [XField(1)]
    public int TotalCount;

    [XField(2)]
    public int PageNumber;
}

public class XPacketAddWord
{
    [XField(0)]
    public string Word = string.Empty;

    [XField(1)]
    public int CategoryId;

    [XField(2)]
    public int Difficulty; }

public class XPacketAddWordResponse
{
    [XField(0)]
    public bool Success;

    [XField(1)]
    public int WordId;

    [XField(2)]
    public string Message = string.Empty;
}

public class XPacketDeleteWord
{
    [XField(0)]
    public int WordId;
}

public class XPacketUpdateWord
{
    [XField(0)]
    public int WordId;

    [XField(1)]
    public string Word = string.Empty;

    [XField(2)]
    public int CategoryId;

    [XField(3)]
    public int Difficulty;
}

public class XPacketGetCategories
{
    [XField(0)]
    public bool IncludeWordCount;
}

public class XPacketCategoriesResponse
{
    [XField(0)]
    public string CategoriesJson = string.Empty;
}

public class XPacketAddCategory
{
    [XField(0)]
    public string Name = string.Empty;

    [XField(1)]
    public string Description = string.Empty;
}

public class XPacketDeleteCategory
{
    [XField(0)]
    public int CategoryId;
}

public class XPacketGetUsers
{
    [XField(0)]
    public int PageNumber;

    [XField(1)]
    public int PageSize;

    [XField(2)]
    public string SearchQuery = string.Empty;
}

public class XPacketUsersResponse
{
    [XField(0)]
    public string UsersJson = string.Empty;

    [XField(1)]
    public int TotalCount;
}

public class XPacketBanUser
{
    [XField(0)]
    public int UserId;

    [XField(1)]
    public string Reason = string.Empty;

    [XField(2)]
    public int DurationMinutes; }

public class XPacketUnbanUser
{
    [XField(0)]
    public int UserId;
}

public class XPacketManualScoreChange
{
    [XField(0)]
    public int TeamId;

    [XField(1)]
    public int PointChange; 
    [XField(2)]
    public string Reason = string.Empty;
}
