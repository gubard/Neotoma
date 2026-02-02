using System.Collections.Frozen;
using Neotoma.Contract.Helpers;
using Neotoma.Contract.Models;
using Neotoma.Contract.Services;
using Nestor.Db.Helpers;
using Zeus.Helpers;

InsertHelper.AddDefaultInsert(
    nameof(FileObjectEntity),
    id => new FileObjectEntity[] { new() { Id = id } }.CreateInsertQuery()
);

var migration = new Dictionary<int, string>();

foreach (var (key, value) in SqliteMigration.Migrations)
{
    migration.Add(key, value);
}

foreach (var (key, value) in NeotomaMigration.Migrations)
{
    migration.Add(key, value);
}

await WebApplication
    .CreateBuilder(args)
    .CreateAndRunZeusApp<
        IFileStorageService,
        FileStorageDbService,
        NeotomaGetRequest,
        NeotomaPostRequest,
        NeotomaGetResponse,
        NeotomaPostResponse
    >(migration.ToFrozenDictionary(), "Neotoma");
