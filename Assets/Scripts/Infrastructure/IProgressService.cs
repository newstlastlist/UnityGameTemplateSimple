namespace Infrastructure
{
    public interface IProgressService
    {
        int LastCompletedLevelIndex { get; set; } 
        int LoopLevelIndex { get; set; }  
        void Save();
        void Load();
        void Reset();
        int GetWinStreak();
        void SetWinStreak(int value);
        void IncrementWinStreak();
        void ResetWinStreak();
        void ClearAllPersistedData();
        int ResolveCurrentLevelIndex(int levelsCount);
        void OnLevelCompleted(int levelsCount, int completedIndex);
    }
}