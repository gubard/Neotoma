namespace Neotoma.Contract.Models;

public class FileData
{
    public string Path { get; set; } = string.Empty;
    public byte[] Data { get; set; } = [];
}
