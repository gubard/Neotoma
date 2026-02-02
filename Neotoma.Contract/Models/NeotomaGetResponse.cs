using Gaia.Models;
using Gaia.Services;

namespace Neotoma.Contract.Models;

public sealed class NeotomaGetResponse : IValidationErrors
{
    public Dictionary<string, FileData[]> GetFiles { get; } = [];
    public List<ValidationError> ValidationErrors { get; } = [];
}
