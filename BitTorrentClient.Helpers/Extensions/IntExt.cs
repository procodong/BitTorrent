namespace BitTorrentClient.Helpers.Extensions;

public static class IntExt
{
    public static int DivWithRemainder(this int dividend, int divisor)
    {
        return dividend / divisor + dividend % divisor;
    }
}