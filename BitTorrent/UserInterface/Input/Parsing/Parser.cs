using BitTorrentClient.UserInterface.Input.Exceptions;

namespace BitTorrentClient.UserInterface.Input.Parsing;
public class Parser(string input)
{
    private readonly string _input = input;
    private int _index;

    public int ParseInteger()
    {
        ClearWhiteSpace();
        int start = _index;
        while (_index < _input.Length && _input[_index] >= '0' && _input[_index] <= '9')
        {
            _index++;
        }
        return int.Parse(_input.AsSpan(start.._index));
    }

    public ReadOnlySpan<char> ParseString()
    {
        ClearWhiteSpace();
        if (_input[_index] != '"')
        {
            throw new InvalidTokenException(_input[_index], _index);
        }
        _index++;
        int start = _index;
        while (_index < _input.Length && _input[_index] != '"')
        {
            _index++;
        }
        var end = _index;
        _index++;
        return _input.AsSpan(start..end);
    }

    public ReadOnlySpan<char> ParseIdentifier()
    {
        ClearWhiteSpace();
        int start = _index;
        while (_index < _input.Length && _input[_index] != ' ')
        {
            _index++;
        }
        return _input.AsSpan(start.._index);
    }

    private void ClearWhiteSpace()
    {
        while (_input[_index] == ' ') _index++;
    }
}
