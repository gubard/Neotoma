using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Gaia.Models;

namespace Neotoma.Contract.Models;

[JsonSerializable(typeof(NeotomaGetRequest))]
[JsonSerializable(typeof(NeotomaPostRequest))]
[JsonSerializable(typeof(NeotomaGetResponse))]
[JsonSerializable(typeof(NeotomaPostResponse))]
[JsonSerializable(typeof(FileData))]
[JsonSerializable(typeof(AlreadyExistsValidationError))]
[JsonSerializable(typeof(NotFoundValidationError))]
public sealed partial class NeotomaJsonContext : JsonSerializerContext
{
    public static readonly IJsonTypeInfoResolver Resolver;

    static NeotomaJsonContext()
    {
        Resolver = Default.WithAddedModifier(typeInfo =>
        {
            if (typeInfo.Type == typeof(ValidationError))
            {
                typeInfo.PolymorphismOptions = new()
                {
                    TypeDiscriminatorPropertyName = "$type",
                    DerivedTypes =
                    {
                        new(
                            typeof(AlreadyExistsValidationError),
                            typeof(AlreadyExistsValidationError).FullName!
                        ),
                        new(
                            typeof(NotFoundValidationError),
                            typeof(NotFoundValidationError).FullName!
                        ),
                    },
                };
            }
        });
    }
}
