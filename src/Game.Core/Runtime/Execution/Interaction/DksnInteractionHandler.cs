using Game.Catalogs;
using Game.Shared.Runtime;
using Game.Text;

namespace Game.Runtime.Execution.Interaction;

/// <summary>
///     Owns the weighted DKSN fallback policy for missing scene-hotspot actions.
/// </summary>
/// <param name="runtime">Owning runtime that provides prompt animation, random selection, and session fallback state.</param>
internal sealed class DksnInteractionHandler(Erbe runtime)
{
    private static readonly byte[][] DefaultResponseLines = TextUtils.EncodeNullTerminated(
        "Das klappt so nicht.",
        "Das scheint nicht so recht zu funktionieren.",
        "Du probierst es kurz aus, aber das Ergebnis bleibt aus.",
        "Ein schöner Gedanke, aber es passiert einfach nichts.",
        "Du fummelst ein wenig daran herum, doch es tut sich nichts.",
        "Das bringt dich hier leider nicht weiter.",
        "Du versuchst es mit Überzeugung, aber ohne Erfolg.",
        "Ein interessanter Versuch, aber die Welt reagiert nicht darauf.",
        "Du lässt davon ab – das führt zu nichts.",
        "Irgendwie hast du dir das hilfreicher vorgestellt.",
        "Das scheint eine Sackgasse zu sein.",
        "Nichts bewegt sich, nichts verändert sich.",
        "Du hantierst kurz damit, aber es ergibt keinen Sinn.",
        "Deine Bemühungen verpuffen wirkungslos.",
        "Du bist dir sicher, dass das nicht die Lösung ist.",
        "Es bleibt alles so, wie es vorher war.",
        "Vielleicht wäre ein anderer Ansatz besser.",
        "Das war wohl ein Schuss in den Ofen.",
        "Du starrst es einen Moment an, aber es passiert nichts Magisches.",
        "Ein klassischer Fall von 'geht leider nicht'.",
        "Du lässt die Hände wieder sinken. Zwecklos.",
        "Die Logik dahinter erschliesst sich der Umgebung nicht.",
        "Nett gedacht, aber leider nicht machbar.",
        "Du spürst, dass du hier gerade Zeit verschwendest.",
        "Das Objekt deiner Bemühungen bleibt völlig unbeeindruckt.",
        "Keine Reaktion. Nicht mal ein leises Geräusch.",
        "Du versuchst es noch einmal, aber es bleibt beim Scheitern.",
        "Das Universum scheint heute nicht kooperieren zu wollen.",
        "Es gibt für alles eine Lösung. Das hier ist sie nicht.",
        "Du zuckst mit den Schultern. Das hat nicht geklappt.",
        "Du lässt es gut sein. Das führt nirgendwohin.",
        "Ein kurzer Moment der Hoffnung, dann: Stille.",
        "Deine Intuition hat dich diesmal im Stich gelassen.",
        "Das wirkt irgendwie unpassend.",
        "Du brichst den Versuch ab, bevor es peinlich wird.");

    private static readonly byte[][] InspectResponseLines = TextUtils.EncodeNullTerminated(
        "Du schaust genauer hin, entdeckst aber keine neuen Details.",
        "Es sieht auf den zweiten Blick genau so aus wie auf den ersten.",
        "Du lässt deinen Blick darüber schweifen, aber es fällt dir nichts Besonderes auf.",
        "Abgesehen von dem, was du bereits weisst, gibt es hier nichts zu sehen.",
        "Du nimmst dir Zeit für eine Untersuchung, bleibst aber ratlos.");

    private static readonly byte[][] UseResponseLines = TextUtils.EncodeNullTerminated(
        "Du versuchst, es sinnvoll einzusetzen, aber es ergibt sich keine Gelegenheit.",
        "Dafür scheint es beim besten Willen nicht gedacht zu sein.",
        "Du hantierst damit herum, doch es erfüllt keinen Zweck.",
        "Die Anwendung scheitert an der Realität.",
        "Es weigert sich beharrlich, in dieser Situation nützlich zu sein.");

    private static readonly byte[][] OpenCloseResponseLines = TextUtils.EncodeNullTerminated(
        "Du suchst nach einer Öffnung, aber da ist nichts als glatte Fläche.",
        "Es lässt sich weder rütteln noch schieben.",
        "Du findest keinen Ansatzpunkt, um hier irgendetwas zu öffnen.",
        "Das scheint fest verschlossen oder gar nicht zum Öffnen gedacht zu sein.",
        "Deine Versuche, daran zu ziehen oder zu drücken, bleiben erfolglos.");

    private static readonly byte[][] ReadResponseLines = TextUtils.EncodeNullTerminated(
        "Du lässt die Augen darüber gleiten, findest aber keinerlei Schrift.",
        "Da ist nichts, was man als Buchstaben oder Symbole entziffern könnte.",
        "Kein Text weit und breit. Nur die Leere der Oberfläche.",
        "Du suchst nach einer Botschaft, aber es bleibt wortkarg.",
        "Es gibt hier nichts zu lesen, ausser vielleicht deiner Enttäuschung.");

