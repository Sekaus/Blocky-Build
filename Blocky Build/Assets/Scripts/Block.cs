using Godot;

public class Block {
    public int ID;
    public string Name;
    public bool IsSolid;
    public BlockShape Shape;
    public Texture2D Texture;
    public Material Material;

    public Block(int id, string name, bool solid, BlockShape shape, Texture2D texture) {
        ID = id;
        Name = name;
        IsSolid = solid;
        Shape = shape;
        Texture = texture;

        var mat = new StandardMaterial3D();
        mat.AlbedoTexture = texture;
        mat.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
        mat.Roughness = 1.0f;
        mat.Metallic = 0.0f;

        Material = mat;
    }
}