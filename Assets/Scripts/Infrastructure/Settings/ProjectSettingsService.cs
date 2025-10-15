using Domain.Project;

namespace Infrastructure.Settings
{
	public sealed class ProjectSettingsService : IProjectSettingsService
	{
		private readonly ProjectSettings _settings;
		public float StartupLoadingMinSeconds => _settings != null ? _settings.StartupLoadingMinSeconds : 5f;

		public ProjectSettingsService(ProjectSettings settings)
		{
			_settings = settings;
		}

	}
}
