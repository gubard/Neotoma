using System.Runtime.CompilerServices;
using Gaia.Helpers;
using Gaia.Models;
using Gaia.Services;
using Neotoma.Contract.Helpers;
using Neotoma.Contract.Models;
using Nestor.Db.Helpers;
using Nestor.Db.Models;
using Nestor.Db.Services;

namespace Neotoma.Contract.Services;

public interface IFileStorageService
    : IService<NeotomaGetRequest, NeotomaPostRequest, NeotomaGetResponse, NeotomaPostResponse>;

public interface IFileStorageHttpService
    : IFileStorageService,
        IHttpService<
            NeotomaGetRequest,
            NeotomaPostRequest,
            NeotomaGetResponse,
            NeotomaPostResponse
        >;

public interface IFileStorageDbService
    : IFileStorageService,
        IDbService<NeotomaGetRequest, NeotomaPostRequest, NeotomaGetResponse, NeotomaPostResponse>;

public interface IFileStorageDbCache : IDbCache<NeotomaPostRequest, NeotomaGetResponse>;

public sealed class FileStorageDbService
    : DbService<NeotomaGetRequest, NeotomaPostRequest, NeotomaGetResponse, NeotomaPostResponse>,
        IFileStorageDbService,
        IFileStorageDbCache
{
    public FileStorageDbService(
        IDbConnectionFactory factory,
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

    protected override ConfiguredValueTaskAwaitable<NeotomaPostResponse> ExecuteAsync(
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

    private async ValueTask UpdateCore(NeotomaPostRequest source, CancellationToken ct)
    {
        var dbValues = _dbValuesFactory.Create();
        var userId = dbValues.UserId.ToString();
        await using var session = await Factory.CreateSessionAsync(ct);

        await session.AddEntitiesAsync(
            userId,
            Guid.NewGuid(),
            false,
            source
                .Creates.SelectMany(x => x.Value.Select(y => y.ToFileObjectEntity(x.Key)))
                .ToArray(),
            ct
        );

        await session.DeleteEntitiesAsync(userId, Guid.NewGuid(), false, source.Deletes, ct);

        foreach (var dir in source.DeleteDirs)
        {
            var ids = await GetIdsByPatternAsync(session, dir + "/%", ct);
            await session.DeleteEntitiesAsync(userId, Guid.NewGuid(), false, ids, ct);
        }

        await session.CommitAsync(ct);
    }

    private async ValueTask UpdateCore(NeotomaGetResponse source, CancellationToken ct)
    {
        await using var session = await Factory.CreateSessionAsync(ct);

        foreach (var getFile in source.GetFiles)
        {
            var dbIds = await GetIdsByPatternAsync(session, getFile.Key + "/%", ct);
            var entities = getFile.Value.Select(x => x.ToFileObjectEntity(getFile.Key)).ToArray();
            var requestIds = entities.Select(x => x.Id).ToArray();
            var deleteIds = dbIds.Except(requestIds).ToArray();
            var exists = await session.IsExistsAsync(entities, ct);
            var inserts = entities.Where(x => !exists.Contains(x.Id)).ToArray();

            var updateQueries = entities
                .Where(x => exists.Contains(x.Id))
                .Select(x => x.CreateUpdateFileObjectsQuery(session))
                .ToArray();

            if (inserts.Length != 0)
            {
                await session.ExecuteNonQueryAsync(inserts.CreateInsertQuery(session), ct);
            }

            foreach (var query in updateQueries)
            {
                await session.ExecuteNonQueryAsync(query, ct);
            }

            if (deleteIds.Length != 0)
            {
                await session.ExecuteNonQueryAsync(
                    deleteIds.CreateDeleteFileObjectsQuery(session),
                    ct
                );
            }
        }

        await session.CommitAsync(ct);
    }

    private async ValueTask<NeotomaPostResponse> ExecuteCore(
        Guid idempotentId,
        NeotomaPostResponse response,
        NeotomaPostRequest request,
        CancellationToken ct
    )
    {
        var dbValues = _dbValuesFactory.Create();
        await using var session = await Factory.CreateSessionAsync(ct);
        var options = _factoryOptions.Create();
        await CreateAsync(session, options, idempotentId, request.Creates, dbValues, ct);
        var deleteIds = new List<Guid>(request.Deletes);

        foreach (var dir in request.DeleteDirs)
        {
            var ids = await GetIdsByPatternAsync(session, dir + "/%", ct);
            deleteIds.AddRange(ids);
        }

        await session.DeleteEntitiesAsync(
            $"{dbValues.UserId}",
            idempotentId,
            options.IsUseEvents,
            deleteIds.ToArray(),
            ct
        );

        await session.CommitAsync(ct);

        return response;
    }

    private async ValueTask<Guid[]> GetIdsByPatternAsync(
        DbSession session,
        string pattern,
        CancellationToken ct
    )
    {
        var query = new SqlQuery(
            FileObjectsExt.SelectIdsQuery + " WHERE Path LIKE @Pattern",
            session.CreateParameter("@Pattern", pattern)
        );

        var ids = await session.GetGuidAsync(query, ct);

        return ids;
    }

    private ConfiguredValueTaskAwaitable CreateAsync(
        DbSession session,
        DbServiceOptions options,
        Guid idempotentId,
        Dictionary<string, FileData[]> creates,
        DbValues dbValues,
        CancellationToken ct
    )
    {
        if (creates.Count == 0)
        {
            return TaskHelper.ConfiguredCompletedTask;
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

        return session.AddEntitiesAsync(
            $"{dbValues.UserId}",
            idempotentId,
            options.IsUseEvents,
            entities,
            ct
        );
    }

    private async ValueTask<NeotomaGetResponse> GetCore(
        NeotomaGetRequest request,
        CancellationToken ct
    )
    {
        await using var session = await Factory.CreateSessionAsync(ct);
        var response = new NeotomaGetResponse();

        foreach (var dir in request.GetFiles)
        {
            var query = new SqlQuery(
                FileObjectsExt.SelectQuery + " WHERE Path LIKE @Pattern",
                session.CreateParameter("@Pattern", dir + "/%")
            );

            var files = await session.GetFileObjectsAsync(query, ct);
            response.GetFiles[dir] = files.Select(x => x.ToFileData()).ToArray();
        }

        return response;
    }
}
