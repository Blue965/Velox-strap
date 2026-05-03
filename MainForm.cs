using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace VeloxStrap
{
    public partial class MainForm : Form
    {
        private List<FastFlag> allFlags = new();
        private List<FastFlag> filteredFlags = new();
        private Dictionary<string, bool> enabledFlags = new();
        private string flagsJsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VeloxStrap", "flags.json");
        private string currentCategory = "All";
        private Panel sidebar, contentArea, topBar;
        private TextBox searchBox;
        private FlowLayoutPanel flagsPanel;
        private Label titleLabel, flagCountLabel, statusLabel;
        private Color accentColor = Color.FromArgb(99, 102, 241);
        private Color accentHover = Color.FromArgb(129, 140, 248);
        private Color bgDark = Color.FromArgb(10, 10, 20);
        private Color bgCard = Color.FromArgb(18, 18, 35);
        private Color bgSidebar = Color.FromArgb(13, 13, 26);
        private Color borderColor = Color.FromArgb(40, 40, 70);
        private Color textPrimary = Color.FromArgb(240, 240, 255);
        private Color textSecondary = Color.FromArgb(140, 140, 180);
        private Button activeNavBtn = null;
        private System.Windows.Forms.Timer animTimer;
        private int glowAlpha = 0;
        private bool glowUp = true;

        public MainForm()
        {
            InitializeFlagData();
            InitializeComponent();
            LoadSavedFlags();
            RenderFlags(currentCategory);
            StartGlowAnimation();
        }

        private void StartGlowAnimation()
        {
            animTimer = new System.Windows.Forms.Timer();
            animTimer.Interval = 30;
            animTimer.Tick += (s, e) =>
            {
                if (glowUp) { glowAlpha += 3; if (glowAlpha >= 80) glowUp = false; }
                else { glowAlpha -= 3; if (glowAlpha <= 10) glowUp = true; }
                topBar?.Invalidate();
            };
            animTimer.Start();
        }

        private void InitializeFlagData()
        {
            allFlags = new List<FastFlag>
            {
                // === GRAPHICS & RENDERING ===
                new FastFlag("DFIntDebugFRMDefaults", "Enable FRM Defaults", "Graphics", "Enables Frame Rate Manager default settings.", "1"),
                new FastFlag("FFlagDebugGraphicsDisableDirect3D11", "Disable Direct3D11", "Graphics", "Forces Roblox away from DX11 rendering.", "false"),
                new FastFlag("FFlagDebugGraphicsPreferD3D11", "Prefer Direct3D11", "Graphics", "Forces Roblox to use DX11.", "true"),
                new FastFlag("FFlagDebugGraphicsPreferVulkan", "Prefer Vulkan", "Graphics", "Forces Roblox to use Vulkan renderer.", "true"),
                new FastFlag("FFlagDebugGraphicsPreferOpenGL", "Prefer OpenGL", "Graphics", "Forces Roblox to use OpenGL renderer.", "true"),
                new FastFlag("DFIntDebugFRMQualityLevelOverride", "FRM Quality Override", "Graphics", "Override FRM quality level (1-21).", "1"),
                new FastFlag("FFlagDisablePostFx", "Disable Post Effects", "Graphics", "Removes all post-processing effects.", "true"),
                new FastFlag("FFlagEnableQuickGameLaunch", "Quick Game Launch", "Graphics", "Speeds up initial game rendering.", "true"),
                new FastFlag("DFIntRenderClampRoughnessMax", "Roughness Max Clamp", "Graphics", "Max roughness clamp value.", "1000000000"),
                new FastFlag("FFlagFastGPULightCulling3", "Fast GPU Light Culling", "Graphics", "Enables optimized GPU light culling.", "true"),
                new FastFlag("FFlagNewLightAttenuation", "New Light Attenuation", "Graphics", "Use new light falloff formula.", "true"),
                new FastFlag("DFIntCullFactorPixelThresholdShadowMapHighQuality", "Shadow Cull Threshold HQ", "Graphics", "Pixel threshold for shadow map high quality.", "64"),
                new FastFlag("DFIntCullFactorPixelThresholdShadowMapLowQuality", "Shadow Cull Threshold LQ", "Graphics", "Pixel threshold for shadow map low quality.", "16"),
                new FastFlag("FFlagDebugForceMSAASamples4", "Force MSAA 4x", "Graphics", "Forces 4x MSAA anti-aliasing.", "true"),
                new FastFlag("FIntRenderShadowIntensity", "Shadow Intensity", "Graphics", "Controls shadow darkness intensity.", "75"),
                new FastFlag("FFlagRenderFixFog", "Fix Fog Rendering", "Graphics", "Fixes fog rendering artifacts.", "true"),
                new FastFlag("DFIntMaxFrameBufferSize", "Max Frame Buffer", "Graphics", "Sets max frame buffer size in MB.", "128"),
                new FastFlag("FFlagGlobalWindRendering", "Global Wind Rendering", "Graphics", "Enables global wind visual effects.", "true"),
                new FastFlag("FFlagGlobalWindActivated", "Global Wind Activated", "Graphics", "Activates global wind physics.", "true"),
                new FastFlag("FFlagRenderNoLowFrustumCulling", "Disable Low Frustum Culling", "Graphics", "Disables aggressive frustum culling.", "true"),
                new FastFlag("DFIntRenderTargetAnalyticsJobPerFrameCount", "Render Analytics Count", "Graphics", "Analytics job count per frame.", "0"),
                new FastFlag("FIntRenderLocalLightUpdatesMax", "Max Local Light Updates", "Graphics", "Maximum local lights updated per frame.", "8"),
                new FastFlag("FIntRenderLocalLightUpdatesMin", "Min Local Light Updates", "Graphics", "Minimum local lights updated per frame.", "4"),
                new FastFlag("FFlagUseSdfFonts", "SDF Fonts", "Graphics", "Use Signed Distance Field fonts for sharpness.", "true"),
                new FastFlag("FFlagDebugSkyGray", "Gray Sky Debug", "Graphics", "Sets sky to gray for debugging.", "false"),
                new FastFlag("DFIntDebugFRMQualityLevelOverride", "FRM Quality Override v2", "Graphics", "Override FRM quality (0=auto).", "0"),
                new FastFlag("FFlagRenderInitializeBindlessEnabled", "Bindless Rendering", "Graphics", "Enables bindless textures for better perf.", "true"),
                new FastFlag("FFlagTerrainArraySliceSelectEnabled", "Terrain Array Slices", "Graphics", "Better terrain texture array selection.", "true"),
                new FastFlag("DFIntTextureCompositorActiveAtlasPageCount", "Texture Atlas Page Count", "Graphics", "Number of texture atlas pages.", "4"),

                // === PERFORMANCE ===
                new FastFlag("DFIntTaskSchedulerTargetFps", "Target FPS", "Performance", "Sets the target FPS cap (0=unlimited).", "0"),
                new FastFlag("FFlagDebugCheckRenderThreading", "Debug Render Threading", "Performance", "Enable render thread debugging.", "false"),
                new FastFlag("FFlagRenderGpuTextureCompressor", "GPU Texture Compressor", "Performance", "Use GPU-accelerated texture compression.", "true"),
                new FastFlag("DFIntConnectionMTUSize", "MTU Size", "Performance", "Network MTU packet size in bytes.", "1400"),
                new FastFlag("DFIntDataSendRate", "Data Send Rate", "Performance", "Network data send rate.", "40"),
                new FastFlag("DFIntMinimalNumberOfSendAcks", "Min Send Acks", "Performance", "Minimum number of send acknowledgements.", "3"),
                new FastFlag("DFIntNumClientsIsNowASafteyBuffer", "Client Safety Buffer", "Performance", "Client connection safety buffer size.", "10"),
                new FastFlag("FFlagOptimizeNetworkTransport2", "Optimize Network Transport", "Performance", "Use optimized network transport protocol.", "true"),
                new FastFlag("DFIntMaxMissedWorldStepsRemembered", "Max Missed Steps", "Performance", "Maximum physics steps remembered.", "10"),
                new FastFlag("FFlagEnableQuickGameLaunch2", "Quick Game Launch v2", "Performance", "Further speed improvements on launch.", "true"),
                new FastFlag("DFIntPhysicsLODThreshold", "Physics LOD Threshold", "Performance", "Physics level-of-detail distance threshold.", "32"),
                new FastFlag("FFlagEnableSmoothTerrainInterpolation", "Smooth Terrain Interpolation", "Performance", "Smooth terrain height interpolation.", "true"),
                new FastFlag("DFIntSimBlockSizeX", "Sim Block Size X", "Performance", "Physics simulation block size X axis.", "128"),
                new FastFlag("DFIntSimBlockSizeY", "Sim Block Size Y", "Performance", "Physics simulation block size Y axis.", "128"),
                new FastFlag("DFIntSimBlockSizeZ", "Sim Block Size Z", "Performance", "Physics simulation block size Z axis.", "128"),
                new FastFlag("FFlagCacheStaticMeshData", "Cache Static Mesh", "Performance", "Cache static mesh data in memory.", "true"),
                new FastFlag("DFIntMaxRaycastDistance", "Max Raycast Distance", "Performance", "Maximum raycast check distance.", "5000"),
                new FastFlag("FFlagLuaGcConcurrent", "Concurrent Lua GC", "Performance", "Enable concurrent Lua garbage collection.", "true"),
                new FastFlag("DFIntLuaGcStepMultiplier", "Lua GC Step Multiplier", "Performance", "Lua GC step speed multiplier.", "200"),
                new FastFlag("FFlagV8ScriptContextUseV8Contexts", "V8 Script Contexts", "Performance", "Use V8-style script context isolation.", "true"),
                new FastFlag("DFIntHttpRbxApiServicesProxyPort", "Proxy Port Override", "Performance", "HTTP proxy port for API services.", "0"),
                new FastFlag("FFlagDebugProfileRunScript", "Debug Profile Script", "Performance", "Enable run script profiling.", "false"),
                new FastFlag("DFIntConnectionThrottlePluginEventsPerMinute", "Plugin Event Throttle", "Performance", "Rate limit plugin events per minute.", "10"),
                new FastFlag("FFlagEnableParallelLuaV2", "Parallel Lua v2", "Performance", "Enable v2 parallel Lua actors.", "true"),
                new FastFlag("DFIntParallelLuaConcurrencyLimit", "Parallel Lua Limit", "Performance", "Concurrent parallel Lua tasks limit.", "4"),

                // === NETWORK ===
                new FastFlag("DFIntRakNetBandwidthPingSendFrequencyMs", "Ping Send Frequency", "Network", "How often to send bandwidth pings in ms.", "200"),
                new FastFlag("DFIntOptimizationMaxPacketsPerStep", "Max Packets Per Step", "Network", "Max packets processed per network step.", "64"),
                new FastFlag("FFlagDebugDisableTimeoutDisconnect", "Disable Timeout Disconnect", "Network", "Prevents disconnect on network timeout.", "false"),
                new FastFlag("DFIntNetworkPredictionNumericPrecision", "Network Precision", "Network", "Numeric precision for network prediction.", "5"),
                new FastFlag("DFIntNetworkInterpolationDelay", "Interpolation Delay", "Network", "Network interpolation delay in ms.", "100"),
                new FastFlag("FFlagEnableNetworkOwnershipPacket", "Network Ownership Packets", "Network", "Enable ownership transfer packets.", "true"),
                new FastFlag("DFIntMaxDataModelSendBuffer", "Send Buffer Size", "Network", "Max data model send buffer size.", "512"),
                new FastFlag("FFlagNetworkOwnerV3", "Network Owner v3", "Network", "Use v3 network ownership protocol.", "true"),
                new FastFlag("DFIntConnectionMTUSizeMin", "Min MTU Size", "Network", "Minimum network MTU size.", "576"),
                new FastFlag("FFlagOptimizeNetworkTransport3", "Optimize Transport v3", "Network", "Third-gen network transport optimization.", "true"),
                new FastFlag("DFIntMaxNetworkPacketSize", "Max Packet Size", "Network", "Maximum allowed network packet size.", "1500"),
                new FastFlag("FFlagUseNewUdpSendBuffer", "New UDP Send Buffer", "Network", "Use optimized UDP send buffer.", "true"),
                new FastFlag("DFIntNetworkSendRateMultiplier", "Send Rate Multiplier", "Network", "Network send rate multiplier value.", "1"),
                new FastFlag("FFlagEnablePatchReplication", "Patch Replication", "Network", "Enable patch-based property replication.", "true"),
                new FastFlag("DFIntKickMessageSizeLimit", "Kick Message Limit", "Network", "Max kick message size in characters.", "256"),

                // === UI / UX ===
                new FastFlag("FFlagEnableNewNotificationService", "New Notifications", "UI", "Use new notification service UI.", "true"),
                new FastFlag("FFlagEnableNewTopBar", "New Top Bar", "UI", "Enable redesigned top bar.", "true"),
                new FastFlag("FFlagEnableTopBarBeta", "Top Bar Beta", "UI", "Enable top bar beta features.", "true"),
                new FastFlag("FFlagFixReportAbuseMenuRoactAnalytics", "Fix Report Menu", "UI", "Fix analytics in report abuse menu.", "true"),
                new FastFlag("FFlagLuaAppEnableNewFriendRequestUI", "New Friend Request UI", "UI", "Enable new friend request interface.", "true"),
                new FastFlag("FFlagNewFriendRequestUI", "Friend Request UI v2", "UI", "Version 2 friend request interface.", "true"),
                new FastFlag("FFlagEnableChatTranslation", "Chat Translation", "UI", "Enable auto-translate in chat.", "true"),
                new FastFlag("FFlagEnableChatTranslationSettingEnabled", "Chat Translation Settings", "UI", "Show chat translation settings.", "true"),
                new FastFlag("FFlagInGameMenuVarsV4StyleFixMainPageScroller", "Fix Main Page Scroller", "UI", "Fix scroll behavior on main menu page.", "true"),
                new FastFlag("FFlagEnableInGameMenuChromeABTest4", "In-Game Chrome Menu", "UI", "Enable Chrome-style in-game menu.", "true"),
                new FastFlag("FFlagUserShowGuiHideToggles", "Show GUI Hide Toggles", "UI", "Show toggles to hide GUI layers.", "true"),
                new FastFlag("FFlagDisplayFPSinGameHud", "FPS in HUD", "UI", "Display FPS counter in game HUD.", "true"),
                new FastFlag("FFlagEnableGlobalSoundscape", "Global Soundscape", "UI", "Enable global background soundscape.", "true"),
                new FastFlag("FFlagEnableCustomChatUI", "Custom Chat UI", "UI", "Enable custom chat user interface.", "true"),
                new FastFlag("FFlagEnableBubbleChat", "Bubble Chat", "UI", "Enable bubble chat above players.", "true"),
                new FastFlag("FFlagModernizeChatTranslation2", "Modernize Chat Translation", "UI", "Use modernized chat translation system.", "true"),
                new FastFlag("FFlagFixChatBubbleEmojis", "Fix Chat Bubble Emojis", "UI", "Fix emoji rendering in chat bubbles.", "true"),
                new FastFlag("FFlagEnablePlayerListRemoveDelay", "Player List Remove Delay", "UI", "Smooth removal animation in player list.", "true"),

                // === GAMEPLAY ===
                new FastFlag("FFlagUserClickToMoveSupportsAnimations", "Click-to-Move Animations", "Gameplay", "Animate click-to-move character paths.", "true"),
                new FastFlag("FFlagFixDeltaTimeInSteppedAnimation", "Fix Animation Delta Time", "Gameplay", "Fix animation delta time calculation.", "true"),
                new FastFlag("FFlagAnimatorRetargetingEnabled", "Animator Retargeting", "Gameplay", "Enable animator retargeting for rigs.", "true"),
                new FastFlag("DFIntCameraFarDistance", "Camera Far Distance", "Gameplay", "Maximum camera render distance.", "10000"),
                new FastFlag("FFlagUserCharacterLoadedFixEnabled", "Char Load Fix", "Gameplay", "Fix character loading edge cases.", "true"),
                new FastFlag("FFlagEnableAccessoryAdjustment", "Accessory Adjustment", "Gameplay", "Allow run-time accessory adjustments.", "true"),
                new FastFlag("FFlagAvatarSelfViewEnabled", "Avatar Self View", "Gameplay", "Show your own avatar in first-person.", "true"),
                new FastFlag("FFlagEnableSoundscape", "Soundscape", "Gameplay", "Enable environmental soundscape system.", "true"),
                new FastFlag("FFlagUserHandleLocalToolEquipFire", "Local Tool Fire Fix", "Gameplay", "Fix tool equip fire events locally.", "true"),
                new FastFlag("DFIntMaximumFreeFallTime", "Max Free Fall Time", "Gameplay", "Maximum time in free fall state (ms).", "5000"),
                new FastFlag("FFlagEnableJumpCooldown", "Jump Cooldown", "Gameplay", "Add cooldown between jumps.", "false"),
                new FastFlag("FFlagRunCharacterLoadedEvent", "Run CharLoaded Event", "Gameplay", "Fire CharacterLoaded on respawn.", "true"),
                new FastFlag("DFIntMinimalNumberOfPlayersForNewStyle", "Min Players New Style", "Gameplay", "Min players to trigger new style mode.", "2"),
                new FastFlag("FFlagEnablePlayerEmotes", "Player Emotes", "Gameplay", "Enable player emote system.", "true"),
                new FastFlag("FFlagEmotesMenuEnabled", "Emotes Menu", "Gameplay", "Show emotes selection menu.", "true"),
                new FastFlag("DFIntCameraFOVCap", "FOV Cap", "Gameplay", "Maximum allowed field of view.", "120"),
                new FastFlag("FFlagUserHideCharacterBehindHorizontalCamera", "Hide Char Behind Cam", "Gameplay", "Hide character when camera is horizontal.", "true"),

                // === SECURITY ===
                new FastFlag("FFlagDebugDisableTelemetryEphemeralCounter", "Disable Ephemeral Telemetry", "Security", "Disables ephemeral telemetry counters.", "true"),
                new FastFlag("FFlagDebugDisableTelemetryEphemeralStat", "Disable Telemetry Stats", "Security", "Disables telemetry stat reporting.", "true"),
                new FastFlag("FFlagDebugDisableTelemetryEventIngest", "Disable Event Telemetry", "Security", "Disables event ingestion telemetry.", "true"),
                new FastFlag("FFlagDebugDisableTelemetryPoint", "Disable Point Telemetry", "Security", "Disables point-based telemetry.", "true"),
                new FastFlag("FFlagDebugDisableTelemetryV2Counter", "Disable V2 Counter", "Security", "Disables V2 telemetry counter system.", "true"),
                new FastFlag("FFlagDebugDisableTelemetryV2Event", "Disable V2 Event", "Security", "Disables V2 telemetry event system.", "true"),
                new FastFlag("FFlagDebugDisableTelemetryV2Stat", "Disable V2 Stats", "Security", "Disables V2 telemetry stat system.", "true"),
                new FastFlag("FFlagEnforceClientSideSecurity", "Enforce Client Security", "Security", "Enable client-side security enforcement.", "true"),
                new FastFlag("FFlagPreventSpoofedRemoteEvents", "Prevent Spoofed Remotes", "Security", "Block spoofed remote event calls.", "true"),
                new FastFlag("DFIntTeleportClientTimeout", "Teleport Timeout", "Security", "Teleport operation timeout in seconds.", "30"),
                new FastFlag("FFlagEnableAssetPreloading", "Asset Preloading", "Security", "Enable safe asset preloading.", "true"),
                new FastFlag("FFlagSandboxing", "Sandboxing Mode", "Security", "Enable Lua sandboxing mode.", "true"),
                new FastFlag("FFlagEnableScriptSignatureV2", "Script Signature V2", "Security", "Enable V2 script signature validation.", "true"),

                // === AUDIO ===
                new FastFlag("FFlagSoundsUsePhysicalVelocity", "Physical Sound Velocity", "Audio", "Sounds use physical object velocity.", "true"),
                new FastFlag("DFIntAudioMaxSources", "Max Audio Sources", "Audio", "Maximum concurrent audio sources.", "256"),
                new FastFlag("FFlagEnableSpatialAudio", "Spatial Audio", "Audio", "Enable 3D positional spatial audio.", "true"),
                new FastFlag("DFIntAudioChannelCount", "Audio Channel Count", "Audio", "Number of audio mixing channels.", "64"),
                new FastFlag("FFlagAudioReverbEnabled", "Audio Reverb", "Audio", "Enable environmental audio reverb.", "true"),
                new FastFlag("DFIntAudioSampleRate", "Audio Sample Rate", "Audio", "Audio playback sample rate in Hz.", "44100"),
                new FastFlag("FFlagNewSoundSystem", "New Sound System", "Audio", "Use new Roblox audio engine.", "true"),
                new FastFlag("FFlagEnableVoiceChat", "Voice Chat", "Audio", "Enable in-game proximity voice chat.", "true"),
                new FastFlag("DFIntVoiceChatMaxBitrate", "Voice Chat Bitrate", "Audio", "Maximum voice chat bitrate in kbps.", "32"),
                new FastFlag("FFlagVoiceChatEchoCancellation", "Echo Cancellation", "Audio", "Enable voice chat echo cancellation.", "true"),
                new FastFlag("FFlagVoiceChatNoiseSuppression", "Noise Suppression", "Audio", "Enable voice chat noise suppression.", "true"),
                new FastFlag("FFlagEnableAudioDeviceSelection", "Audio Device Selection", "Audio", "Allow custom audio device selection.", "true"),

                // === PHYSICS ===
                new FastFlag("FFlagDebugSimInterfaceDisabled", "Disable Sim Interface", "Physics", "Disable simulation interface overlay.", "false"),
                new FastFlag("DFIntPhysicsUpdatesPerSec", "Physics Updates/Sec", "Physics", "Physics simulation updates per second.", "60"),
                new FastFlag("FFlagPhysicsSkipRedundantSleepSteps", "Skip Sleep Steps", "Physics", "Skip redundant sleep steps in physics.", "true"),
                new FastFlag("DFIntPhysicsAnalyticsSolverLoopCount", "Solver Loop Count", "Physics", "Physics analytics solver iterations.", "8"),
                new FastFlag("FFlagEnableSleepingPartsInsteadOfParts", "Parts Sleeping", "Physics", "Use sleeping state for idle parts.", "true"),
                new FastFlag("DFIntMaxReplicatedPartsInFrame", "Max Replicated Parts", "Physics", "Max parts replicated per network frame.", "64"),
                new FastFlag("FFlagPhysicsPacketCompression", "Packet Compression", "Physics", "Enable physics packet compression.", "true"),
                new FastFlag("DFIntInterpolationAwareRaycastMaxDistance", "Raycast Interpolation Dist", "Physics", "Max distance for interpolation-aware raycasts.", "2048"),
                new FastFlag("FFlagUseImprovedCCD", "Improved CCD", "Physics", "Use improved continuous collision detection.", "true"),
                new FastFlag("DFIntPhysicsContactRadius", "Contact Radius", "Physics", "Physics contact detection radius.", "10"),
                new FastFlag("FFlagDebugVisualizationShowDeltaDecompressor", "Show Delta Decompress", "Physics", "Visualize delta decompressor states.", "false"),
                new FastFlag("DFIntPhysicsNetworkSendInterval", "Physics Send Interval", "Physics", "Physics network send interval in ms.", "40"),

                // === EXPERIMENTAL ===
                new FastFlag("FFlagEnableHumanoidStateMachineV2", "Humanoid State Machine V2", "Experimental", "Use improved humanoid state machine.", "true"),
                new FastFlag("FFlagEnableNewAnimationSystem", "New Animation System", "Experimental", "Enable next-gen animation system.", "true"),
                new FastFlag("FFlagSimpleHumanoidPhysics", "Simple Humanoid Physics", "Experimental", "Simplified humanoid physics model.", "false"),
                new FastFlag("FFlagNewHumanoidDescriptionBeta", "New Humanoid Description", "Experimental", "Beta humanoid description system.", "true"),
                new FastFlag("FFlagEnableDeferredLighting", "Deferred Lighting", "Experimental", "Enable deferred lighting pipeline.", "true"),
                new FastFlag("FFlagDebugEnableRealtimeGI", "Realtime GI", "Experimental", "Enable realtime global illumination.", "false"),
                new FastFlag("FFlagEnableUnifiedLighting", "Unified Lighting", "Experimental", "Unified lighting calculation pass.", "true"),
                new FastFlag("FFlagEnableRaycastedShadows", "Raycasted Shadows", "Experimental", "Use raycasted shadow rendering.", "false"),
                new FastFlag("FFlagEnableOutlines", "Enable Outlines", "Experimental", "Enable outline rendering on objects.", "false"),
                new FastFlag("FFlagEnableWorldWaterShader", "Water Shader", "Experimental", "Enable next-gen water shader.", "true"),
                new FastFlag("FFlagFutureIsBrightPhase3", "Future is Bright P3", "Experimental", "Phase 3 Future is Bright lighting.", "true"),
                new FastFlag("FFlagTerrainLODEnhanced", "Enhanced Terrain LOD", "Experimental", "Enhanced terrain level-of-detail system.", "true"),
                new FastFlag("FFlagEnableSkinDeformer", "Skin Deformer", "Experimental", "Enable GPU skin mesh deformer.", "true"),
                new FastFlag("FFlagEnableLayeredClothingV3", "Layered Clothing V3", "Experimental", "Version 3 layered clothing system.", "true"),
                new FastFlag("FFlagDynamicHeadAnimationsEnabled", "Dynamic Head Anims", "Experimental", "Enable dynamic head facial animations.", "true"),
                new FastFlag("FFlagEnableFaceAnimationStreaming", "Face Anim Streaming", "Experimental", "Stream face animation data.", "true"),
                new FastFlag("FFlagEmojiAutoComplete", "Emoji Auto-Complete", "Experimental", "Auto-complete emojis in chat.", "true"),
                new FastFlag("FFlagEnableHDROutput", "HDR Output", "Experimental", "Enable HDR display output.", "false"),
            };

            enabledFlags = allFlags.ToDictionary(f => f.Key, f => false);
        }

        private void LoadSavedFlags()
        {
            try
            {
                if (File.Exists(flagsJsonPath))
                {
                    var json = File.ReadAllText(flagsJsonPath);
                    var saved = JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
                    if (saved != null)
                        foreach (var kv in saved)
                            if (enabledFlags.ContainsKey(kv.Key))
                                enabledFlags[kv.Key] = kv.Value;
                }
            }
            catch { }
        }

        private void SaveFlags()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(flagsJsonPath));
                File.WriteAllText(flagsJsonPath, JsonSerializer.Serialize(enabledFlags, new JsonSerializerOptions { WriteIndented = true }));
                SetStatus("✓ Flags saved successfully", Color.FromArgb(52, 211, 153));
            }
            catch (Exception ex)
            {
                SetStatus($"✗ Save failed: {ex.Message}", Color.FromArgb(239, 68, 68));
            }
        }

        private void ExportFlags()
        {
            try
            {
                var json = JsonSerializer.Serialize(enabledFlags, new JsonSerializerOptions { WriteIndented = true });
                using var dialog = new SaveFileDialog
                {
                    Filter = "Executable files (*.exe)|*.exe|JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "exe",
                    AddExtension = true,
                    FileName = "velox-fastflags.exe",
                    Title = "Export FastFlags as EXE",
                };

                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                File.WriteAllText(dialog.FileName, json);
                SetStatus($"✓ Exported flags to {Path.GetFileName(dialog.FileName)}", Color.FromArgb(52, 211, 153));
            }
            catch (Exception ex)
            {
                SetStatus($"✗ Export failed: {ex.Message}", Color.FromArgb(239, 68, 68));
            }
        }

        private void SetStatus(string msg, Color col)
        {
            if (statusLabel.InvokeRequired)
                statusLabel.Invoke(() => { statusLabel.Text = msg; statusLabel.ForeColor = col; });
            else { statusLabel.Text = msg; statusLabel.ForeColor = col; }

            var t = new System.Windows.Forms.Timer { Interval = 3000 };
            t.Tick += (s, e) => { statusLabel.Text = "Ready"; statusLabel.ForeColor = textSecondary; t.Stop(); t.Dispose(); };
            t.Start();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Text = "VeloxStrap — FastFlag Manager";
            this.Size = new Size(1280, 820);
            this.MinimumSize = new Size(1100, 700);
            this.BackColor = bgDark;
            this.ForeColor = textPrimary;
            this.Font = new Font("Segoe UI", 9.5f);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = true;
            this.DoubleBuffered = true;
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            // Custom title bar
            topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = bgSidebar,
            };
            topBar.Paint += TopBar_Paint;

            var closeBtn = MakeTopBarBtn("✕", Color.FromArgb(239, 68, 68));
            closeBtn.Click += (s, e) => { SaveFlags(); Application.Exit(); };
            closeBtn.Location = new Point(this.Width - 42, 12);
            closeBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            var minBtn = MakeTopBarBtn("─", Color.FromArgb(251, 191, 36));
            minBtn.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            minBtn.Location = new Point(this.Width - 84, 12);
            minBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            var logoLabel = new Label
            {
                Text = "⚡ VELOXSTRAP",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = textPrimary,
                AutoSize = true,
                Location = new Point(18, 15),
                BackColor = Color.Transparent,
            };

            var subLabel = new Label
            {
                Text = "FastFlag Manager",
                Font = new Font("Segoe UI", 8f),
                ForeColor = textSecondary,
                AutoSize = true,
                Location = new Point(132, 19),
                BackColor = Color.Transparent,
            };

            statusLabel = new Label
            {
                Text = "Ready",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = textSecondary,
                AutoSize = true,
                BackColor = Color.Transparent,
            };
            statusLabel.Location = new Point((this.Width / 2) - 60, 18);
            statusLabel.Anchor = AnchorStyles.Top;

            topBar.Controls.AddRange(new Control[] { logoLabel, subLabel, statusLabel, minBtn, closeBtn });

            // Drag support
            bool dragging = false;
            Point dragOffset = Point.Empty;
            topBar.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { dragging = true; dragOffset = e.Location; } };
            topBar.MouseMove += (s, e) => { if (dragging) Location = new Point(Location.X + e.X - dragOffset.X, Location.Y + e.Y - dragOffset.Y); };
            topBar.MouseUp += (s, e) => dragging = false;

            // Sidebar
            sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = bgSidebar,
                Padding = new Padding(0, 12, 0, 12),
            };

            var categories = new[] { "All", "Graphics", "Performance", "Network", "UI", "Gameplay", "Security", "Audio", "Physics", "Experimental" };
            var icons = new[] { "◈", "🎨", "⚡", "🌐", "🖥", "🎮", "🔒", "🔊", "⚙", "🧪" };
            int btnY = 10;
            for (int i = 0; i < categories.Length; i++)
            {
                var cat = categories[i];
                var icon = icons[i];
                var btn = new Button
                {
                    Text = $"  {icon}  {cat}",
                    FlatStyle = FlatStyle.Flat,
                    Width = 200,
                    Height = 40,
                    Location = new Point(10, btnY),
                    TextAlign = ContentAlignment.MiddleLeft,
                    ForeColor = textSecondary,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 9.5f),
                    Cursor = Cursors.Hand,
                    Tag = cat,
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(25, 99, 102, 241);
                btn.Click += NavBtn_Click;
                if (cat == "All") { SetActiveNav(btn); }
                sidebar.Controls.Add(btn);
                btnY += 45;
            }

            // Separator
            var sep = new Panel { Width = 180, Height = 1, BackColor = borderColor, Location = new Point(20, btnY + 5) };
            sidebar.Controls.Add(sep);
            btnY += 20;

            // Enable All / Disable All
            var enableAllBtn = MakeSidebarAction("Enable All", Color.FromArgb(52, 211, 153), btnY);
            enableAllBtn.Click += (s, e) =>
            {
                foreach (var f in filteredFlags) enabledFlags[f.Key] = true;
                RenderFlags(currentCategory);
                SetStatus("✓ All visible flags enabled", Color.FromArgb(52, 211, 153));
            };
            sidebar.Controls.Add(enableAllBtn);
            btnY += 45;

            var disableAllBtn = MakeSidebarAction("Disable All", Color.FromArgb(239, 68, 68), btnY);
            disableAllBtn.Click += (s, e) =>
            {
                foreach (var f in filteredFlags) enabledFlags[f.Key] = false;
                RenderFlags(currentCategory);
                SetStatus("✓ All visible flags disabled", Color.FromArgb(239, 68, 68));
            };
            sidebar.Controls.Add(disableAllBtn);
            btnY += 55;

            var saveBtn = new Button
            {
                Text = "💾  Download EXE",
                FlatStyle = FlatStyle.Flat,
                Width = 200,
                Height = 44,
                Location = new Point(10, btnY),
                BackColor = accentColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
            };
            saveBtn.FlatAppearance.BorderSize = 0;
            saveBtn.FlatAppearance.MouseOverBackColor = accentHover;
            saveBtn.Click += (s, e) => ExportFlags();
            sidebar.Controls.Add(saveBtn);

            // Version label at bottom of sidebar
            var verLabel = new Label
            {
                Text = "v1.0.0  •  200+ Flags",
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(70, 70, 110),
                AutoSize = false,
                Width = 200,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(10, sidebar.Height - 30),
                Anchor = AnchorStyles.Bottom,
            };
            sidebar.Controls.Add(verLabel);

            // Content area
            contentArea = new Panel { Dock = DockStyle.Fill, BackColor = bgDark, Padding = new Padding(20, 16, 20, 16) };

            // Search + stats bar
            var topRow = new Panel { Height = 52, Dock = DockStyle.Top, BackColor = Color.Transparent };

            searchBox = new TextBox
            {
                Width = 340,
                Height = 38,
                Location = new Point(0, 7),
                BackColor = bgCard,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10f),
                Text = "🔍  Search flags...",
                ForeColor = textSecondary,
                Padding = new Padding(10, 0, 0, 0),
            };
            RoundControl(searchBox, 8);
            searchBox.GotFocus += (s, e) => { if (searchBox.Text.StartsWith("🔍")) searchBox.Text = ""; searchBox.ForeColor = textPrimary; };
            searchBox.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(searchBox.Text)) { searchBox.Text = "🔍  Search flags..."; searchBox.ForeColor = textSecondary; } };
            searchBox.TextChanged += (s, e) => RenderFlags(currentCategory);

            flagCountLabel = new Label
            {
                Text = $"{allFlags.Count} flags",
                Font = new Font("Segoe UI", 9f),
                ForeColor = textSecondary,
                AutoSize = true,
                Location = new Point(360, 18),
            };

            topRow.Controls.AddRange(new Control[] { searchBox, flagCountLabel });
            contentArea.Controls.Add(topRow);

            // Flags panel
            flagsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0, 6, 0, 6),
            };
            contentArea.Controls.Add(flagsPanel);

            // Assemble
            this.Controls.Add(contentArea);
            this.Controls.Add(sidebar);
            this.Controls.Add(topBar);
            this.ResumeLayout(false);
        }

        private void TopBar_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            // Bottom border glow
            using var pen = new Pen(Color.FromArgb(glowAlpha, accentColor), 1.5f);
            g.DrawLine(pen, 0, topBar.Height - 1, topBar.Width, topBar.Height - 1);
        }

        private Button MakeTopBarBtn(string text, Color hoverColor)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(28, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = textSecondary,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = hoverColor;
            btn.MouseEnter += (s, e) => btn.ForeColor = Color.White;
            btn.MouseLeave += (s, e) => btn.ForeColor = textSecondary;
            return btn;
        }

        private Button MakeSidebarAction(string text, Color col, int y)
        {
            var btn = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Width = 200,
                Height = 36,
                Location = new Point(10, y),
                BackColor = Color.FromArgb(30, col.R, col.G, col.B),
                ForeColor = col,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(60, col.R, col.G, col.B);
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, col.R, col.G, col.B);
            return btn;
        }

        private void SetActiveNav(Button btn)
        {
            if (activeNavBtn != null)
            {
                activeNavBtn.BackColor = Color.Transparent;
                activeNavBtn.ForeColor = textSecondary;
            }
            activeNavBtn = btn;
            btn.BackColor = Color.FromArgb(25, accentColor.R, accentColor.G, accentColor.B);
            btn.ForeColor = accentColor;
        }

        private void NavBtn_Click(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            currentCategory = btn.Tag.ToString();
            SetActiveNav(btn);
            RenderFlags(currentCategory);
        }

        private void RenderFlags(string category)
        {
            string searchText = searchBox.Text.StartsWith("🔍") ? "" : searchBox.Text.ToLower();

            filteredFlags = allFlags
                .Where(f => (category == "All" || f.Category == category) &&
                            (string.IsNullOrEmpty(searchText) || f.Name.ToLower().Contains(searchText) || f.Key.ToLower().Contains(searchText) || f.Description.ToLower().Contains(searchText)))
                .OrderBy(f => f.Name)
                .ToList();

            flagsPanel.SuspendLayout();
            flagsPanel.Controls.Clear();

            foreach (var flag in filteredFlags)
            {
                var card = CreateFlagCard(flag);
                flagsPanel.Controls.Add(card);
            }

            flagsPanel.ResumeLayout();
            flagCountLabel.Text = $"{filteredFlags.Count} of {allFlags.Count} flags";
        }

        private Panel CreateFlagCard(FastFlag flag)
        {
            bool isEnabled = enabledFlags[flag.Key];

            var card = new Panel
            {
                Width = flagsPanel.Width - 30,
                Height = 82,
                BackColor = bgCard,
                Margin = new Padding(0, 0, 0, 8),
                Cursor = Cursors.Hand,
                Tag = flag.Key,
            };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var p = new Pen(enabledFlags[flag.Key] ? Color.FromArgb(60, accentColor.R, accentColor.G, accentColor.B) : borderColor, 1);
                using var path = RoundRectPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10);
                g.DrawPath(p, path);

                if (enabledFlags[flag.Key])
                {
                    using var glow = new LinearGradientBrush(new Rectangle(0, 0, 4, card.Height), accentColor, Color.Transparent, LinearGradientMode.Horizontal);
                    g.FillRectangle(glow, 0, 10, 3, card.Height - 20);
                }
            };

            // Category badge
            var catBadge = new Label
            {
                Text = flag.Category.ToUpper(),
                Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                ForeColor = GetCategoryColor(flag.Category),
                AutoSize = true,
                Location = new Point(16, 12),
                BackColor = Color.Transparent,
            };

            // Flag name
            var nameLabel = new Label
            {
                Text = flag.Name,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = textPrimary,
                AutoSize = true,
                Location = new Point(16, 30),
                BackColor = Color.Transparent,
            };

            // Description
            var descLabel = new Label
            {
                Text = flag.Description,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = textSecondary,
                AutoSize = false,
                Width = card.Width - 180,
                Height = 20,
                Location = new Point(16, 54),
                BackColor = Color.Transparent,
            };

            // Key label
            var keyLabel = new Label
            {
                Text = flag.Key.Length > 30 ? flag.Key.Substring(0, 27) + "..." : flag.Key,
                Font = new Font("Consolas", 7.5f),
                ForeColor = Color.FromArgb(80, 80, 120),
                AutoSize = true,
                Location = new Point(card.Width - 170, 12),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };

            // Toggle switch
            var toggle = new ToggleSwitch(isEnabled, accentColor)
            {
                Location = new Point(card.Width - 68, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            toggle.Toggled += (enabled) =>
            {
                enabledFlags[flag.Key] = enabled;
                card.Invalidate();
            };

            card.Controls.AddRange(new Control[] { catBadge, nameLabel, descLabel, keyLabel, toggle });
            card.Resize += (s, e) =>
            {
                descLabel.Width = card.Width - 180;
                keyLabel.Location = new Point(card.Width - 170, 12);
                toggle.Location = new Point(card.Width - 68, 28);
            };

            return card;
        }

        private Color GetCategoryColor(string cat) => cat switch
        {
            "Graphics" => Color.FromArgb(139, 92, 246),
            "Performance" => Color.FromArgb(52, 211, 153),
            "Network" => Color.FromArgb(59, 130, 246),
            "UI" => Color.FromArgb(251, 191, 36),
            "Gameplay" => Color.FromArgb(249, 115, 22),
            "Security" => Color.FromArgb(239, 68, 68),
            "Audio" => Color.FromArgb(236, 72, 153),
            "Physics" => Color.FromArgb(20, 184, 166),
            "Experimental" => Color.FromArgb(168, 85, 247),
            _ => accentColor,
        };

        private void RoundControl(Control ctrl, int radius)
        {
            var path = RoundRectPath(new Rectangle(0, 0, ctrl.Width, ctrl.Height), radius);
            ctrl.Region = new Region(path);
        }

        private GraphicsPath RoundRectPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // ─── Toggle Switch Custom Control ───────────────────────────────────────────
    public class ToggleSwitch : Control
    {
        private bool _isOn;
        private Color _onColor;
        private bool _animating;
        private float _thumbX;
        public event Action<bool> Toggled;

        public ToggleSwitch(bool isOn, Color onColor)
        {
            _isOn = isOn;
            _onColor = onColor;
            _thumbX = isOn ? 24f : 4f;
            Size = new Size(50, 26);
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            _isOn = !_isOn;
            var timer = new System.Windows.Forms.Timer { Interval = 12 };
            timer.Tick += (s, ev) =>
            {
                float target = _isOn ? 24f : 4f;
                _thumbX += (_isOn ? 1.5f : -1.5f);
                if ((_isOn && _thumbX >= target) || (!_isOn && _thumbX <= target)) { _thumbX = target; timer.Stop(); timer.Dispose(); }
                Invalidate();
            };
            timer.Start();
            Toggled?.Invoke(_isOn);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var trackColor = _isOn ? _onColor : Color.FromArgb(50, 50, 80);
            using var trackBrush = new SolidBrush(trackColor);
            g.FillRoundedRectangle(trackBrush, 0, 0, Width, Height, Height / 2);

            using var thumbBrush = new SolidBrush(Color.White);
            g.FillEllipse(thumbBrush, _thumbX, 3, 20, 20);
        }
    }

    // ─── Data Model ─────────────────────────────────────────────────────────────
    public class FastFlag
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string DefaultValue { get; set; }

        public FastFlag(string key, string name, string category, string desc, string defaultVal)
        {
            Key = key; Name = name; Category = category; Description = desc; DefaultValue = defaultVal;
        }
    }

    // ─── GDI+ Extensions ────────────────────────────────────────────────────────
    public static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics g, Brush brush, float x, float y, float width, float height, float radius)
        {
            using var path = new GraphicsPath();
            path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
            path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
            path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            g.FillPath(brush, path);
        }
    }
}
