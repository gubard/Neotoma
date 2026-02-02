namespace Neotoma.Contract.Models;

public sealed class FileObjectEntity
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public byte[] Data { get; set; } = [];
}
