using Nestor.Db.Models;

namespace Neotoma.Contract.Models;

public sealed class NeotomaPostRequest : IPostRequest
{
    public Dictionary<string, FileData[]> Creates { get; set; } = [];
    public Guid[] Deletes { get; set; } = [];
    public string[] DeleteDirs { get; set; } = [];
    public EventEntity[] Events { get; set; } = [];
}