    private static readonly byte[][] WriteResponseLines = TextUtils.EncodeNullTerminated(
        "Du hättest zwar Ideen, aber hier ist kein Platz für Notizen.",
        "Du findest keine Stelle, die sich als Schreibunterlage eignen würde.",
        "Ohne passendes Werkzeug und einen Grund wird das nichts.",
        "Du überlegst kurz, etwas zu notieren, lässt es dann aber bleiben.",
        "Schreiben wäre hier eine ziemliche Verschwendung von Tinte.");

    private static readonly byte[][] TakeResponseLines = TextUtils.EncodeNullTerminated(
        "Du überlegst kurz, es einzustecken, aber wozu eigentlich?",
        "Das würde dein Gepäck nur unnötig schwer machen.",
        "Du lässt es lieber dort, wo es hingehört.",
        "Es gibt keinen vernünftigen Grund, das mit dir herumzuschleppen.",
        "Dein Inventar ist auch ohne dieses Ding schon voll genug mit Kram.");

    private static readonly byte[][] BuyResponseLines = TextUtils.EncodeNullTerminated(
        "Du siehst dich nach einem Verkäufer um, aber hier herrscht kein Handel.",
        "Das steht offensichtlich nicht zum Verkauf.",
        "Dein Geldbeutel bleibt heute zu. Hier gibt es keine Preise.",
        "Niemand hier scheint an deinem Gold interessiert zu sein.",
        "Ein Handel kommt unter diesen Umständen nicht zustande.");

    private static readonly byte[][] SitStandResponseLines = TextUtils.EncodeNullTerminated(
        "Das sieht nicht gerade nach einer bequemen Sitzgelegenheit aus.",
        "Du bleibst lieber stehen, bevor du dir den Rücken ruinierst.",
        "Kein guter Ort, um die Beine hochzulegen.",
        "Du findest keine Position, die auch nur ansatzweise gemütlich wäre.",
        "Dein Drang, dich hier niederzulassen, hält sich in Grenzen.");

    /// <summary>
    ///     Tries to run the weighted DKSN fallback response for one missing hotspot action.
    /// </summary>
    /// <param name="descriptorRef">Committed descriptor reference for the currently selected interaction.</param>
    /// <param name="commandBucket">Fallback bucket that corresponds to the triggering command.</param>
    /// <returns>
    ///     True when the fallback ran for a scene hotspot; otherwise false when the selection is not a scene hotspot.
    /// </returns>
    internal bool TryRunMissingHotspotAction(InteractionDescriptorRef descriptorRef, DksnFallbackBucket commandBucket)
    {
        if (descriptorRef.Table != InteractionDescriptorTable.SceneEntry)
        {
            return false;
        }

        var selection = SelectResponse(runtime.DksnFallbackState, runtime.Random, commandBucket);
        runtime.PromptController.RunTextAnimation(ReadResponseLine(selection));
        return true;
    }

    /// <summary>
    ///     Selects the preferred bucket for one fallback trigger.
    /// </summary>
    /// <param name="random">Deterministic session random source.</param>
    /// <param name="commandBucket">Command-specific bucket associated with the triggering action.</param>
    /// <returns>
    ///     The shared default bucket 20% of the time; otherwise the supplied command-specific bucket.
    /// </returns>
    private static DksnFallbackBucket SelectPreferredBucket(RandomSource random, DksnFallbackBucket commandBucket)
    {
        if (!UsesWeightedDefaultFallback(commandBucket))
        {
            return commandBucket;
        }

        return random.NextInt(100) < 20 ? DksnFallbackBucket.Default : commandBucket;
    }

    /// <summary>
    ///     Selects one fallback response line according to the weighted bucket policy and bucket history bitmasks.
    /// </summary>
    /// <param name="fallbackState">Runtime-owned shown-line state for the active session.</param>
    /// <param name="random">Deterministic session random source.</param>
    /// <param name="commandBucket">Command-specific bucket associated with the triggering action.</param>
    /// <returns>The selected fallback bucket and line index.</returns>
    private static DksnFallbackSelection SelectResponse(DksnFallbackState fallbackState,
        RandomSource random,
        DksnFallbackBucket commandBucket)
    {
        if (!UsesWeightedDefaultFallback(commandBucket))
        {
            return SelectFromExclusiveBucket(fallbackState, random, commandBucket);
        }

        var preferredBucket = SelectPreferredBucket(random, commandBucket);
        if (preferredBucket == DksnFallbackBucket.Default)
        {
            return SelectFromDefaultBucket(fallbackState, random);
        }

        if (TrySelectUnseenResponse(fallbackState, random, preferredBucket, out var preferredSelection))
        {
            return preferredSelection;
        }

        if (TrySelectUnseenResponse(fallbackState, random, DksnFallbackBucket.Default, out var defaultSelection))
        {
            return defaultSelection;
        }

        fallbackState.ClearBucket(preferredBucket);
        fallbackState.ClearBucket(DksnFallbackBucket.Default);

        if (TrySelectUnseenResponse(fallbackState, random, preferredBucket, out preferredSelection))
        {
            return preferredSelection;
        }

        if (TrySelectUnseenResponse(fallbackState, random, DksnFallbackBucket.Default, out defaultSelection))
        {
            return defaultSelection;
        }

        throw new InvalidOperationException(
            $"No DKSN fallback text is configured for command bucket '{preferredBucket}' or the shared default bucket.");
    }

