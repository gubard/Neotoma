using System.Collections.Frozen;
using System.Security.Cryptography;
using Gaia.Helpers;
using Gaia.Services;
using Neotoma.Contract.Helpers;
using Neotoma.Contract.Models;
using Neotoma.Contract.Services;
using Neotoma.Db.Services;
using Neotoma.Services;
using Nestor.Db.Helpers;
using Zeus.Helpers;
using Zeus.Services;

InsertHelper.AddDefaultInsert(
    nameof(FileObjectEntity),
    i => new FileObjectEntity[] { new() { Id = i } }.CreateInsertQuery()
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

foreach (var (key, value) in IdempotenceMigration.Migrations)
{
    migration.Add(key, value);
}

const string name = "Neotoma";

await WebApplication
    .CreateBuilder(args)
    .CreateAndRunZeusApp<
        IFileStorageService,
        FileStorageLiteDbService,
        NeotomaGetRequest,
        NeotomaPostRequest,
        NeotomaGetResponse,
        NeotomaPostResponse
    >(
        migration.ToFrozenDictionary(),
        name,
        builder =>
            builder
                .Services.AddSingleton(NeotomaJsonContext.Default.Options)
                .AddTransient<IHashService<byte[], string>, BytesToStringHashService>()
                .AddTransient<ITransformer<byte[], string>, BytesToBase64>()
                .AddTransient<IHashService<byte[], byte[]>, Sha512HashService>()
                .AddSingleton(_ => SHA512.Create())
                .AddHostedService(sp => new CheckHashBackgroundService(
                    sp.GetRequiredService<IStorageService>().GetDbDirectory().Combine(name),
                    sp.GetRequiredService<GuidDatabaseFactory>(),
                    sp.GetRequiredService<ILogger<CheckHashBackgroundService>>(),
                    sp.GetRequiredService<IHashService<byte[], string>>()
                ))
    );
