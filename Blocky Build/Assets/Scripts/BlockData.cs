using Godot;
using System.Net.Sockets;

public class BlockData {
    public string BlockName { get; } = "air";
    public Block.BlockType Type { get; }
    public string VariationOfBlock { get; }
    public string[] Tags { get; }
    public bool CanBeConnected { get; }
    public bool Unbreakable { get; }
    public bool BlocksCanBePlacedOn { get; }
    public Block.FacingDirections FacingDirection { get; }
    public bool UpsideDown { get; }
    public CSharpScript BehaviorScript { get; }
    public CsgMesh3D CsgMesh3D { get; }

    public BlockData() { }

    public BlockData(Block source) {
        BlockName = source.BlockName;
        Type = source.Type;
        VariationOfBlock = source.VariationOfBlock;
        Tags = source.Tags;
        CanBeConnected = source.CanBeConnected;
        Unbreakable = source.Unbreakable;
        BlocksCanBePlacedOn = source.BlocksCanBePlacedOn;
        FacingDirection = source.FacingDirection;
        UpsideDown = source.UpsideDown;
        BehaviorScript = source.BehaviorScript;
        CsgMesh3D = source.GetNodeOrNull<CsgMesh3D>("Mesh");
    }
}