namespace Neotoma.Contract.Models;

public sealed class FileObjectData
{
    public Guid Id { get; set; }
    public string Hash { get; set; } = string.Empty;
    public byte[] Data { get; set; } = [];
}
