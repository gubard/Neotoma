using Nestor.Db.Models;

namespace Neotoma.Contract.Models;

public sealed class NeotomaPostRequest : IPostRequest
{
    public FileData[] Creates { get; set; } = [];
    public Guid[] Deletes { get; set; } = [];
    public EventEntity[] Events { get; set; } = [];
}
