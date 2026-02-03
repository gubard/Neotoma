using System.Collections.Frozen;

namespace Neotoma.Contract.Helpers;

public static class NeotomaMigration
{
    public static readonly FrozenDictionary<int, string> Migrations;

    static NeotomaMigration()
    {
        Migrations = new Dictionary<int, string>
        {
            {
                19,
                @"CREATE TABLE IF NOT EXISTS FileObjects (
    Id   TEXT PRIMARY KEY NOT NULL,
    Path TEXT NOT NULL CHECK(length(Path) <= 1000),
    Description TEXT NOT NULL CHECK(length(Description) <= 10000),
    Data BLOB NOT NULL
);"
            },
        }.ToFrozenDictionary();
    }
}
