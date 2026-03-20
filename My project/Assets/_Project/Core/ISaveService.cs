namespace MergeSurvivor.Core
{
    public interface ISaveService
    {
        void Save<T>(string key, T data);
        bool TryLoad<T>(string key, out T data) where T : new();
    }
}
