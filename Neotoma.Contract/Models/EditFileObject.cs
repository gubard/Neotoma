namespace Neotoma.Contract.Models;

public sealed class EditFileObject
{
    public Guid[] Ids { get; set; } = [];
    public bool IsEditDescription { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsEditPath { get; set; }
    public string Path { get; set; } = string.Empty;
    public bool IsEditHash { get; set; }
    public string Hash { get; set; } = string.Empty;
    public bool IsEditData { get; set; }
    public byte[] Data { get; set; } = [];
}
