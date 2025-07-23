using Godot;
using System.Data;

public partial class BlockBehavior : GodotObject {
    protected WorldData WorldData { get; private set; }
    protected Chunk Chunk { get; private set; }
    Transform3D transform;
    protected Transform3D Transform { 
        get { 
            return transform;
        }
        private set { 
            transform = value;
        } 
    }
    protected Vector3 Position { 
        get {
            return transform.Origin;
        }
        set {
            transform.Origin = value;
        }
    }
    protected Block.BlockType BlockType {
        get {
            return Chunk.Blocks[
                new Vector3I(
                    Mathf.RoundToInt(Position.X),
                    Mathf.RoundToInt(Position.Y),
                    Mathf.RoundToInt(Position.Z)
                )].Type;
        }
    }

    public virtual void OnClick() {
        GD.Print("Default OnClick");
    }

    public virtual void OnPlace() {
        GD.Print("Default OnPlace");
    }

    public virtual void OnRemove() {
        GD.Print("Default OnRemove");
    }

    public void Setup(WorldData worldData, Transform3D transform, Chunk chunk) {
        this.WorldData = worldData;
        this.Chunk = chunk;
        this.Transform = transform;
    }
}