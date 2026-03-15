public enum SeesawSide
{
    Left = -1,
    Right = 1
}

public static class SeesawSideExtensions
{
    public static int DownwardAngularSign(this SeesawSide side)
    {
        return -(int)side;
    }

    public static int UpwardAngularSign(this SeesawSide side)
    {
        return (int)side;
    }
}
