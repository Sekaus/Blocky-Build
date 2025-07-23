using Godot;
using System;

public partial class Default : BlockBehavior {
    public override void OnClick() {
        GD.Print("Default OnClick: pose (" + Position.X + ", " + Position.Y + ", " + Position.Z + ")");
    }

    public override void OnPlace() {
        GD.Print("Default OnPlace");
    }

    public override void OnRemove() {
        GD.Print("Default OnRemove");
    }
}