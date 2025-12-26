using Godot;
using System.Collections.Generic;

public static class BlockRegistry {
    private static readonly Dictionary<int, Block> blocksByID = new();
    private static readonly Dictionary<string, Block> blocksByName = new();

    public static void RegisterBlock(Block block) {
        blocksByID[block.ID] = block;
        blocksByName[block.Name.ToLower()] = block;
    }

    public static Block GetBlock(int id) => blocksByID.TryGetValue(id, out var b) ? b : null;
    public static Block GetBlock(string name) => blocksByName.TryGetValue(name.ToLower(), out var b) ? b : null;
    public static IEnumerable<Block> GetAllBlocks() => blocksByID.Values;

    public static void Init() {
        RegisterBlock(new Block(
            0, "Air", false, BlockShape.Cube, null
        ));

        RegisterBlock(new Block(
            1, "Dirt", true, BlockShape.Cube,
            GD.Load<Texture2D>("res://Assets/Blocks/Textures/dirt.png")
        ));

        RegisterBlock(new Block(
            2, "Grass", true, BlockShape.Cube,
            GD.Load<Texture2D>("res://Assets/Blocks/Textures/grass.png")
        ));

        RegisterBlock(new Block(
            3, "Stone", true, BlockShape.Cube,
            GD.Load<Texture2D>("res://Assets/Blocks/Textures/stone.png")
        ));

        RegisterBlock(new Block(
            4, "Plant", false, BlockShape.Cross,
            GD.Load<Texture2D>("res://Assets/Blocks/Textures/plant.png")
        ));
    }
}