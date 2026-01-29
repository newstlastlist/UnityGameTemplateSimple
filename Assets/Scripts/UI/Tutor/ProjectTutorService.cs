namespace UI.Tutor
{
    // Проектно-специфичная часть (пока пустая): тутор-сценарии удалены, а геймплей будет переписан с нуля.
    public sealed class ProjectTutorService : TutorService, ITutorProjectContext
    {
        public ProjectTutorService(TutorView view) : base(view) { }

        protected override System.Collections.Generic.IReadOnlyList<ITutorScenario> CreateScenarios()
        {
            return System.Array.Empty<ITutorScenario>();
        }

        public void BlockAllColumnsExceptByTutorIds(int[] allowedTutorObjectIds)
        {
            // TODO: будет реализовано вместе с новой логикой поля/кликов.
        }

        public void UnblockAllColumns()
        {
            // TODO: будет реализовано вместе с новой логикой поля/кликов.
        }
    }
}


