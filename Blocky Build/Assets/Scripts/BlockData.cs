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
    public Block.FacingDirections FacingDirection { get; set; }
    public bool UpsideDown { get; set; }
    public CSharpScript BehaviorScript { get; }
    public CsgMesh3D CsgMesh3D { get; }
    public Basis Basis { get; set; }

    public BlockData() { }

    // Rotate a block at xyz (hole turns)
    public void Rotate(Vector3 rotationDegrees) {
        // If there's no rotation at all, bail out
        if (rotationDegrees == Vector3.Zero)
            return;

        // rotationDegrees.Y encodes the 'up‑flip' (0, 90 or 180)
        bool upsideDown = rotationDegrees.Y != 0;
        float radUp = Mathf.DegToRad(rotationDegrees.Y);

        // X‑axis turns (roll left/right)
        if (rotationDegrees.X != 0) {
            FacingDirection = (rotationDegrees.X > 0)
                ? Block.FacingDirections.Left
                : Block.FacingDirections.Right;

            // 180° yaw when rotating negatively around X
            if (rotationDegrees.X < 0)
                Basis.Rotated(Vector3.Up, Mathf.DegToRad(180));

            // do the “up‑flip” if requested
            if (upsideDown)
                Basis.Rotated(
                    rotationDegrees.X > 0 ? Vector3.Forward : Vector3.Back,
                    radUp
                );
        }
        // Z‑axis turns (yaw forward/back)
        else if (rotationDegrees.Z != 0) {
            FacingDirection = (rotationDegrees.Z > 0)
                ? Block.FacingDirections.Forward
                : Block.FacingDirections.Backward;

            Basis.Rotated(
                Vector3.Up,
                Mathf.DegToRad(rotationDegrees.Z > 0 ? -90 : 90)
            );

            if (upsideDown)
                Basis.Rotated(
                    rotationDegrees.Z > 0 ? Vector3.Right : Vector3.Left,
                    radUp
                );
        }
        // Pure “up‑flip” around the forward axis
        else if (upsideDown) {
            Basis.Rotated(Vector3.Forward, radUp);
        }

        // finally mark the block as upside‑down if we rolled it
        if (upsideDown)
            UpsideDown = true;
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