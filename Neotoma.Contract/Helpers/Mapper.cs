using Neotoma.Contract.Models;

namespace Neotoma.Contract.Helpers;

public static class Mapper
{
    public static FileObject ToFileObject(this FileObjectEntity value)
    {
        return new()
        {
            Data = value.Data,
            Description = value.Description,
            Id = value.Id,
            Hash = value.Hash,
            Name = Path.GetFileName(value.Path),
        };
    }

    public static FileObjectEntity ToFileObjectEntity(this FileObject value, string dir)
    {
        return new()
        {
            Data = value.Data,
            Description = value.Description,
            Id = value.Id,
            Path = $"{dir}/{value.Name}",
            Hash = value.Hash,
        };
    }

    public static FileObjectInfo ToFileObjectInfo(this FileObjectEntity value)
    {
        return new()
        {
            Description = value.Description,
            Id = value.Id,
            Name = Path.GetFileName(value.Path),
            Hash = value.Hash,
        };
    }

    public static FileObjectData ToFileObjectData(this FileObjectEntity value)
    {
        return new()
        {
            Id = value.Id,
            Hash = value.Hash,
            Data = value.Data,
        };
    }
}
