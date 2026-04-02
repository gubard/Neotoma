using Nestor.Db.Models;

namespace Neotoma.Contract.Models;

public sealed class NeotomaPostRequest : IPostRequest
{
    public Dictionary<string, FileObject[]> Creates { get; set; } = [];
    public Guid[] Deletes { get; set; } = [];
    public string[] DeleteDirs { get; set; } = [];
    public EditFileObject[] Edits { get; set; } = [];
    public EventEntity[] Events { get; set; } = [];
}
