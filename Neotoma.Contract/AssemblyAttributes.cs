using Neotoma.Contract.Models;
using Nestor.Db.Models;

[assembly: SqliteAdo(typeof(FileObjectEntity), nameof(FileObjectEntity.Id), false)]
[assembly: SourceEntity(typeof(FileObjectEntity), nameof(FileObjectEntity.Id))]
