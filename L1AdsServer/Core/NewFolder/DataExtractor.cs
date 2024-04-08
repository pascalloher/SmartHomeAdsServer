using System.Text.RegularExpressions;

namespace L1AdsServer.Core.NewFolder;

public class VariableInfo
{
    public string Name { get; }
    public string Board { get; }
    public string Floor { get; }
    public string Number { get; }

    public VariableInfo(string name, string board, string floor, string number)
    {
        Name = name;
        Board = board;
        Floor = floor;
        Number = number;
    }
}

public interface IDataExtractor
{
    string CreateVariableName(string id, string entity, out bool firstAccess, out VariableInfo info);
    bool Exists(string id);
}

public class DataExtractor: IDataExtractor
{
    private readonly Dictionary<string, VariableInfo> _variables = new Dictionary<string, VariableInfo>();

    bool IDataExtractor.Exists(string id)
    {
        return _variables.ContainsKey(id);
    }

    string IDataExtractor.CreateVariableName(string id, string entity, out bool firstAccess, out VariableInfo info)
    {
        if(_variables.TryGetValue(id, out VariableInfo? i))
        {
            firstAccess = false;
            info = i;
        }
        else
        {
            firstAccess = true;
            info = ExtractInfo(id);
            _variables.Add(id, info);
        }

        return $"GVL_{info.Board}.{info.Floor}{entity}[{info.Number}]";
    }

    private static VariableInfo ExtractInfo(string id)
    {
        Regex r = new Regex("(?<Board>[A-Z]+)_(?<Floor>[A-Z]+)(?<Number>[0-9]+)");
        var m = r.Match(id);
        if (!m.Success)
            throw new ArgumentException("Failed to extract Info of passed string", id);
        return new VariableInfo(
            id,
            m.Groups["Board"].Value.ToUpperInvariant(),
            string.Concat(m.Groups["Floor"].Value[0].ToString().ToUpperInvariant(), m.Groups["Floor"].Value[1..].ToLowerInvariant()),
            m.Groups["Number"].Value);
    }
}

