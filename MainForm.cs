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
        // Enhanced color palette with modern gradients
        private Color accentColor = Color.FromArgb(124, 58, 237);
        private Color accentHover = Color.FromArgb(167, 139, 250);
        private Color accentGlow = Color.FromArgb(196, 181, 253);
        private Color bgDark = Color.FromArgb(3, 7, 18);
        private Color bgCard = Color.FromArgb(17, 24, 39);
        private Color bgCardHover = Color.FromArgb(30, 41, 59);
        private Color bgSidebar = Color.FromArgb(15, 23, 42);
        private Color borderColor = Color.FromArgb(51, 65, 85);
        private Color textPrimary = Color.FromArgb(248, 250, 252);
        private Color textSecondary = Color.FromArgb(148, 163, 184);
        private Color successColor = Color.FromArgb(34, 197, 94);
        private Color warningColor = Color.FromArgb(251, 146, 60);
        private Color dangerColor = Color.FromArgb(239, 68, 68);
        
        // Animation properties
        private System.Windows.Forms.Timer cardAnimationTimer;
        private List<Panel> animatedCards = new();
        private float pulseAlpha = 0;
        private bool pulseUp = true;
        private Button activeNavBtn = null;
        private System.Windows.Forms.Timer animTimer;
        private System.Windows.Forms.Timer pulseTimer;
        private int glowAlpha = 0;
        private bool glowUp = true;
        private float backgroundOffset = 0;

        public MainForm()
        {
            // Initialize components first
            InitializeComponent();
            InitializeFlagData();
            LoadSavedFlags();
            RenderFlags(currentCategory);
            
            // Setup form properties
            this.ShowInTaskbar = true;
            this.WindowState = FormWindowState.Normal;
            this.Show();
            this.BringToFront();
            this.Activate();
            
            // Start advanced animations after form is shown
            StartGlowAnimation();
            StartPulseAnimation();
            StartCardAnimations();
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

        private void StartPulseAnimation()
        {
            pulseTimer = new System.Windows.Forms.Timer();
            pulseTimer.Interval = 50;
            pulseTimer.Tick += (s, e) =>
            {
                if (pulseUp) { pulseAlpha += 2; if (pulseAlpha >= 30) pulseUp = false; }
                else { pulseAlpha -= 2; if (pulseAlpha <= 5) pulseUp = true; }
                
                // Animate background offset for parallax effect
                backgroundOffset += 0.5f;
                if (backgroundOffset > 360) backgroundOffset = 0;
                
                // Invalidate animated cards
                foreach (var card in animatedCards)
                    card?.Invalidate();
            };
            pulseTimer.Start();
        }

        private void StartCardAnimations()
        {
            cardAnimationTimer = new System.Windows.Forms.Timer();
            cardAnimationTimer.Interval = 100;
            cardAnimationTimer.Tick += (s, e) =>
            {
                // Add subtle floating animation to cards
                var time = Environment.TickCount / 1000.0f;
                foreach (var card in animatedCards)
                {
                    if (card != null)
                    {
                        var floatOffset = (float)Math.Sin(time + card.Tag.GetHashCode() % 10) * 2;
                        card.Top = (int)floatOffset + card.Top;
                    }
                }
            };
            cardAnimationTimer.Start();
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
            catch (Exception ex)
            {
                // Log the error but don't crash the application
                System.Diagnostics.Debug.WriteLine($"Failed to load saved flags: {ex.Message}");
                SetStatus("⚠ Failed to load saved flags", Color.FromArgb(251, 191, 36));
            }
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
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json",
                    AddExtension = true,
                    FileName = "velox-fastflags.json",
                    Title = "Export FastFlags as JSON",
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
            try
            {
                this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                // Use default icon if extraction fails
            }

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
            closeBtn.Location = new Point(1280 - 42, 12);
            closeBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            var minBtn = MakeTopBarBtn("─", Color.FromArgb(251, 191, 36));
            minBtn.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            minBtn.Location = new Point(1280 - 84, 12);
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
            statusLabel.Location = new Point(640 - 60, 18);
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
                Text = "💾  Export JSON",
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
            
            // Create gradient background
            using var gradientBrush = new LinearGradientBrush(
                new Rectangle(0, 0, topBar.Width, topBar.Height),
                Color.FromArgb(15, 23, 42),
                Color.FromArgb(30, 41, 59),
                LinearGradientMode.Vertical);
            
            // Add animated gradient overlay
            using var overlayBrush = new LinearGradientBrush(
                new Rectangle(0, 0, topBar.Width, topBar.Height),
                Color.FromArgb((int)pulseAlpha, accentColor),
                Color.Transparent,
                LinearGradientMode.Horizontal);
            
            g.FillRectangle(gradientBrush, 0, 0, topBar.Width, topBar.Height);
            g.FillRectangle(overlayBrush, 0, 0, topBar.Width, topBar.Height);
            
            // Enhanced bottom border with glow effect
            using var glowPen = new Pen(Color.FromArgb(glowAlpha, accentColor), 2f);
            using var shadowPen = new Pen(Color.FromArgb(glowAlpha / 2, accentColor), 4f);
            
            // Draw shadow
            g.DrawLine(shadowPen, 0, topBar.Height, topBar.Width, topBar.Height);
            // Draw main glow
            g.DrawLine(glowPen, 0, topBar.Height - 1, topBar.Width, topBar.Height - 1);
            
            // Add subtle corner accents
            using var accentBrush = new SolidBrush(Color.FromArgb((int)(pulseAlpha / 2), accentGlow));
            g.FillEllipse(accentBrush, 10, 10, 4, 4);
            g.FillEllipse(accentBrush, topBar.Width - 14, 10, 4, 4);
        }

        private Button MakeTopBarBtn(string text, Color hoverColor)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(32, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = textSecondary,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(2),
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, hoverColor.R, hoverColor.G, hoverColor.B);
            
            // Enhanced hover effects
            btn.MouseEnter += (s, e) => {
                btn.ForeColor = Color.White;
                btn.BackColor = Color.FromArgb(60, hoverColor.R, hoverColor.G, hoverColor.B);
                btn.Invalidate();
            };
            btn.MouseLeave += (s, e) => {
                btn.ForeColor = textSecondary;
                btn.BackColor = Color.Transparent;
                btn.Invalidate();
            };
            
            // Custom paint for rounded corners and glow
            btn.Paint += (s, e) => {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                
                if (btn.BackColor != Color.Transparent)
                {
                    // Draw glow effect
                    using var glowBrush = new SolidBrush(Color.FromArgb(20, hoverColor));
                    g.FillEllipse(glowBrush, -2, -2, btn.Width + 4, btn.Height + 4);
                }
                
                // Draw rounded background
                using var path = new GraphicsPath();
                path.AddEllipse(0, 0, btn.Width, btn.Height);
                using var brush = new SolidBrush(btn.BackColor);
                g.FillPath(brush, path);
                
                // Draw border
                using var pen = new Pen(Color.FromArgb((int)(pulseAlpha / 3), hoverColor), 1f);
                g.DrawPath(pen, path);
            };
            
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
                Height = 92,
                BackColor = bgCard,
                Margin = new Padding(0, 0, 0, 12),
                Cursor = Cursors.Hand,
                Tag = flag.Key,
            };
            
            // Add to animated cards list
            animatedCards.Add(card);
            
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                
                // Create gradient background
                var categoryColor = GetCategoryColor(flag.Category);
                using var bgGradient = new LinearGradientBrush(
                    new Rectangle(0, 0, card.Width, card.Height),
                    bgCard,
                    isEnabled ? Color.FromArgb(30, categoryColor.R, categoryColor.G, categoryColor.B) : bgCardHover,
                    LinearGradientMode.Vertical);
                
                using var path = RoundRectPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 12);
                g.FillPath(bgGradient, path);
                
                // Enhanced border with glow effect
                if (isEnabled)
                {
                    // Multi-layer glow effect
                    using var glowPen1 = new Pen(Color.FromArgb((int)pulseAlpha, categoryColor), 3f);
                    using var glowPen2 = new Pen(Color.FromArgb((int)(pulseAlpha / 2), categoryColor), 2f);
                    using var mainPen = new Pen(Color.FromArgb(100, categoryColor.R, categoryColor.G, categoryColor.B), 1.5f);
                    
                    g.DrawPath(glowPen1, path);
                    g.DrawPath(glowPen2, path);
                    g.DrawPath(mainPen, path);
                    
                    // Animated side glow
                    using var sideGlow = new LinearGradientBrush(
                        new Rectangle(0, 0, 6, card.Height),
                        Color.FromArgb((int)pulseAlpha, categoryColor),
                        Color.Transparent,
                        LinearGradientMode.Horizontal);
                    g.FillRectangle(sideGlow, 0, 15, 4, card.Height - 30);
                }
                else
                {
                    using var borderPen = new Pen(Color.FromArgb(80, borderColor.R, borderColor.G, borderColor.B), 1f);
                    g.DrawPath(borderPen, path);
                }
                
                // Add subtle corner highlight
                using var highlightBrush = new SolidBrush(Color.FromArgb((int)(pulseAlpha / 4), Color.White));
                g.FillEllipse(highlightBrush, 8, 8, 2, 2);
            };
            
            // Enhanced hover effects
            card.MouseEnter += (s, e) => {
                card.BackColor = bgCardHover;
                card.Invalidate();
            };
            card.MouseLeave += (s, e) => {
                card.BackColor = bgCard;
                card.Invalidate();
            };

            // Enhanced category badge with glow
            var catBadge = new Label
            {
                Text = flag.Category.ToUpper(),
                Font = new Font("Segoe UI", 6.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Width = 70,
                Height = 18,
                Location = new Point(16, 10),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
            };
            catBadge.Paint += (s, e) => {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var catColor = GetCategoryColor(flag.Category);
                
                // Rounded background with gradient
                using var bgBrush = new LinearGradientBrush(
                    new Rectangle(0, 0, catBadge.Width, catBadge.Height),
                    Color.FromArgb(200, catColor.R, catColor.G, catColor.B),
                    catColor,
                    LinearGradientMode.Vertical);
                using var path = RoundRectPath(new Rectangle(0, 0, catBadge.Width - 1, catBadge.Height - 1), 9);
                g.FillPath(bgBrush, path);
                
                // Glow effect
                using var glowPen = new Pen(Color.FromArgb((int)(pulseAlpha / 2), catColor), 1f);
                g.DrawPath(glowPen, path);
                
                // Text
                var textBrush = new SolidBrush(Color.White);
                g.DrawString(catBadge.Text, catBadge.Font, textBrush, 4, 2);
            };

            // Enhanced flag name with better typography
            var nameLabel = new Label
            {
                Text = flag.Name,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = textPrimary,
                AutoSize = true,
                Location = new Point(16, 32),
                BackColor = Color.Transparent,
            };

            // Enhanced description with subtle gradient
            var descLabel = new Label
            {
                Text = flag.Description,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = textSecondary,
                AutoSize = false,
                Width = card.Width - 180,
                Height = 22,
                Location = new Point(16, 58),
                BackColor = Color.Transparent,
            };

            // Enhanced key label with modern styling
            var keyLabel = new Label
            {
                Text = flag.Key.Length > 30 ? flag.Key.Substring(0, 27) + "..." : flag.Key,
                Font = new Font("Consolas", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 100, 140),
                AutoSize = false,
                Width = 160,
                Height = 16,
                Location = new Point(card.Width - 170, 10),
                BackColor = Color.FromArgb(20, 30, 50),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleCenter,
            };
            keyLabel.Paint += (s, e) => {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                
                // Background with rounded corners
                using var bgBrush = new SolidBrush(Color.FromArgb(20, 30, 50));
                using var path = RoundRectPath(new Rectangle(0, 0, keyLabel.Width - 1, keyLabel.Height - 1), 8);
                g.FillPath(bgBrush, path);
                
                // Border
                using var borderPen = new Pen(Color.FromArgb(40, 60, 90), 1f);
                g.DrawPath(borderPen, path);
                
                // Text
                var textBrush = new SolidBrush(keyLabel.ForeColor);
                g.DrawString(keyLabel.Text, keyLabel.Font, textBrush, 4, 1);
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
            _thumbX = isOn ? 26f : 4f;
            Size = new Size(56, 30);
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            _animating = false;
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (_animating) return;
            
            _isOn = !_isOn;
            _animating = true;
            
            var timer = new System.Windows.Forms.Timer { Interval = 8 };
            timer.Tick += (s, ev) =>
            {
                float target = _isOn ? 26f : 4f;
                float speed = 2.0f;
                
                if (_isOn && _thumbX < target)
                {
                    _thumbX = Math.Min(_thumbX + speed, target);
                }
                else if (!_isOn && _thumbX > target)
                {
                    _thumbX = Math.Max(_thumbX - speed, target);
                }
                
                if (_thumbX == target)
                {
                    timer.Stop();
                    timer.Dispose();
                    _animating = false;
                }
                
                Invalidate();
            };
            timer.Start();
            Toggled?.Invoke(_isOn);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            // Enhanced track with gradient and glow
            var trackColor = _isOn ? _onColor : Color.FromArgb(60, 60, 90);
            
            using var trackGradient = new LinearGradientBrush(
                new Rectangle(0, 0, Width, Height),
                _isOn ? Color.FromArgb(120, _onColor.R, _onColor.G, _onColor.B) : Color.FromArgb(40, 40, 60),
                trackColor,
                LinearGradientMode.Vertical);
            
            // Draw track with rounded corners
            g.FillRoundedRectangle(trackGradient, 0, 0, Width, Height, Height / 2);
            
            // Add glow effect when on
            if (_isOn)
            {
                using var glowBrush = new SolidBrush(Color.FromArgb(30, _onColor.R, _onColor.G, _onColor.B));
                g.FillRoundedRectangle(glowBrush, -2, -2, Width + 4, Height + 4, Height / 2 + 2);
            }
            
            // Enhanced thumb with shadow and gradient
            var thumbSize = 22;
            var thumbY = (Height - thumbSize) / 2;
            
            // Draw shadow
            using var shadowBrush = new SolidBrush(Color.FromArgb(20, 0, 0, 0));
            g.FillEllipse(shadowBrush, _thumbX + 1, thumbY + 2, thumbSize, thumbSize);
            
            // Draw thumb with gradient
            using var thumbGradient = new LinearGradientBrush(
                new Rectangle((int)_thumbX, thumbY, thumbSize, thumbSize),
                Color.White,
                Color.FromArgb(240, 240, 240),
                LinearGradientMode.Vertical);
            g.FillEllipse(thumbGradient, (int)_thumbX, thumbY, thumbSize, thumbSize);
            
            // Add inner highlight
            using var highlightBrush = new SolidBrush(Color.FromArgb(100, 255, 255, 255));
            g.FillEllipse(highlightBrush, (int)_thumbX + 4, thumbY + 4, 6, 6);
        }
        
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            // Add hover effect
            Invalidate();
        }
        
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            // Remove hover effect
            Invalidate();
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
