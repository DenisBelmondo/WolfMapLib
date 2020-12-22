namespace Belmondo.WolfMapLib
{
    public interface IMapReader<T>
    {
        T GetMap(int mapNum);
    }
}
