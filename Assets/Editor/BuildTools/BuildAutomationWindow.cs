using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Editor.BuildTools
{
    public sealed class BuildAutomationWindow : EditorWindow
    {
        private const string DefaultOutputDirectoryRelativePath = "Builds/Android";
        private const string DefaultFileNameTemplate = "{product}_{profile}.{ext}";
        private const string ScriptRelativePath = "Tools/Build/BuildWithLibraryProfile.ps1";
        private const string LibraryProfileMarkerFileName = ".library_profile";

        private string _outputDirectoryRelativePath = DefaultOutputDirectoryRelativePath;
        private string _fileNameTemplate = DefaultFileNameTemplate;
        // Консоль/батч-билды больше не используем: билд идёт в GUI редактора.
        private string _newVersion;
        private int _newBundleVersionCode;
        private bool _isNewVersionDefaultsInitialized;
        private string _releaseKeystorePassword;
        private string _releaseKeyAliasPassword;
        private bool _showReleasePasswords;
        private bool _isReleaseSecretsInitialized;
        private Vector2 _scrollPosition;
        private bool _showAdvanced;
        private bool _useAggressiveUnlock = true;

        private void OnGUI()
        {
            EnsureNewVersionDefaultsInitialized();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Build Automation (Library Profiles)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Сценарий:\n" +
                "- Если текущий кэш уже нужного профиля (Dev/Release) — билд идёт прямо в открытом Unity.\n" +
                "- Если нужно переключить кэш — Unity закроется, тулза переключит Library, Unity откроется и автоматически запустит билд в GUI.\n",
                MessageType.Info);

            DrawStatusSection();
            DrawVersionSection();
            DrawReleaseSigningSecretsSection();
            DrawOutputSection();
            DrawBuildButtonsSection();
            DrawNotesSection();

            EditorGUILayout.EndScrollView();
        }

        [MenuItem("Tools/Build/Build Automation...")]
        public static void Open()
        {
            BuildAutomationWindow window = GetWindow<BuildAutomationWindow>("Build Automation");
            window.minSize = new Vector2(680f, 560f);
        }

        private void DrawStatusSection()
        {
            string projectRootPath = GetProjectRootPath();

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

            string scriptFullPath = Path.Combine(projectRootPath, ScriptRelativePath);
            bool isScriptExists = File.Exists(scriptFullPath);

            string currentProfile = GetCurrentLibraryProfile(projectRootPath);
            string libraryPath = Path.Combine(projectRootPath, "Library");
            string devLibraryPath = Path.Combine(projectRootPath, "Library_Dev");
            string releaseLibraryPath = Path.Combine(projectRootPath, "Library_Release");
            string inferredProfile = string.IsNullOrWhiteSpace(currentProfile) ? GetLibraryProfileHintFromLibraryFolder(libraryPath) : string.Empty;

            EditorGUILayout.LabelField("Project", projectRootPath);
            EditorGUILayout.LabelField("Script", isScriptExists ? "OK" : "NOT FOUND");
            if (!string.IsNullOrWhiteSpace(currentProfile))
            {
                EditorGUILayout.LabelField("Current Library Profile", currentProfile);
            }
            else if (!string.IsNullOrWhiteSpace(inferredProfile))
            {
                EditorGUILayout.LabelField("Current Library Profile", $"{inferredProfile} (inferred)");
            }
            else
            {
                EditorGUILayout.LabelField("Current Library Profile", "(unknown)");
            }
            EditorGUILayout.LabelField("Library", Directory.Exists(libraryPath) ? "exists" : "missing");
            EditorGUILayout.LabelField("Library_Dev", Directory.Exists(devLibraryPath) ? "exists" : "missing");
            EditorGUILayout.LabelField("Library_Release", Directory.Exists(releaseLibraryPath) ? "exists" : "missing");

            if (BuildAutomationStateStore.TryReadLastBuild(out string lastBuildProfile, out string lastBuildArtifact, out DateTime lastBuildUtc))
            {
                string lastBuildTimeText = lastBuildUtc == DateTime.MinValue ? string.Empty : $" ({lastBuildUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss})";
                string lastBuildArtifactText = lastBuildArtifact.Equals("Apk", StringComparison.OrdinalIgnoreCase) ? "APK" : lastBuildArtifact.ToUpperInvariant();
                EditorGUILayout.LabelField("Last build", $"{lastBuildProfile} {lastBuildArtifactText}{lastBuildTimeText}");
            }
            else
            {
                EditorGUILayout.LabelField("Last build", "(none yet)");
            }

            if (!isScriptExists)
            {
                EditorGUILayout.HelpBox($"Не найден PowerShell-скрипт: `{ScriptRelativePath}`", MessageType.Error);
            }
        }

        private void DrawReleaseSigningSecretsSection()
        {
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Release Signing (local encrypted secrets)", EditorStyles.boldLabel);

            bool hasStoredSecrets = BuildSecretsStore.HasStoredSecrets();
            bool canLoadSecrets = BuildSecretsStore.TryLoadReleaseSigningPasswords(out string storedKeystorePassword, out string storedKeyAliasPassword);
            EnsureReleaseSecretsInitialized(hasStoredSecrets, canLoadSecrets, storedKeystorePassword, storedKeyAliasPassword);

            if (!hasStoredSecrets)
            {
                EditorGUILayout.HelpBox(
                    "Локальные секреты НЕ найдены.\n" +
                    "Введи Keystore Pass / KeyAlias Pass и нажми Save/Update secrets.",
                    MessageType.Error);
            }
            else if (!canLoadSecrets)
            {
                EditorGUILayout.HelpBox(
                    "Локальные секреты найдены, но не удалось расшифровать/прочитать.\n" +
                    "Возможно файл повреждён или был создан под другим пользователем Windows.\n" +
                    "Нажми Clear secrets и задай заново.",
                    MessageType.Error);
            }

            EditorGUILayout.HelpBox(
                "Можно хранить Release-пароли локально в зашифрованном файле (Windows DPAPI, CurrentUser).\n" +
                "Файл лежит в UserSettings/ и не коммитится в git.",
                MessageType.None);

            _showReleasePasswords = EditorGUILayout.ToggleLeft("Show passwords (опасно при шаринге экрана)", _showReleasePasswords);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Keystore Pass", GUILayout.Width(160f));
                _releaseKeystorePassword = _showReleasePasswords
                    ? EditorGUILayout.TextField(_releaseKeystorePassword)
                    : EditorGUILayout.PasswordField(_releaseKeystorePassword);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("KeyAlias Pass", GUILayout.Width(160f));
                _releaseKeyAliasPassword = _showReleasePasswords
                    ? EditorGUILayout.TextField(_releaseKeyAliasPassword)
                    : EditorGUILayout.PasswordField(_releaseKeyAliasPassword);
            }

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Load stored secrets"))
                {
                    if (canLoadSecrets)
                    {
                        _releaseKeystorePassword = storedKeystorePassword;
                        _releaseKeyAliasPassword = storedKeyAliasPassword;
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Build Automation", hasStoredSecrets ? "Не удалось расшифровать/прочитать секреты." : "Секреты не найдены.", "OK");
                    }
                }

                if (GUILayout.Button("Save/Update secrets"))
                {
                    try
                    {
                        BuildSecretsStore.SaveReleaseSigningPasswords(_releaseKeystorePassword, _releaseKeyAliasPassword);
                        EditorUtility.DisplayDialog("Build Automation", "Секреты сохранены локально (UserSettings/, encrypted).", "OK");
                    }
                    catch (Exception exception)
                    {
                        EditorUtility.DisplayDialog("Build Automation", $"Не удалось сохранить секреты:\n{exception.Message}", "OK");
                    }
                }

                if (GUILayout.Button("Clear secrets"))
                {
                    BuildSecretsStore.ClearStoredSecrets();
                    _releaseKeystorePassword = string.Empty;
                    _releaseKeyAliasPassword = string.Empty;
                    EditorUtility.DisplayDialog("Build Automation", "Локальные секреты удалены.", "OK");
                }
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Stored secrets", hasStoredSecrets ? (canLoadSecrets ? "present (decrypt ok)" : "present (decrypt failed)") : "missing");
        }

        private void DrawVersionSection()
        {
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Version", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Current (PlayerSettings)", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Version", GUILayout.Width(160f));
                EditorGUILayout.LabelField(PlayerSettings.bundleVersion);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Bundle Version Code", GUILayout.Width(160f));
                EditorGUILayout.LabelField(PlayerSettings.Android.bundleVersionCode.ToString());
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("New values (applied automatically on build)", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Version", GUILayout.Width(160f));
                _newVersion = EditorGUILayout.TextField(_newVersion);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Bundle Version Code", GUILayout.Width(160f));
                _newBundleVersionCode = EditorGUILayout.IntField(_newBundleVersionCode);
            }
        }

        private void DrawOutputSection()
        {
            string projectRootPath = GetProjectRootPath();

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                _outputDirectoryRelativePath = EditorGUILayout.TextField("Output Directory (relative)", _outputDirectoryRelativePath);
                if (GUILayout.Button("Browse...", GUILayout.Width(90f)))
                {
                    string selectedFolderPath = EditorUtility.OpenFolderPanel("Select output folder", projectRootPath, string.Empty);
                    if (!string.IsNullOrWhiteSpace(selectedFolderPath))
                    {
                        _outputDirectoryRelativePath = MakeRelativePathSafe(projectRootPath, selectedFolderPath);
                    }
                }
            }

            _fileNameTemplate = EditorGUILayout.TextField("File Name Template", _fileNameTemplate);

            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(
                "Шаблон имени файла поддерживает токены:\n" +
                "- {product}: PlayerSettings.productName\n" +
                "- {version}: PlayerSettings.bundleVersion\n" +
                "- {profile}: dev|release\n" +
                "- {artifact}: apk|aab\n" +
                "- {ext}: apk|aab\n" +
                "- {date}: yyyyMMdd_HHmmss\n\n" +
                "Пример: {product}_{version}_{profile}_{date}.{ext}",
                MessageType.None);

            string outputDirectoryRelativePath = string.IsNullOrWhiteSpace(_outputDirectoryRelativePath)
                ? DefaultOutputDirectoryRelativePath
                : _outputDirectoryRelativePath.Trim();
            string outputDirectoryFullPath = Path.GetFullPath(Path.Combine(projectRootPath, outputDirectoryRelativePath));

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Preview (computed output paths)", EditorStyles.boldLabel);
            DrawOutputPreviewLine(projectRootPath, outputDirectoryFullPath, BuildProfile.Dev, BuildArtifact.Apk);
            DrawOutputPreviewLine(projectRootPath, outputDirectoryFullPath, BuildProfile.Release, BuildArtifact.Aab);

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Output Folder"))
                {
                    EditorUtility.RevealInFinder(outputDirectoryFullPath);
                }

                if (GUILayout.Button("Copy Output Folder Path"))
                {
                    EditorGUIUtility.systemCopyBuffer = outputDirectoryFullPath;
                }

                if (GUILayout.Button("Reset Defaults"))
                {
                    _outputDirectoryRelativePath = DefaultOutputDirectoryRelativePath;
                    _fileNameTemplate = DefaultFileNameTemplate;
                }
            }
        }

        private void DrawBuildButtonsSection()
        {
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Build", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                bool isDevRestartRequired = IsRestartRequiredForProfile("Dev");
                string devLabel = isDevRestartRequired ? "Build Dev APK (Debug) (restart required)" : "Build Dev APK (Debug)";
                if (GUILayout.Button(devLabel, GUILayout.Height(36f)))
                {
                    StartDevApkBuildSmart();
                }
            }

            EditorGUILayout.Space(6f);

            using (new EditorGUILayout.HorizontalScope())
            {
                bool isReleaseRestartRequired = IsRestartRequiredForProfile("Release");
                string releaseLabel = isReleaseRestartRequired ? "Build Release AAB (restart required)" : "Build Release AAB";
                if (GUILayout.Button(releaseLabel, GUILayout.Height(36f)))
                {
                    StartReleaseAabBuildSmart();
                }
            }

            EditorGUILayout.Space(10f);
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced");
            if (_showAdvanced)
            {
                _useAggressiveUnlock = EditorGUILayout.ToggleLeft("Aggressive unlock on Access Denied (restart Explorer / stop WSearch)", _useAggressiveUnlock);
                EditorGUILayout.HelpBox(
                    "Опция нужна только если свитч Library периодически падает с 'Отказано в доступе'.\n" +
                    "Включение может кратко перезапустить explorer.exe (панель задач/проводник) и/или остановить службу Windows Search.\n" +
                    "По умолчанию выключено.",
                    MessageType.Warning);

                if (GUILayout.Button("Force switch cache to Dev (restart) + GUI build Dev APK", GUILayout.Height(28f)))
                {
                    StartSwitchCacheAndGuiBuild(BuildProfile.Dev, BuildArtifact.Apk);
                }

                if (GUILayout.Button("Force switch cache to Release (restart) + GUI build Release AAB", GUILayout.Height(28f)))
                {
                    StartSwitchCacheAndGuiBuild(BuildProfile.Release, BuildArtifact.Aab);
                }
            }
        }

        private void StartSwitchCacheAndGuiBuild(BuildProfile buildProfile, BuildArtifact buildArtifact)
        {
            string projectRootPath = GetProjectRootPath();
            string scriptFullPath = Path.Combine(projectRootPath, ScriptRelativePath);
            if (!File.Exists(scriptFullPath))
            {
                EditorUtility.DisplayDialog("Build Automation", $"Не найден скрипт: {scriptFullPath}", "OK");
                return;
            }

            if (!EditorSceneManager.SaveOpenScenes())
            {
                EditorUtility.DisplayDialog("Build Automation", "Сохранение сцен отменено. Билд не запущен.", "OK");
                return;
            }

            ApplyPendingVersionOverridesIfAny();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string unityEditorPath = EditorApplication.applicationPath;
            string outputDirectoryRelativePath = string.IsNullOrWhiteSpace(_outputDirectoryRelativePath)
                ? DefaultOutputDirectoryRelativePath
                : _outputDirectoryRelativePath.Trim();
            string outputDirectoryFullPath = Path.GetFullPath(Path.Combine(projectRootPath, outputDirectoryRelativePath));
            Directory.CreateDirectory(outputDirectoryFullPath);

            string outputFileName = BuildOutputFileName(buildProfile, buildArtifact);
            string outputFileFullPath = Path.Combine(outputDirectoryFullPath, outputFileName);

            int unityProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;

            string arguments =
                "-NoProfile -ExecutionPolicy Bypass " +
                $"-File \"{scriptFullPath}\" " +
                $"-ProjectPath \"{projectRootPath}\" " +
                $"-UnityPath \"{unityEditorPath}\" " +
                $"-Profile \"{buildProfile}\" " +
                $"-Artifact \"{buildArtifact}\" " +
                $"-OutputPath \"{outputFileFullPath}\" " +
                $"-EditorPid {unityProcessId}";

            if (_useAggressiveUnlock)
            {
                arguments += " -AggressiveUnlock";
            }

            try
            {
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = arguments,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WorkingDirectory = projectRootPath
                };

                System.Diagnostics.Process.Start(startInfo);
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog("Build Automation", $"Не удалось запустить PowerShell:\n{exception}", "OK");
                return;
            }

            EditorApplication.Exit(0);
        }

        private void StartDevApkBuildSmart()
        {
            if (IsCurrentLibraryProfile("Dev"))
            {
                StartInEditorDevApkBuild();
                return;
            }

            StartSwitchCacheAndGuiBuild(BuildProfile.Dev, BuildArtifact.Apk);
        }

        private void StartReleaseAabBuildSmart()
        {
            if (IsCurrentLibraryProfile("Release"))
            {
                StartInEditorReleaseAabBuild();
                return;
            }

            StartSwitchCacheAndGuiBuild(BuildProfile.Release, BuildArtifact.Aab);
        }

        private bool IsCurrentLibraryProfile(string expectedProfile)
        {
            string projectRootPath = GetProjectRootPath();
            string currentProfile = GetCurrentLibraryProfile(projectRootPath);
            if (string.IsNullOrWhiteSpace(currentProfile))
            {
                string libraryPath = Path.Combine(projectRootPath, "Library");
                currentProfile = GetLibraryProfileHintFromLibraryFolder(libraryPath);
            }

            return string.Equals(currentProfile, expectedProfile, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsRestartRequiredForProfile(string expectedProfile)
        {
            string projectRootPath = GetProjectRootPath();
            string currentProfile = GetCurrentLibraryProfile(projectRootPath);
            if (string.IsNullOrWhiteSpace(currentProfile))
            {
                string libraryPath = Path.Combine(projectRootPath, "Library");
                currentProfile = GetLibraryProfileHintFromLibraryFolder(libraryPath);
            }

            if (string.IsNullOrWhiteSpace(currentProfile))
            {
                return false;
            }

            return !string.Equals(currentProfile, expectedProfile, StringComparison.OrdinalIgnoreCase);
        }

        private static void DrawNotesSection()
        {
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Notes", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("1) Первый билд профиля может быть долгим (создание кэша).");
            EditorGUILayout.LabelField("2) При переключении профиля Dev↔Release Unity будет перезапущена (это нужно для безопасной подмены Library).");
            EditorGUILayout.LabelField("3) Dev сборки подписываются debug keystore автоматически.");
            EditorGUILayout.LabelField("4) Release сборки требуют local encrypted secrets (см. Release Signing).");
        }

        private static string GetProjectRootPath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private void StartInEditorDevApkBuild()
        {
            if (EditorApplication.isCompiling)
            {
                EditorUtility.DisplayDialog("Build Automation", "Идёт компиляция скриптов. Подожди окончания.", "OK");
                return;
            }

            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Build Automation", "Останови Play Mode перед билдом.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveOpenScenes())
            {
                EditorUtility.DisplayDialog("Build Automation", "Сохранение сцен отменено. Билд не запущен.", "OK");
                return;
            }

            ApplyPendingVersionOverridesIfAny();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string projectRootPath = GetProjectRootPath();
            string outputDirectoryRelativePath = string.IsNullOrWhiteSpace(_outputDirectoryRelativePath)
                ? DefaultOutputDirectoryRelativePath
                : _outputDirectoryRelativePath.Trim();
            string outputDirectoryFullPath = Path.GetFullPath(Path.Combine(projectRootPath, outputDirectoryRelativePath));
            Directory.CreateDirectory(outputDirectoryFullPath);

            string outputFileName = BuildOutputFileName(BuildProfile.Dev, BuildArtifact.Apk);
            string outputFileFullPath = Path.Combine(outputDirectoryFullPath, outputFileName);

            string[] enabledScenePaths = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (enabledScenePaths.Length <= 0)
            {
                EditorUtility.DisplayDialog("Build Automation", "Нет включённых сцен в Build Settings.", "OK");
                return;
            }

            try
            {
                WriteProfileMarker(projectRootPath, "Dev");
                WriteLibraryProfileHint(projectRootPath, "Dev");

                BuildGuiEntryPoint.RequestBuildFromEditor("Dev", "Apk", outputFileFullPath);
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog("Build Automation", $"Dev APK build exception:\n{exception}", "OK");
            }
        }

        private void StartInEditorReleaseAabBuild()
        {
            if (EditorApplication.isCompiling)
            {
                EditorUtility.DisplayDialog("Build Automation", "Идёт компиляция скриптов. Подожди окончания.", "OK");
                return;
            }

            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Build Automation", "Останови Play Mode перед билдом.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveOpenScenes())
            {
                EditorUtility.DisplayDialog("Build Automation", "Сохранение сцен отменено. Билд не запущен.", "OK");
                return;
            }

            ApplyPendingVersionOverridesIfAny();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string projectRootPath = GetProjectRootPath();
            string outputDirectoryRelativePath = string.IsNullOrWhiteSpace(_outputDirectoryRelativePath)
                ? DefaultOutputDirectoryRelativePath
                : _outputDirectoryRelativePath.Trim();
            string outputDirectoryFullPath = Path.GetFullPath(Path.Combine(projectRootPath, outputDirectoryRelativePath));
            Directory.CreateDirectory(outputDirectoryFullPath);

            string outputFileName = BuildOutputFileName(BuildProfile.Release, BuildArtifact.Aab);
            string outputFileFullPath = Path.Combine(outputDirectoryFullPath, outputFileName);

            string[] enabledScenePaths = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (enabledScenePaths.Length <= 0)
            {
                EditorUtility.DisplayDialog("Build Automation", "Нет включённых сцен в Build Settings.", "OK");
                return;
            }

            if (!BuildSecretsStore.TryLoadReleaseSigningPasswords(out string keystorePassword, out string keyAliasPassword))
            {
                EditorUtility.DisplayDialog("Build Automation", "Не найдены local encrypted secrets для Release-подписи. Задай их в секции Release Signing.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(PlayerSettings.Android.keystoreName) || string.IsNullOrWhiteSpace(PlayerSettings.Android.keyaliasName))
            {
                EditorUtility.DisplayDialog("Build Automation", "Не задан keystore/alias в Player Settings (Publishing Settings).", "OK");
                return;
            }

            try
            {
                WriteProfileMarker(projectRootPath, "Release");
                WriteLibraryProfileHint(projectRootPath, "Release");

                BuildGuiEntryPoint.RequestBuildFromEditor("Release", "Aab", outputFileFullPath);
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog("Build Automation", $"Release AAB build exception:\n{exception}", "OK");
            }
        }

        private void ApplyPendingVersionOverridesIfAny()
        {
            bool isAnyChangeRequested = !string.IsNullOrWhiteSpace(_newVersion) || _newBundleVersionCode > 0;
            if (!isAnyChangeRequested)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(_newVersion))
            {
                PlayerSettings.bundleVersion = _newVersion.Trim();
            }

            if (_newBundleVersionCode > 0)
            {
                PlayerSettings.Android.bundleVersionCode = _newBundleVersionCode;
            }
        }

        private void EnsureNewVersionDefaultsInitialized()
        {
            if (_isNewVersionDefaultsInitialized)
            {
                return;
            }

            _newVersion = PlayerSettings.bundleVersion;
            _newBundleVersionCode = PlayerSettings.Android.bundleVersionCode;
            _isNewVersionDefaultsInitialized = true;
        }

        private void EnsureReleaseSecretsInitialized(bool hasStoredSecrets, bool canLoadSecrets, string storedKeystorePassword, string storedKeyAliasPassword)
        {
            if (_isReleaseSecretsInitialized)
            {
                return;
            }

            if (hasStoredSecrets && canLoadSecrets)
            {
                // Авто-подгрузка, чтобы не забывать нажимать Load каждый раз.
                _releaseKeystorePassword = storedKeystorePassword;
                _releaseKeyAliasPassword = storedKeyAliasPassword;
            }

            _isReleaseSecretsInitialized = true;
        }

        private void DrawOutputPreviewLine(string projectRootPath, string outputDirectoryFullPath, BuildProfile buildProfile, BuildArtifact buildArtifact)
        {
            string fileName = BuildOutputFileName(buildProfile, buildArtifact);
            string fullPath = Path.Combine(outputDirectoryFullPath, fileName);
            string relativePath = MakeRelativePathSafe(projectRootPath, fullPath);
            EditorGUILayout.LabelField($"{buildProfile} {buildArtifact}", relativePath);
        }

        private static string MakeRelativePathSafe(string projectRootPath, string fullPath)
        {
            try
            {
                Uri projectUri = new Uri(projectRootPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                    ? projectRootPath
                    : projectRootPath + Path.DirectorySeparatorChar);
                Uri fullUri = new Uri(fullPath);
                string relativePath = Uri.UnescapeDataString(projectUri.MakeRelativeUri(fullUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
                return relativePath;
            }
            catch
            {
                return fullPath;
            }
        }

        private string BuildOutputFileName(BuildProfile buildProfile, BuildArtifact buildArtifact)
        {
            string template = string.IsNullOrWhiteSpace(_fileNameTemplate) ? DefaultFileNameTemplate : _fileNameTemplate.Trim();

            string productName = PlayerSettings.productName;
            string version = PlayerSettings.bundleVersion;
            string profileToken = buildProfile == BuildProfile.Dev ? "dev" : "release";
            string artifactToken = buildArtifact == BuildArtifact.Apk ? "apk" : "aab";
            string extension = artifactToken;
            string dateToken = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            string fileName = template
                .Replace("{product}", SanitizeFileNamePart(productName))
                .Replace("{version}", SanitizeFileNamePart(version))
                .Replace("{profile}", profileToken)
                .Replace("{artifact}", artifactToken)
                .Replace("{ext}", extension)
                .Replace("{date}", dateToken);

            if (!fileName.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) &&
                !fileName.EndsWith(".aab", StringComparison.OrdinalIgnoreCase))
            {
                fileName = fileName + "." + extension;
            }

            return fileName;
        }

        private static string SanitizeFileNamePart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unnamed";
            }

            StringBuilder stringBuilder = new StringBuilder(value.Length);
            foreach (char character in value)
            {
                if (character == Path.DirectorySeparatorChar || character == Path.AltDirectorySeparatorChar)
                {
                    stringBuilder.Append('_');
                    continue;
                }

                if (Array.IndexOf(Path.GetInvalidFileNameChars(), character) >= 0)
                {
                    stringBuilder.Append('_');
                    continue;
                }

                stringBuilder.Append(character);
            }

            return stringBuilder.ToString();
        }

        private static string GetCurrentLibraryProfile(string projectRootPath)
        {
            string markerPath = Path.Combine(projectRootPath, LibraryProfileMarkerFileName);
            if (!File.Exists(markerPath))
            {
                return string.Empty;
            }

            try
            {
                return File.ReadAllText(markerPath).Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetLibraryProfileHintFromLibraryFolder(string libraryPath)
        {
            try
            {
                string hintFilePath = Path.Combine(libraryPath, "BuildAutomationProfile.txt");
                if (!File.Exists(hintFilePath))
                {
                    return string.Empty;
                }

                string value = File.ReadAllText(hintFilePath).Trim();
                return value == "Dev" || value == "Release" ? value : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void WriteProfileMarker(string projectRootPath, string profileValue)
        {
            try
            {
                string markerPath = Path.Combine(projectRootPath, LibraryProfileMarkerFileName);
                File.WriteAllText(markerPath, profileValue);
            }
            catch
            {
                // Best-effort.
            }
        }

        private static void WriteLibraryProfileHint(string projectRootPath, string profileValue)
        {
            try
            {
                string libraryPath = Path.Combine(projectRootPath, "Library");
                if (!Directory.Exists(libraryPath))
                {
                    return;
                }

                string hintFilePath = Path.Combine(libraryPath, "BuildAutomationProfile.txt");
                File.WriteAllText(hintFilePath, profileValue);
            }
            catch
            {
                // Best-effort.
            }
        }

        private enum BuildProfile
        {
            Dev = 0,
            Release = 1
        }

        private enum BuildArtifact
        {
            Apk = 0,
            Aab = 1
        }
    }
}
