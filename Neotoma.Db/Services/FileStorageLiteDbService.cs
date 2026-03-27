using System.Runtime.CompilerServices;
using Gaia.Models;
using Gaia.Services;
using Neotoma.Contract.Helpers;
using Neotoma.Contract.Models;
using Neotoma.Contract.Services;
using Nestor.Db.LiteDb.Services;
using Nestor.Db.Models;
using UltraLiteDB;

namespace Neotoma.Db.Services;

public sealed class FileStorageLiteDbService
    : LiteDbService<NeotomaGetRequest, NeotomaPostRequest, NeotomaGetResponse, NeotomaPostResponse>,
        IFileStorageDbService,
        IFileStorageDbCache
{
    public FileStorageLiteDbService(
        IDatabaseFactory factory,
        IFactory<DbValues> dbValuesFactory,
        IFactory<DbServiceOptions> factoryOptions
    )
        : base(factory, nameof(FileObjectEntity))
    {
        _dbValuesFactory = dbValuesFactory;
        _factoryOptions = factoryOptions;
    }

    public override ConfiguredValueTaskAwaitable<NeotomaGetResponse> GetAsync(
        NeotomaGetRequest request,
        CancellationToken ct
    )
    {
        return GetCore(request, ct).ConfigureAwait(false);
    }

    public ConfiguredValueTaskAwaitable UpdateAsync(NeotomaPostRequest source, CancellationToken ct)
    {
        return UpdateCore(source, ct).ConfigureAwait(false);
    }

    public ConfiguredValueTaskAwaitable UpdateAsync(NeotomaGetResponse source, CancellationToken ct)
    {
        return UpdateCore(source, ct).ConfigureAwait(false);
    }

    protected override ConfiguredValueTaskAwaitable ExecuteAsync(
        Guid idempotentId,
        NeotomaPostResponse response,
        NeotomaPostRequest request,
        CancellationToken ct
    )
    {
        return ExecuteCore(idempotentId, response, request, ct).ConfigureAwait(false);
    }

    private readonly IFactory<DbValues> _dbValuesFactory;
    private readonly IFactory<DbServiceOptions> _factoryOptions;

    private async ValueTask ExecuteCore(
        Guid idempotentId,
        NeotomaPostResponse response,
        NeotomaPostRequest request,
        CancellationToken ct
    )
    {
        var dbValues = _dbValuesFactory.Create();
        using var database = await Factory.CreateAsync(ct);
        var collection = database.GetFileObjectEntityCollection();
        var options = _factoryOptions.Create();
        Create(database, options, idempotentId, request.Creates, dbValues);
        var deleteIds = new List<Guid>(request.Deletes);

        foreach (var dir in request.DeleteDirs)
        {
            var ids = GetIdsByPattern(collection, dir + "/");
            deleteIds.AddRange(ids);
        }

        database.DeleteEntities(
            $"{dbValues.UserId}",
            idempotentId,
            options.IsUseEvents,
            deleteIds.ToArray()
        );

        await database.SaveChangesAsync(ct);
    }

    private async ValueTask UpdateCore(NeotomaGetResponse source, CancellationToken ct)
    {
        using var database = await Factory.CreateAsync(ct);
        var collection = database.GetFileObjectEntityCollection();

        foreach (var getFile in source.GetFiles)
        {
            var dbIds = GetIdsByPattern(collection, getFile.Key + "/");
            var entities = getFile.Value.Select(x => x.ToFileObjectEntity(getFile.Key)).ToArray();
            var requestIds = entities.Select(x => x.Id).ToArray();
            var deleteIds = dbIds.Except(requestIds).Select(x => new BsonValue(x)).ToArray();

            var exists = entities
                .Where(x => collection.Exists(Query.EQ("_id", x.Id)))
                .Select(x => x.Id)
                .ToArray();

            var inserts = entities
                .Where(x => !exists.Contains(x.Id))
                .Select(x => x.ToBsonDocument())
                .ToArray();

            var updates = entities
                .Where(x => exists.Contains(x.Id))
                .Select(x => x.ToBsonDocument())
                .ToArray();

            if (inserts.Length != 0)
            {
                collection.Insert(inserts);
            }

            if (updates.Length != 0)
            {
                collection.Update(updates);
            }

            if (deleteIds.Length != 0)
            {
                collection.Delete(Query.In("_id", deleteIds));
            }
        }

        await database.SaveChangesAsync(ct);
    }

    private async ValueTask<NeotomaGetResponse> GetCore(
        NeotomaGetRequest request,
        CancellationToken ct
    )
    {
        using var database = await Factory.CreateAsync(ct);
        var collection = database.GetFileObjectEntityCollection();
        var response = new NeotomaGetResponse();

        foreach (var dir in request.GetFiles)
        {
            var files = collection
                .Find(Query.StartsWith(nameof(FileObjectEntity.Path), dir + "/"))
                .Select(x => x.ToFileObjectEntity());

            response.GetFiles[dir] = files.Select(x => x.ToFileData()).ToArray();
        }

        return response;
    }

    private async ValueTask UpdateCore(NeotomaPostRequest source, CancellationToken ct)
    {
        await ExecuteAsync(Guid.NewGuid(), new(), source, ct);
    }

    private Guid[] GetIdsByPattern(UltraLiteCollection<BsonDocument> collection, string pattern)
    {
        var ids = collection
            .Find(Query.StartsWith(nameof(FileObjectEntity.Path), pattern))
            .Select(x => x["_id"].AsGuid)
            .ToArray();

        return ids;
    }

    private void Create(
        IDatabase database,
        DbServiceOptions options,
        Guid idempotentId,
        Dictionary<string, FileData[]> creates,
        DbValues dbValues
    )
    {
        if (creates.Count == 0)
        {
            return;
        }

        var entities = new FileObjectEntity[creates.Values.Sum(x => x.Length)];
        var index = 0;

        foreach (var create in creates)
        {
            foreach (var file in create.Value)
            {
                entities[index] = file.ToFileObjectEntity(create.Key);
                index++;
            }
        }

        database.AddEntities($"{dbValues.UserId}", idempotentId, options.IsUseEvents, entities);
    }
}
