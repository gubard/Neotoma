using Gaia.Models;
using Nestor.Db.Models;

namespace Neotoma.Contract.Models;

public sealed class NeotomaGetResponse : IResponse
{
    public List<ValidationError> ValidationErrors { get; }
}
