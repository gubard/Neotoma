using System.Runtime.CompilerServices;
using Gaia.Services;
using Neotoma.Contract.Models;
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
    public FileStorageDbService(IDbConnectionFactory factory)
        : base(factory, nameof(FileObjectEntity)) { }

    public override ConfiguredValueTaskAwaitable<NeotomaGetResponse> GetAsync(
        NeotomaGetRequest request,
        CancellationToken ct
    )
    {
        throw new NotImplementedException();
    }

    protected override ConfiguredValueTaskAwaitable<NeotomaPostResponse> ExecuteAsync(
        Guid idempotentId,
        NeotomaPostResponse response,
        NeotomaPostRequest request,
        CancellationToken ct
    )
    {
        throw new NotImplementedException();
    }

    public ConfiguredValueTaskAwaitable UpdateAsync(NeotomaPostRequest source, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public ConfiguredValueTaskAwaitable UpdateAsync(NeotomaGetResponse source, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
