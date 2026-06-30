public interface IOutPoolResettable
{
    void OnTakenFromPool();
    void OnReturnedToPool();
}
