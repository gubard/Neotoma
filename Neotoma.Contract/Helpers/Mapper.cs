using Neotoma.Contract.Models;

namespace Neotoma.Contract.Helpers;

public static class Mapper
{
    public static FileData ToFileData(this FileObjectEntity entity)
    {
        return new()
        {
            Data = entity.Data,
            Description = entity.Description,
            Id = entity.Id,
            Name = Path.GetFileName(entity.Path),
        };
    }

    public static FileObjectEntity ToFileObjectEntity(this FileData data, string dir)
    {
        return new()
        {
            Data = data.Data,
            Description = data.Description,
            Id = data.Id,
            Path = Path.Combine(dir, data.Name),
        };
    }
}
