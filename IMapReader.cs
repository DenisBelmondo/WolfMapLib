namespace Wolf3D
{
    public interface IMapReader<T>
    {
        T GetMap(int mapNum);
    }
}
