using Game.Assets;
using Game.Catalogs;
using Game.Display;
using Game.Hosting;
using Game.Input;
using Game.Runtime.Bootstrapping;
using Game.Runtime.Execution;
using Game.Runtime.Execution.Interaction;
using Game.Runtime.Overlays;
using Game.Runtime.Overlays.Inventory;
using Game.Runtime.Rooms.Basement;
using Game.Runtime.Rooms.Bedroom;
using Game.Runtime.Rooms.City;
using Game.Runtime.Rooms.City2;
using Game.Runtime.Rooms.FurnitureStore;
using Game.Runtime.Rooms.Garden;
using Game.Runtime.Rooms.House;
using Game.Runtime.Rooms.IngesShop;
using Game.Runtime.Rooms.Kitchen;
using Game.Runtime.Rooms.Living;
using Game.Shared.Host.Audio;
using Game.Shared.Host.Input;
using Game.Shared.Host.Rendering;
using Game.Shared.Resources.Management;
using Game.Shared.Runtime;
using Game.State;
using Game.Text;

namespace Game.Runtime;

/// <summary>
///     Owns one live game runtime instance together with the generic runtime services it consumes.
/// </summary>
internal sealed class Erbe
{
    /// <summary>
    ///     Creates the game-owned runtime graph and loads its boot data once.
    /// </summary>
    /// <param name="resources">Resource manager used to resolve files and executable images.</param>
    /// <param name="exePath">Canonical path of the EXE used for boot-data loading.</param>
    /// <param name="inputBackend">Host input backend.</param>
    /// <param name="language">Optional language overlay identifier.</param>
    /// <param name="useClassicInteractions">
    ///     True to preserve the original double-confirmation interaction flow; false to use the modernized flow.
    /// </param>
    /// <param name="dksnMode">True to enable the DKSN missing-hotspot fallback mode.</param>
    internal Erbe(GameResourceManager resources,
        string exePath,
        IInputBackend inputBackend,
        string? language,
        bool useClassicInteractions,
        bool dksnMode)
    {
        Resources = resources;
        InputBackend = inputBackend;
        Language = language;

        BootData = GameBootDataLoader.Load(resources, exePath);
        UseClassicInteractions = useClassicInteractions;
        DksnMode = dksnMode;
        Assets = new AssetCatalog();
        Strings = new GameStringCatalog(this);
        Interactions = new InteractionCatalog(this);
        Scenes = new ProgramSceneCatalog(this);
        ContentFileLoader = new ContentFileLoader(Assets, Resources);
        LbmDecoder = new LbmDecoder(State);
        FullScreenSourceSurface = new RetainedFullScreenSourceSurface(ContentFileLoader, LbmDecoder);
        DisplayCopy = new DisplayCopyController(State);
        DisplayBufferPublisher = new DisplayBufferPublisher(State);
        DisplayPrimitives = new DisplayPrimitiveController(State);
        TransitionEffect = new TransitionEffectController(State, Strings);
        ContentFiles = new ContentFilePersistenceController(Assets, Resources);
        Palette = new PaletteController(State);
        InputAdapter = new InputAdapter(State.Input);
        InteractiveRegions = new InteractiveRegionController(Interactions, State);
        TextCursor = new TextCursorController(State);
        TextRenderer = new TextRendererController(State, Strings, TextCursor);
        HostPacing = new HostPacing(State.Program, InputAdapter);
        PointerOverlay = new PointerOverlayController(DisplayBufferPublisher, HostPacing, InputAdapter, State);
        Bootstrap = new Bootstrap(this);
        PromptController = new PromptController(this);
        var entrySelectionAnimation = new EntrySelectionAnimator(this);
        LolitaHeartOutro = new LolitaHeartOutroSequence(this);
        CarTrafficJamCutscene = new CarTrafficJamCutsceneSequence(this);
        KeypadOverlay = new KeypadOverlay(this);
        LivingRoom = new LivingRoom(this);
        HouseRoom = new HouseRoom(this);
        KitchenRoom = new KitchenRoom(this);
        BasementRoom = new BasementRoom(this);
        GardenRoom = new GardenRoom(this);
        BedroomRoom = new BedroomRoom(this);
        CityRoom = new CityRoom(this);
        City2Room = new City2Room(this);
        FurnitureStore = new FurnitureStoreRoom(this);
        IngesShop = new IngesShopRoom(this);
        SaveScreenController = new SaveScreenController(this);
        var handlerRouter = new InteractionHandlerRouter(this, SaveScreenController);
        InventoryOverlay = new InventoryOverlay(this, new PhoneBookViewer(this));
        InteractionLoop = new InteractionLoop(this, PromptController, SaveScreenController, handlerRouter);
        EntrySequence = new EntrySequence(this, PromptController, entrySelectionAnimation);
        ProgramScene = new ProgramSceneController(this);
        ScreenComposer =
            new PublishedBufferScreenComposer(this, RuntimeState.FrameWidth, RuntimeState.FrameHeight);
    }

