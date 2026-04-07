using Gaia.Models;
using Nestor.Db.Models;

namespace Neotoma.Contract.Models;

public sealed class NeotomaGetResponse : IResponse
{
    public Dictionary<string, FileObjectInfo[]> Info { get; set; } = [];
    public FileObjectData[] Data { get; set; } = [];
    public Dictionary<string, FileObject[]> All { get; set; } = [];
    public List<ValidationError> ValidationErrors { get; set; } = [];
}
