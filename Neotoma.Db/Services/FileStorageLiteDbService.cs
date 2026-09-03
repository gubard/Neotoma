using System.Runtime.CompilerServices;
using Gaia.Helpers;
using Gaia.Models;
using Gaia.Services;
using Neotoma.Contract.Helpers;
using Neotoma.Contract.Models;
using Neotoma.Contract.Services;
using Nestor.Db.LiteDb.Services;
using Nestor.Db.Models;
using Nestor.Db.Services;
using UltraLiteDB;

namespace Neotoma.Db.Services;

public sealed class FileStorageLiteDbService
    : LiteDbService<NeotomaGetRequest, NeotomaPostRequest, NeotomaGetResponse, NeotomaPostResponse>,
        IFileStorageDbService,
        IFileStorageDbCache
{
    public FileStorageLiteDbService(
        IUltraLiteDatabaseFactory factory,
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
        var database = await Factory.CreateAsync(ct);

        await database.ExecuteAsync(
            db =>
            {
                var collection = db.GetFileObjectEntityCollection();
                var options = _factoryOptions.Create();
                Create(db, options, idempotentId, request.Creates, dbValues);
                var deleteIds = new List<Guid>(request.Deletes);

                foreach (var dir in request.DeleteDirs)
                {
                    var ids = GetIdsByPattern(collection, dir + "/");
                    deleteIds.AddRange(ids);
                }

                db.DeleteEntities(
                    $"{dbValues.UserId}",
                    idempotentId,
                    options.IsUseEvents,
                    deleteIds.ToArray()
                );

                return TaskHelper.ConfiguredCompletedTask;
            },
            ct
        );
    }

    private async ValueTask UpdateCore(NeotomaGetResponse source, CancellationToken ct)
    {
        var database = await Factory.CreateAsync(ct);

        await database.ExecuteAsync(
            db =>
            {
                var edits = new AutoDictionary<Guid, EditFileObjectEntity>();
                var collection = db.GetFileObjectEntityCollection();
                var deleteIds = new List<BsonValue>();
                var insertDocuments = new List<BsonDocument>();

                foreach (var info in source.Info)
                {
                    var requestIds = info.Value.SelectAsSpan(x => x.Id).ToArray();

                    var existIds = collection
                        .Find(Query.In("_id", requestIds.Select(x => new BsonValue(x))))
                        .Select(x => x["_id"].AsGuid);

                    var dbIds = GetIdsByPattern(collection, info.Key + "/")
                        .Concat(existIds)
                        .ToArray();

                    var di = dbIds
                        .Where(x => !requestIds.Contains(x))
                        .Select(x => new BsonValue(x));

                    deleteIds.AddRange(di);

                    var documents = requestIds
                        .Where(x => !dbIds.Contains(x))
                        .Select(x => new FileObjectEntity { Id = x }.ToBsonDocument());

                    insertDocuments.AddRange(documents);

                    foreach (var value in info.Value)
                    {
                        SetEdit(edits, info.Key, value);
                    }
                }

                foreach (var data in source.Data)
                {
                    var item = edits.GetItem(data.Id);
                    item.IsEditData = true;
                    item.Data = data.Data;
                    item.IsEditHash = true;
                    item.Hash = data.Hash;
                }

                if (insertDocuments.Count != 0)
                {
                    collection.Insert(insertDocuments);
                }

                if (edits.Count != 0)
                {
                    collection.Edit(edits.ToItemsArray());
                }

                if (deleteIds.Count != 0)
                {
                    collection.Delete(Query.In("_id", deleteIds));
                }

                return TaskHelper.ConfiguredCompletedTask;
            },
            ct
        );
    }

    private void SetEdit(
        AutoDictionary<Guid, EditFileObjectEntity> dictionary,
        string dir,
        FileObjectInfo info
    )
    {
        var item = dictionary.GetItem(info.Id);

        item.IsEditDescription = true;
        item.Description = info.Description;

        item.IsEditPath = true;
        item.Path = $"{dir}/{info.Name}";
    }

    private async ValueTask<NeotomaGetResponse> GetCore(
        NeotomaGetRequest request,
        CancellationToken ct
    )
    {
        var database = await Factory.CreateAsync(ct);

        return await database.ExecuteAsync(
            db =>
            {
                var collection = db.GetFileObjectEntityCollection();
                var response = new NeotomaGetResponse();

                if (request.IsGetAll)
                {
                    response.All = collection
                        .FindAll()
                        .Select(x => x.ToFileObjectEntity())
                        .GroupBy(x => Path.GetDirectoryName(x.Path).ThrowIfNull())
                        .ToDictionary(x => x.Key, x => x.Select(y => y.ToFileObject()).ToArray());
                }

                foreach (var dir in request.GetInfo)
                {
                    var files = collection
                        .Find(Query.StartsWith(nameof(FileObjectEntity.Path), dir + "/"))
                        .Select(x => x.ToFileObjectEntity());

                    response.Info[dir] = files.Select(x => x.ToFileObjectInfo()).ToArray();
                }

                var dataDocuments = new List<BsonDocument>();

                foreach (var id in request.GetData)
                {
                    var document = collection.FindById(new(id));

                    if (document is null)
                    {
                        response.ValidationErrors.Add(new NotFoundValidationError(id.ToString()));

                        continue;
                    }

                    dataDocuments.Add(document);
                }

                response.Data = dataDocuments
                    .Select(x => x.ToFileObjectEntity().ToFileObjectData())
                    .ToArray();

                return TaskHelper.FromResult(response);
            },
            ct
        );
    }

    private async ValueTask UpdateCore(NeotomaPostRequest source, CancellationToken ct)
    {
        await ExecuteAsync(Guid.NewGuid(), new(), source, ct);
    }

    private Guid[] GetIdsByPattern(
        UltraLiteCollection<BsonDocument> collection,
        params string[] patterns
    )
    {
        if (patterns.Length == 0)
        {
            return [];
        }

        if (patterns.Length == 1)
        {
            return collection
                .Find(Query.StartsWith(nameof(FileObjectEntity.Path), patterns[0]))
                .Select(x => x["_id"].AsGuid)
                .ToArray();
        }

        var queries = patterns
            .SelectAsSpan(x => Query.StartsWith(nameof(FileObjectEntity.Path), x))
            .ToArray();

        var ids = collection.Find(Query.Or(queries)).Select(x => x["_id"].AsGuid).ToArray();

        return ids;
    }

    private void Create(
        UltraLiteDatabase database,
        DbServiceOptions options,
        Guid idempotentId,
        Dictionary<string, FileObject[]> creates,
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
