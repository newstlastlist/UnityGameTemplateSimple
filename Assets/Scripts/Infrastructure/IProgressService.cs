namespace Infrastructure
{
    public interface IProgressService
    {
        int LastCompletedLevelIndex { get; set; } 
        int LoopLevelIndex { get; set; }  
        void Save();
        void Load();
        void Reset();
        int ResolveCurrentLevelIndex(int levelsCount);
        void OnLevelCompleted(int levelsCount, int completedIndex);
    }
}