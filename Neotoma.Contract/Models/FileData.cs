namespace Neotoma.Contract.Models;

public sealed class FileData
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public byte[] Data { get; set; } = [];
}
