using Godot;
using static Block;

public class BlockData {
    public string BlockName { get; } = "air";
    public Block.BlockType Type { get; }
    public string VariationOfBlock { get; }
    public string[] Tags { get; }
    public bool CanBeConnected { get; }
    public bool Unbreakable { get; }
    public bool BlocksCanBePlacedOn { get; }
    public Block.FacingDirections FacingDirection { get; private set; }
    public bool UpsideDown { get; private set; }
    public CSharpScript BehaviorScript { get; }
    public CsgMesh3D CsgMesh3D { get; }
    public Basis Basis { get; private set; }

    public BlockData() { }

    // Rotate a block at xyz (hole turns)
    public void Rotate(Vector3 rotationDeg) {
        // Snap each component to 90°
        Vector3 snapped = new Vector3(
            Mathf.Round(rotationDeg.X / 90f) * 90f,
            Mathf.Round(rotationDeg.Y / 90f) * 90f,
            Mathf.Round(rotationDeg.Z / 90f) * 90f
        );

        // Convert degrees to radians
        Vector3 radians = snapped * Mathf.DegToRad(1.0f);

        // Build basis from Euler angles (YXZ order = yaw, pitch, roll)
        Basis b = Basis.FromEuler(radians);

        // Orthonormalize to make sure it’s clean (important!)
        Basis = b.Orthonormalized();

        // Optional: set flag for upside down
        UpsideDown = !Mathf.IsZeroApprox(snapped.X);

        switch ((int)snapped.Y % 360) {
            case 0:
                FacingDirection = Block.FacingDirections.Forward;
                break;
            case 90:
            case -270:
                FacingDirection = Block.FacingDirections.Right;
                break;
            case 180:
            case -180:
                FacingDirection = Block.FacingDirections.Backward;
                break;
            case 270:
            case -90:
                FacingDirection = Block.FacingDirections.Left;
                break;
            default:
                GD.PrintErr($"Unexpected Y: {(int)snapped.Y}");
                FacingDirection = Block.FacingDirections.Forward; // fallback
                break;
        }

    }


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