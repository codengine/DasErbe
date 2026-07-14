namespace Game.DataBlock;

/// <summary>
///     Writes a structured <see cref="DataBlockModel" /> back to raw data-block bytes.
/// </summary>
internal static class DataBlockWriter
{
    /// <summary>
    ///     Serializes one full data block.
    /// </summary>
    /// <param name="model">Structured data-block model to serialize.</param>
    /// <returns>The exact raw data-block byte layout for the model.</returns>
    internal static byte[] Write(DataBlockModel model)
    {
        var destination = new byte[DataBlockModel.BlockLength];
        model.SelectionTable.WriteToBlock(destination);
        model.InteractionDescriptors.WriteToBlock(destination);
        model.SceneDescriptors.WriteToBlock(destination);
        model.Control.WriteToBlock(destination);

        return destination;
    }
}
