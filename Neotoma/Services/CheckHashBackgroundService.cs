using Gaia.Helpers;
using Gaia.Services;
using Neotoma.Contract.Models;
using Neotoma.Helpers;
using Nestor.Db.LiteDb.Helpers;
using Zeus.Services;

namespace Neotoma.Services;

public sealed class CheckHashBackgroundService : BackgroundService
{
    public CheckHashBackgroundService(
        DirectoryInfo dbsDirectory,
        GuidDatabaseFactory factory,
        ILogger<CheckHashBackgroundService> logger,
        IHashService<byte[], string> hashService
    )
    {
        _dbsDirectory = dbsDirectory;
        _factory = factory;
        _logger = logger;
        _hashService = hashService;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            if (!_dbsDirectory.Exists)
            {
                return;
            }

            var files = _dbsDirectory.GetFiles("*.litedb");

            foreach (var file in files)
            {
                if (!Guid.TryParse(file.GetFileNameWithoutExtension(), out var id))
                {
                    continue;
                }

                var database = _factory.Create(id);

                await database.ExecuteAsync(
                    db =>
                    {
                        var collection = db.GetFileObjectEntityCollection();
                        var documents = collection.FindAll().ToArray();

                        if (documents.Length == 0)
                        {
                            return;
                        }

                        foreach (var document in documents)
                        {
                            if (
                                document.TryGetValue(
                                    nameof(FileObjectEntity.Hash),
                                    out var hashValue
                                ) && (!hashValue.IsNull || !hashValue.AsString.IsNullOrWhiteSpace())
                            )
                            {
                                continue;
                            }

                            var documentId = document["_id"].AsGuid;
                            var data = document[nameof(FileObjectEntity.Data)].ToByteArray();
                            var hash = _hashService.ComputeHash(data);

                            if (hashValue is null)
                            {
                                document.Add(nameof(FileObjectEntity.Hash), hash);
                            }
                            else
                            {
                                document[nameof(FileObjectEntity.Hash)] = hash;
                            }

                            collection.Update(document);
                            _logger.AddHashToFile(documentId, hash, id);
                        }
                    },
                    ct
                );
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"{nameof(CheckHashBackgroundService)} error");
        }
    }

    private readonly DirectoryInfo _dbsDirectory;
    private readonly GuidDatabaseFactory _factory;
    private readonly ILogger<CheckHashBackgroundService> _logger;
    private readonly IHashService<byte[], string> _hashService;
}
