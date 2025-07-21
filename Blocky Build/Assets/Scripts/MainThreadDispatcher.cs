using Godot;
using System;
using System.Collections.Concurrent;

public partial class MainThreadDispatcher : Node {
    private static ConcurrentQueue<Action> _actions = new ConcurrentQueue<Action>();

    public static void Enqueue(Action action) {
        _actions.Enqueue(action);
    }

    public override void _Process(double delta) {
        while (_actions.TryDequeue(out var action)) {
            action?.Invoke();
        }
    }
}
