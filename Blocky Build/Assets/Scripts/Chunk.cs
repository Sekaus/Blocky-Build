using Godot;

public class Chunk {
    public const int Width = 16;
    public const int Height = 128;
    public const int Depth = 16;

    // Store ONLY block IDs
    public int[,,] Blocks = new int[Width, Height, Depth];

    public Chunk() {
        // Initialize everything to Air (ID = 0)
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                for (int z = 0; z < Depth; z++)
                    Blocks[x, y, z] = 0;
    }

    // Returns null if air or invalid
    public Block GetBlock(int x, int y, int z) {
        int id = Blocks[x, y, z];
        return id == 0 ? null : BlockRegistry.GetBlock(id);
    }

    public int GetBlockId(int x, int y, int z) {
        return Blocks[x, y, z];
    }

    public void SetBlock(int x, int y, int z, int blockId) {
        Blocks[x, y, z] = blockId;
    }
}