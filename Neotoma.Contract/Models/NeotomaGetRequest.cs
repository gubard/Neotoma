namespace Neotoma.Contract.Models;

public sealed class NeotomaGetRequest
{
    public string[] GetInfo { get; set; } = [];
    public Guid[] GetData { get; set; } = [];
    public bool IsGetAll { get; set; }
}
