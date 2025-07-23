using Godot;

public partial class Door : BlockBehavior {
    private bool open = false;
    float offset = 0.436f;

    public override void OnClick() {
        if (BlockType is not Block.BlockType.Door)
            return;

        open = !open;
        if (!open) {
            Transform.Basis.Rotated(Vector3.Up, Mathf.DegToRad(90));
            Position += Transform.Basis.X * offset;
        }
        else {
            Position -= Transform.Basis.X * offset;
            Transform.Basis.Rotated(Vector3.Up, Mathf.DegToRad(-90));
        }
    }
}