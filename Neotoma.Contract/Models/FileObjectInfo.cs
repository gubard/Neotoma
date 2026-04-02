namespace Neotoma.Contract.Models;

public sealed class FileObjectInfo
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
}
