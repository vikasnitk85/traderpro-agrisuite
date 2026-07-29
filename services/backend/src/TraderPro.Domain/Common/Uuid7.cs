namespace TraderPro.Domain.Common;

public static class Uuid7
{
    public static Guid NewGuid()
    {
        return Guid.CreateVersion7();
    }
}
