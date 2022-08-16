public abstract class Singleton<T> where T : class, new()
{
    private static readonly T SingleInstance = new T();

    public static T Instance
    {
        get
        {
            return SingleInstance;
        }
    }
}