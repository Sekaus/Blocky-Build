using System.Collections.Generic;
public static class BlockShapeRegistry {
    private static readonly Dictionary<BlockShape, IBlockShapeBuilder> builders = new();

    static BlockShapeRegistry() {
        builders[BlockShape.Cube]   = new CubeShapeBuilder();
        builders[BlockShape.Slab]   = new SlabShapeBuilder();
        builders[BlockShape.Cross]  = new CrossShapeBuilder();
        //builders[BlockShape.Torch]  = new TorchShapeBuilder();
        // Stairs later
    }

    public static IBlockShapeBuilder Get(BlockShape shape)
        => builders[shape];
}