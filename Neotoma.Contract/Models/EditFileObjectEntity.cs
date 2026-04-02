using Gaia.Models;
using Gaia.Services;

namespace Neotoma.Contract.Models;

public sealed partial class EditFileObjectEntity
    : IStaticFactory<Guid, EditFileObjectEntity>,
        IId<Guid>
{
    public static EditFileObjectEntity Create(Guid input)
    {
        return new(input);
    }
}
