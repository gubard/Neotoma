using System.Runtime.CompilerServices;
using Gaia.Helpers;
using Gaia.Models;
using Gaia.Services;
using Microsoft.Data.Sqlite;
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
    private readonly GaiaValues _gaiaValues;
    private readonly IFactory<DbServiceOptions> _factoryOptions;

    public FileStorageDbService(
        IDbConnectionFactory factory,
        GaiaValues gaiaValues,
        IFactory<DbServiceOptions> factoryOptions
    )
        : base(factory, nameof(FileObjectEntity))
    {
        _gaiaValues = gaiaValues;
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

        await session.DeleteEntitiesAsync(
            $"{_gaiaValues.UserId}",
            idempotentId,
            options.IsUseEvents,
            request.Deletes,
            ct
        );

        return response;
    }

    private ConfiguredValueTaskAwaitable CreateAsync(
        DbSession session,
        DbServiceOptions options,
        Guid idempotentId,
        FileData[] creates,
        CancellationToken ct
    )
    {
        if (creates.Length == 0)
        {
            return TaskHelper.ConfiguredCompletedTask;
        }

        var entities = new Span<FileObjectEntity>(new FileObjectEntity[creates.Length]);

        for (var index = 0; index < creates.Length; index++)
        {
            var create = creates[index];
            entities[index] = new() { Id = create.Id };
        }

        return session.AddEntitiesAsync(
            $"{_gaiaValues.UserId}",
            idempotentId,
            options.IsUseEvents,
            entities.ToArray(),
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
            var files = await session.GetFileObjectsAsync(
                new SqlQuery(
                    FileObjectsExt.SelectQuery + " WHERE Path LIKE '@Dir%'",
                    new SqliteParameter("@Dir", dir)
                ),
                ct
            );

            response.GetFiles[dir] = files.Select(x => x.ToFileData()).ToArray();
        }

        return response;
    }
}
