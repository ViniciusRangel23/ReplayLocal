param([Parameter(Mandatory)][string] $LogDirectory)
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $LogDirectory | Out-Null
# Reviewed allowlist: pure logic, synthetic files, fixture playback (muted), hidden UI. No capture engine tests.
$classes = @('ApplicationAudioReconnectTests','AudioTimelineTests','AudioFeedbackTests','AutomaticGameAudioTests','WindowsProcessIdentityTests','ApplicationAudioGateTests','AudioLayoutTests','AudioContainerTests','ApplicationAudioSettingsTests','ApplicationAudioTargetsTests','AudioRoutingTests','ApplicationAudioSessionTests','AudioSaveFinalizerTests','AudioEditTests','MultitrackExportTests','PreviewAudioMixTests','ApplicationAudioUiTests','EditorMultitrackUiTests','MultitrackIntegrationTests',
 'ReviewRegressionTests','PersonalIdentityTests','LocalExportTests','CaptureLifecycleTests','PreviewLifetimeTests','PersonalPackageTests','NoCaptionFeatureTests',
 'SettingsTests','ShareSettingsTests','SharePlannerTests','ShareViewModelTests','HotkeyTests','InstallPathsTests','VideoMathTests','ResolutionTests',
 'BufferBudgetTests','EncoderCatalogTests','EncoderPresetTests','AudioLevelTests','MeterBallisticsTests','SaveWatchTests','SaveFeedbackTests',
 'AppShellTests','AppIconTests','AppearanceTests','ClipTests','ClipLibraryTests','ClipDeleteTests','EditProjectTests','EditGraphTests',
 'EditorUiTests','EditorStillTests','RecordingTests','ScreenshotTests','LanguageTests','VersionTests','UiRefreshTests','ThemeCursorTests',
 'ThemeEverywhereTests','LevelBarTests','TakenKeyTests','NoteLengthTests','ObsLayoutTests','PipeClientTests','OffScreenWindowTests',
 'VoiceUnitTests','VoiceRoutingTests','VoiceModelTests','VoiceTriggerTests','FrameGrabberTests','ClipPlaybackTests','ClipCutterTests',
 'PreviewPlayerTests','SoundTempoTests','LookAndStyleTests','EditRenderTests','EditExportTests','ShareTranscodeTests','ReleaseAuditTests')
$failed = @()
foreach ($name in $classes) {
    try {
        & (Join-Path $PSScriptRoot 'run-safe-tests.ps1') -Filter $name *> (Join-Path $LogDirectory "$name.log")
        $summary = Get-Content -LiteralPath (Join-Path $LogDirectory "$name.log") -Tail 1
        Write-Output "$name $summary"
    } catch { $failed += $name; Write-Output "FAIL $name (see log)" }
}
if ($failed.Count) { throw "Regression classes failed: $($failed -join ', ')" }
Write-Output "All $($classes.Count) reviewed regression classes passed."
