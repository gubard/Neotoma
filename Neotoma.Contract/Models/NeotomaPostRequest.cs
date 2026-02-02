using Nestor.Db.Models;

namespace Neotoma.Contract.Models;

public sealed class NeotomaPostRequest : IPostRequest
{
    public FileData[] SetFiles { get; set; } = [];
    public EventEntity[] Events { get; set; } = [];
}
