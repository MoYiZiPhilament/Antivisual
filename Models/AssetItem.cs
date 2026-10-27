namespace Antivisual.Models;

public sealed record AssetItem(string Name, string Path)
{
    public override string ToString() => Name;
}
