using Domain;

namespace Infrastructure
{
    public interface ILevelRepository
    {
        LevelData[] LoadAll();
        LevelData LoadById(int id);
        int Count { get; }
    }
}