using Gaia.Models;
using Nestor.Db.Models;

namespace Neotoma.Contract.Models;

public sealed class NeotomaGetResponse : IResponse
{
    public Dictionary<string, FileData[]> GetFiles { get; } = [];
    public List<ValidationError> ValidationErrors { get; } = [];
}
