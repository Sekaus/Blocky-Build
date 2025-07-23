using Godot;

public class BlockData {
    public string BlockName { get; set; }
    public Block.BlockType Type { get; set; }
    public string VariationOfBlock { get; set; }
    public string[] Tags { get; set; }
    public bool CanBeConnected { get; set; }
    public bool Unbreakable { get; set; }
    public Block.FacingDirections FacingDirection { get; set; }
    public bool UpsideDown { get; set; }
    public CSharpScript BehaviorScript { get; set; }

    public BlockData() { }

    public BlockData(Block source) {
        BlockName = source.BlockName;
        Type = source.Type;
        VariationOfBlock = source.VariationOfBlock;
        Tags = source.Tags;
        CanBeConnected = source.CanBeConnected;
        Unbreakable = source.Unbreakable;
        FacingDirection = source.FacingDirection;
        UpsideDown = source.UpsideDown;
        BehaviorScript = source.BehaviorScript;
    }
}