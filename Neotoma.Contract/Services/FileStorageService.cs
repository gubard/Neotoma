using Gaia.Services;
using Neotoma.Contract.Models;
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

public sealed class EmptyFileStorageDbCache
    : EmptyDbCache<NeotomaPostRequest, NeotomaGetResponse>,
        IFileStorageDbCache;

public sealed class EmptyFileStorageDbService
    : EmptyDbService<
        NeotomaGetRequest,
        NeotomaPostRequest,
        NeotomaGetResponse,
        NeotomaPostResponse
    >,
        IFileStorageDbService;
