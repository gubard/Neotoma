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
    private readonly DbValues _dbValues;
    private readonly IFactory<DbServiceOptions> _factoryOptions;

    public FileStorageDbService(
        IDbConnectionFactory factory,
        DbValues dbValues,
        IFactory<DbServiceOptions> factoryOptions
    )
        : base(factory, nameof(FileObjectEntity))
    {
        _dbValues = dbValues;
        _factoryOptions = factoryOptions;
    }

    public override ConfiguredValueTaskAwaitable<NeotomaGetResponse> GetAsync(
        NeotomaGetRequest request,
        CancellationToken ct
    )
    {
        return GetCore(request, ct).ConfigureAwait(false);
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

    public ConfiguredValueTaskAwaitable UpdateAsync(NeotomaPostRequest source, CancellationToken ct)
    {
        return TaskHelper.ConfiguredCompletedTask;
    }

    public ConfiguredValueTaskAwaitable UpdateAsync(NeotomaGetResponse source, CancellationToken ct)
    {
        return TaskHelper.ConfiguredCompletedTask;
    }

    private async ValueTask<NeotomaPostResponse> ExecuteCore(
        Guid idempotentId,
        NeotomaPostResponse response,
        NeotomaPostRequest request,
        CancellationToken ct
    )
    {
        await using var session = await Factory.CreateSessionAsync(ct);
        var options = _factoryOptions.Create();
        await CreateAsync(session, options, idempotentId, request.Creates, ct);
        var deleteIds = new List<Guid>(request.Deletes);

        foreach (var dir in request.DeleteDirs)
        {
            var query = new SqlQuery(
                FileObjectsExt.SelectIdsQuery + " WHERE Path LIKE @Pattern",
                session.CreateParameter("@Pattern", dir + "/%")
            );

            var ids = await session.GetGuidAsync(query, ct);
            deleteIds.AddRange(ids);
        }

        await session.DeleteEntitiesAsync(
            $"{_dbValues.UserId}",
            idempotentId,
            options.IsUseEvents,
            deleteIds.ToArray(),
            ct
        );

        await session.CommitAsync(ct);

        return response;
    }

    private ConfiguredValueTaskAwaitable CreateAsync(
        DbSession session,
        DbServiceOptions options,
        Guid idempotentId,
        Dictionary<string, FileData[]> creates,
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
            $"{_dbValues.UserId}",
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
