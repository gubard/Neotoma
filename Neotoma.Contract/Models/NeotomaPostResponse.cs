using Gaia.Models;
using Nestor.Db.Models;

namespace Neotoma.Contract.Models;

public sealed class NeotomaPostResponse : IPostResponse
{
    public List<ValidationError> ValidationErrors { get; } = [];
    public bool IsEventSaved { get; set; }
}