    internal Bootstrap Bootstrap { get; }
    internal ContentFileLoader ContentFileLoader { get; }
    internal ContentFilePersistenceController ContentFiles { get; }
    internal DisplayCopyController DisplayCopy { get; }
    internal DisplayBufferPublisher DisplayBufferPublisher { get; }
    internal DisplayPrimitiveController DisplayPrimitives { get; }
    internal TransitionEffectController TransitionEffect { get; }
    internal CarTrafficJamCutsceneSequence CarTrafficJamCutscene { get; }

    /// <summary>
    ///     Gets the runtime-owned DKSN fallback history for the current session.
    /// </summary>
    internal DksnFallbackState DksnFallbackState { get; } = new();

    /// <summary>
    ///     Gets the purpose-owned catalog for game assets.
    /// </summary>
    internal AssetCatalog Assets { get; }

    /// <summary>
    ///     Gets the immutable data projected at boot before runtime catalogs are constructed.
    /// </summary>
    internal GameBootData BootData { get; }

    internal EntrySequence EntrySequence { get; }
    internal RetainedFullScreenSourceSurface FullScreenSourceSurface { get; }
    internal HouseRoom HouseRoom { get; }
    internal HostPacing HostPacing { get; }

    /// <summary>
    ///     Gets a value indicating whether the game is currently paused by the hotspot blink overlay.
    /// </summary>
    internal bool IsPaused => State.Program.IsPaused;

    internal IHostMusicPlayer Music { get; private set; } = new NullHostMusicPlayer();
    internal RandomSource Random { get; } = new();
    internal IRenderBackend? RenderBackend { get; private set; }
    internal GameResourceManager Resources { get; }
    internal CityRoom CityRoom { get; }
    internal City2Room City2Room { get; }
    internal IngesShopRoom IngesShop { get; }
    internal IInputBackend InputBackend { get; }
    internal string? Language { get; }

    /// <summary>
    ///     Gets the purpose-owned interaction catalog backed by boot data and the session data block.
    /// </summary>
    internal InteractionCatalog Interactions { get; }

    internal KeypadOverlay KeypadOverlay { get; }
    internal LivingRoom LivingRoom { get; }
    internal LolitaHeartOutroSequence LolitaHeartOutro { get; }
    internal KitchenRoom KitchenRoom { get; }
    internal BasementRoom BasementRoom { get; }
    internal GardenRoom GardenRoom { get; }
    internal BedroomRoom BedroomRoom { get; }
    internal FurnitureStoreRoom FurnitureStore { get; }
    internal InventoryOverlay InventoryOverlay { get; }
    internal InputAdapter InputAdapter { get; }
    internal InteractionLoop InteractionLoop { get; }
    internal InteractiveRegionController InteractiveRegions { get; }
    internal LbmDecoder LbmDecoder { get; }
    internal PaletteController Palette { get; }
    internal PointerOverlayController PointerOverlay { get; }
    internal ProgramSceneController ProgramScene { get; }
    internal PromptController PromptController { get; }
    internal SaveScreenController SaveScreenController { get; }

    /// <summary>
    ///     Gets the purpose-owned scene catalog backed by boot data and the session data block.
    /// </summary>
    internal ProgramSceneCatalog Scenes { get; }

    internal PublishedBufferScreenComposer ScreenComposer { get; }
    internal RuntimeState State { get; } = new();

    /// <summary>
    ///     Gets a value indicating whether the classic double-confirmation interaction flow should be preserved.
    /// </summary>
    internal bool UseClassicInteractions { get; }

    /// <summary>
    ///     Gets a value indicating whether the DKSN missing-hotspot fallback mode is enabled.
    /// </summary>
    internal bool DksnMode { get; }

    /// <summary>
    ///     Gets the runtime-owned string catalog loaded during boot.
    /// </summary>
    internal GameStringCatalog Strings { get; }

    internal TextCursorController TextCursor { get; }
    internal TextRendererController TextRenderer { get; }

    /// <summary>
    ///     Attaches a render backend to the game, disposing the previously attached backend first.
    /// </summary>
    /// <param name="renderBackend">Render backend to attach.</param>
    internal void AttachRenderBackend(IRenderBackend renderBackend)
    {
        RenderBackend?.Dispose();
        RenderBackend = renderBackend;
    }

    /// <summary>
    ///     Attaches a streamed music player to the game, disposing the previous player.
    /// </summary>
    /// <param name="musicPlayer">Music player to attach.</param>
    internal void AttachMusicPlayer(IHostMusicPlayer musicPlayer)
    {
        Music.Dispose();
        Music = musicPlayer;
        Music.SetPaused(State.Program.IsPaused);
    }

    /// <summary>
    ///     Updates the current paused state and synchronizes the attached music player.
    /// </summary>
    /// <param name="paused">Whether the game should be paused.</param>
    internal void SetPaused(bool paused)
    {
        if (State.Program.IsPaused == paused)
        {
            return;
        }

        State.Program.IsPaused = paused;
        Music.SetPaused(paused);
    }
}
