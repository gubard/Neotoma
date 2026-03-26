using Neotoma.Contract.Models;
using Nestor.Db.LiteDb.Models;
using Nestor.Db.Models;

[assembly: Ado(typeof(FileObjectEntity), nameof(FileObjectEntity.Id), false)]
[assembly: AdoSourceEntity(typeof(FileObjectEntity), nameof(FileObjectEntity.Id))]
[assembly: EditModel(typeof(FileObjectEntity), nameof(FileObjectEntity.Id))]
[assembly: LiteDb(typeof(FileObjectEntity), nameof(FileObjectEntity.Id), false)]
[assembly: LiteDbSourceEntity(typeof(FileObjectEntity), nameof(FileObjectEntity.Id))]
