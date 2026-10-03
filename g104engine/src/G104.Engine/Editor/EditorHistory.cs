using G104.Engine.Scene;

namespace G104.Engine.Editor;

// 历史保存设计快照，运行态不入栈；对象池使Undo删除与重做创建继续使用同一DTO身份。
public sealed class EditorHistory
{
    private sealed record Entry(string Label, SceneDocument Before, SceneDocument After);
    private readonly SceneGraph _graph;
    private readonly List<Entry> _entries = [];
    private readonly Dictionary<Guid, SceneObjectData> _identities = [];
    private int _cursor;
    private SceneDocument? _transactionBefore;
    private string? _transactionLabel;
    private string _saved;

    public EditorHistory(SceneGraph graph, int capacity = 100)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _graph = graph;
        Capacity = capacity;
        RememberIdentities();
        _saved = SceneSerializer.Serialize(graph.Document);
    }

    public int Capacity { get; }
    // 应用在安全点准备候选资源；失败在设计/历史提交前回滚，Engine不依赖具体渲染后端。
    public Action<SceneGraph>? PrepareChange { get; set; }
    public bool CanUndo => !InTransaction && _cursor > 0;
    public bool CanRedo => !InTransaction && _cursor < _entries.Count;
    public bool InTransaction => _transactionBefore is not null;
    public bool IsDirty => SceneSerializer.Serialize(_graph.Document) != _saved;
    public int UndoCount => _cursor;
    public int RedoCount => _entries.Count - _cursor;
    public string? UndoLabel => CanUndo ? _entries[_cursor - 1].Label : null;
    public string? RedoLabel => CanRedo ? _entries[_cursor].Label : null;

    public void Execute(string label, Action change)
    {
        ArgumentNullException.ThrowIfNull(change);
        var before = SceneSerializer.Clone(_graph.Document);
        try
        {
            change();
            _graph.Rebuild();
            PrepareChange?.Invoke(_graph);
            _graph.ResetInterpolation();
            RememberIdentities();
            if (!InTransaction) Record(label, before, SceneSerializer.Clone(_graph.Document));
        }
        catch
        {
            Restore(before);
            throw;
        }
    }

    public void BeginTransaction(string label)
    {
        if (InTransaction) throw new InvalidOperationException("An editor transaction is already active.");
        _transactionBefore = SceneSerializer.Clone(_graph.Document);
        _transactionLabel = label;
    }

    public void CommitTransaction()
    {
        if (_transactionBefore is null) throw new InvalidOperationException("No editor transaction is active.");
        _graph.Rebuild();
        var before = _transactionBefore;
        var label = _transactionLabel!;
        _transactionBefore = null;
        _transactionLabel = null;
        Record(label, before, SceneSerializer.Clone(_graph.Document));
    }

    public void CancelTransaction()
    {
        if (_transactionBefore is null) return;
        var before = _transactionBefore;
        Restore(before);
        _transactionBefore = null;
        _transactionLabel = null;
        PruneIdentities();
    }

    public bool Undo()
    {
        if (!CanUndo) return false;
        RestorePrepared(_entries[_cursor - 1].Before);
        _cursor--;
        return true;
    }

    public bool Redo()
    {
        if (!CanRedo) return false;
        RestorePrepared(_entries[_cursor].After);
        _cursor++;
        return true;
    }

    public void MarkSaved()
    {
        if (InTransaction) throw new InvalidOperationException("Commit or cancel the editor transaction before saving.");
        _saved = SceneSerializer.Serialize(_graph.Document);
    }

    public void Clear(bool markSaved = true)
    {
        if (InTransaction) CancelTransaction();
        _entries.Clear();
        _cursor = 0;
        _identities.Clear();
        RememberIdentities();
        if (markSaved) MarkSaved();
    }

    private void Record(string label, SceneDocument before, SceneDocument after)
    {
        if (SceneSerializer.Serialize(before) == SceneSerializer.Serialize(after)) return;
        if (_cursor < _entries.Count) _entries.RemoveRange(_cursor, _entries.Count - _cursor);
        _entries.Add(new Entry(label, before, after));
        if (_entries.Count > Capacity) _entries.RemoveAt(0);
        _cursor = _entries.Count;
        PruneIdentities();
    }

    private void RestorePrepared(SceneDocument snapshot)
    {
        var candidate = new SceneGraph(SceneSerializer.Clone(snapshot));
        PrepareChange?.Invoke(candidate);
        Restore(snapshot);
    }

    private void Restore(SceneDocument snapshot)
    {
        var source = SceneSerializer.Clone(snapshot);
        var destination = _graph.Document;
        destination.Name = source.Name;
        destination.SchemaVersion = source.SchemaVersion;
        destination.Rendering = source.Rendering;
        destination.Templates = source.Templates;
        var restored = new List<SceneObjectData>(source.Objects.Count);
        foreach (var item in source.Objects)
        {
            if (!_identities.TryGetValue(item.Id, out var identity)) _identities[item.Id] = identity = item;
            else Copy(item, identity);
            restored.Add(identity);
        }
        destination.Objects = restored;
        _graph.Rebuild();
        _graph.ResetInterpolation();
    }

    private void RememberIdentities()
    {
        foreach (var item in _graph.Document.Objects) _identities[item.Id] = item;
    }

    private void PruneIdentities()
    {
        var retained = _graph.Document.Objects.Select(item => item.Id)
            .Concat(_entries.SelectMany(entry => entry.Before.Objects.Concat(entry.After.Objects)).Select(item => item.Id)).ToHashSet();
        foreach (var id in _identities.Keys.Where(id => !retained.Contains(id)).ToArray()) _identities.Remove(id);
    }

    private static void Copy(SceneObjectData source, SceneObjectData destination)
    {
        destination.Name = source.Name;
        destination.ParentId = source.ParentId;
        destination.Kind = source.Kind;
        destination.Primitive = source.Primitive;
        destination.Transform = source.Transform;
        destination.Material = source.Material;
        destination.Collider = source.Collider;
        destination.ModelPath = source.ModelPath;
        destination.TemplateId = source.TemplateId;
        destination.TemplateOverrides = source.TemplateOverrides;
        destination.TargetId = source.TargetId;
        destination.Visible = source.Visible;
        destination.Parameters = source.Parameters;
    }
}