    private static DksnFallbackSelection SelectFromDefaultBucket(DksnFallbackState fallbackState, RandomSource random)
    {
        if (TrySelectUnseenResponse(fallbackState, random, DksnFallbackBucket.Default, out var selection))
        {
            return selection;
        }

        fallbackState.ClearBucket(DksnFallbackBucket.Default);
        return TrySelectUnseenResponse(fallbackState, random, DksnFallbackBucket.Default, out selection)
            ? selection
            : throw new InvalidOperationException("No DKSN default fallback text is configured.");
    }

    private static DksnFallbackSelection SelectFromExclusiveBucket(DksnFallbackState fallbackState,
        RandomSource random,
        DksnFallbackBucket bucket)
    {
        if (TrySelectUnseenResponse(fallbackState, random, bucket, out var selection))
        {
            return selection;
        }

        fallbackState.ClearBucket(bucket);
        return TrySelectUnseenResponse(fallbackState, random, bucket, out selection)
            ? selection
            : throw new InvalidOperationException(
                $"No DKSN fallback text is configured for command bucket '{bucket}'.");
    }

    private static bool TrySelectUnseenResponse(DksnFallbackState fallbackState,
        RandomSource random,
        DksnFallbackBucket bucket,
        out DksnFallbackSelection selection)
    {
        var lines = GetBucketLines(bucket);
        EnsureBucketFitsBitmask(bucket, lines.Length);
        if (lines.Length == 0)
        {
            selection = default;
            return false;
        }

        var shownMask = fallbackState.ReadShownLineMask(bucket);
        Span<int> unseenIndices = stackalloc int[64];
        var unseenCount = 0;
        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            if ((shownMask & (1UL << lineIndex)) != 0)
            {
                continue;
            }

            unseenIndices[unseenCount++] = lineIndex;
        }

        if (unseenCount == 0)
        {
            selection = default;
            return false;
        }

        var selectedLineIndex = unseenIndices[random.NextInt(unseenCount)];
        fallbackState.MarkLineShown(bucket, selectedLineIndex);
        selection = new DksnFallbackSelection(bucket, selectedLineIndex);
        return true;
    }

    private static byte[] ReadResponseLine(DksnFallbackSelection selection)
    {
        return GetBucketLines(selection.Bucket)[selection.LineIndex];
    }

    private static byte[][] GetBucketLines(DksnFallbackBucket bucket)
    {
        return bucket switch
        {
            DksnFallbackBucket.Default => DefaultResponseLines,
            DksnFallbackBucket.Inspect => InspectResponseLines,
            DksnFallbackBucket.Use => UseResponseLines,
            DksnFallbackBucket.OpenClose => OpenCloseResponseLines,
            DksnFallbackBucket.Read => ReadResponseLines,
            DksnFallbackBucket.Write => WriteResponseLines,
            DksnFallbackBucket.Take => TakeResponseLines,
            DksnFallbackBucket.Buy => BuyResponseLines,
            DksnFallbackBucket.SitStand => SitStandResponseLines,
            _ => throw new InvalidOperationException($"Unsupported DKSN fallback bucket '{bucket}'.")
        };
    }

    private static bool UsesWeightedDefaultFallback(DksnFallbackBucket commandBucket)
    {
        return commandBucket is not DksnFallbackBucket.Default and not DksnFallbackBucket.Write;
    }

    private static void EnsureBucketFitsBitmask(DksnFallbackBucket bucket, int lineCount)
    {
        if (lineCount > 64)
        {
            throw new InvalidOperationException(
                $"DKSN fallback bucket '{bucket}' has {lineCount} lines, which exceeds the 64-bit shown-line bitmask capacity.");
        }
    }
}

/// <summary>
///     Identifies one selected DKSN fallback bucket line.
/// </summary>
/// <param name="Bucket">Bucket that owns the selected line.</param>
/// <param name="LineIndex">Zero-based line index within the selected bucket.</param>
internal readonly record struct DksnFallbackSelection(DksnFallbackBucket Bucket, int LineIndex);
