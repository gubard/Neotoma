namespace Neotoma.Helpers;

public static partial class NeotomaLogs
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Add hash {Hash} to file {FileId}, for user {UserId}"
    )]
    public static partial void AddHashToFile(
        this ILogger logger,
        Guid fileId,
        string hash,
        Guid userId
    );
}
