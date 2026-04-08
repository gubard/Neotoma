using Neotoma.Contract.Models;
using Nestor.Db.LiteDb.Models;

[assembly: LiteDb(typeof(FileObjectEntity), nameof(FileObjectEntity.Id), false)]
[assembly: LiteDbSourceEntity(typeof(FileObjectEntity), nameof(FileObjectEntity.Id))]
