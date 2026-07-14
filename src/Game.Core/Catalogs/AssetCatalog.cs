namespace Game.Catalogs;

/// <summary>
///     Maps executable asset identifiers to canonical game resource paths.
/// </summary>
internal sealed class AssetCatalog
{
    private readonly Dictionary<AssetId, string> _paths = new()
    {
        [AssetId.Lawyer] = "GRAFIK/ANWALT.LBM",
        [AssetId.LawyerAlternate] = "GRAFIK/ANWALT_A.LBM",
        [AssetId.Car] = "GRAFIK/AUTO.LBM",
        [AssetId.Bus] = "GRAFIK/BUS.LBM",
        [AssetId.Computer] = "GRAFIK/COMPUTER.LBM",
        [AssetId.ComputerOverlay] = "GRAFIK/COMPUT_O.LBM",
        [AssetId.DisplayBackdrop] = "GRAFIK/DISPLAY.LBM",
        [AssetId.ShopScene1] = "GRAFIK/EINKAUF1.LBM",
        [AssetId.ShopScene1Overlay] = "GRAFIK/EINK1_O.LBM",
        [AssetId.ShopScene2] = "GRAFIK/EINKAUF2.LBM",
        [AssetId.ShopScene2Overlay] = "GRAFIK/EINK2_O.LBM",
        [AssetId.OzoneScene] = "GRAFIK/OZON.LBM",
        [AssetId.BicyclePump] = "GRAFIK/FAHRRADA.LBM",
        [AssetId.FontVga] = "GRAFIK/FONT_VGA.LBM",
        [AssetId.Garden] = "GRAFIK/GARTEN.LBM",
        [AssetId.GardenOverlay] = "GRAFIK/GARTEN_O.LBM",
        [AssetId.Goebel] = "GRAFIK/GOEBEL.LBM",
        [AssetId.GoebelOverlay] = "GRAFIK/GOEBEL_O.LBM",
        [AssetId.House] = "GRAFIK/HAUS.LBM",
        [AssetId.HouseOverlay] = "GRAFIK/HAUS_O.LBM",
        [AssetId.HeroPortrait] = "GRAFIK/HELD.LBM",
        [AssetId.HeroPortraitAlternate] = "GRAFIK/HELD_S.LBM",
        [AssetId.SnackBar] = "GRAFIK/IMBISS.LBM",
        [AssetId.SnackBarOverlay] = "GRAFIK/IMBISS_O.LBM",
        [AssetId.Inge] = "GRAFIK/INGE.LBM",
        [AssetId.IngeOverlay] = "GRAFIK/INGE_O.LBM",
        [AssetId.Basement] = "GRAFIK/KELLER.LBM",
        [AssetId.BasementOverlay] = "GRAFIK/KELLER_O.LBM",
        [AssetId.OzoneAnimation] = "GRAFIK/OZON_A2.LBM",
        [AssetId.Kitchen] = "GRAFIK/KUECHE.LBM",
        [AssetId.KitchenOverlay] = "GRAFIK/KUECHE_O.LBM",
        [AssetId.OzoneIntro] = "GRAFIK/OZON_A1.LBM",
        [AssetId.SaveGame] = "ERBE.GAM",
        [AssetId.DefaultDataImage] = "ERBE.DAT",
        [AssetId.PhoneBook] = "GRAFIK/TELEBUCH.LBM",
        [AssetId.TitleScreen] = "GRAFIK/TITEL.LBM",
        [AssetId.LivingRoom] = "GRAFIK/WOHNZIMM.LBM",
        [AssetId.LivingRoomOverlay] = "GRAFIK/WOHNZI_O.LBM"
    };

    /// <summary>
    ///     Formats the file names associated with a pair of scene assets for diagnostics.
    /// </summary>
    /// <param name="firstAsset">Primary scene asset id.</param>
    /// <param name="secondAsset">Secondary scene asset id.</param>
    internal string? FormatSceneHint(AssetId firstAsset, AssetId secondAsset)
    {
        var firstFileName = TryResolveFileName(firstAsset);
        var secondFileName = TryResolveFileName(secondAsset);

        return (firstFileName, secondFileName) switch
        {
            (null, null) => null,
            (not null, null) => $"\"{firstFileName}\"",
            (null, not null) => $"\"{secondFileName}\"",
            (not null, not null) when string.Equals(firstFileName, secondFileName, StringComparison.OrdinalIgnoreCase)
                => $"\"{firstFileName}\"",
            _ => $"\"{firstFileName}\", \"{secondFileName}\""
        };
    }

    /// <summary>
    ///     Resolves a supported executable asset id to its canonical resource path.
    /// </summary>
    /// <param name="assetId">Executable asset id to resolve.</param>
    internal string ResolveOrThrow(AssetId assetId)
    {
        return _paths.TryGetValue(assetId, out var path)
            ? path
            : throw new NotImplementedException(
                $"Unsupported asset id 0x{(ushort)assetId:X4} for content-file access.");
    }

    private string? TryResolveFileName(AssetId assetId)
    {
        if (assetId == AssetId.None || !_paths.TryGetValue(assetId, out var path))
        {
            return null;
        }

        return Path.GetFileName(path);
    }
}
