using G104.Engine.Scene;

namespace G104.Engine.Editor;

// 历史保存设计快照，运行态不入栈；对象池使Undo删除与重做创建继续使用同一DTO身份。
/// <summary>管理有限设计快照与拖动事务，恢复时保留同GUID对象的DTO身份。</summary>
public sealed class EditorHistory
{
    /// <summary>一条可撤销设计变更的前后快照。</summary>
    /// <param name="Label">显示给用户的操作说明。</param>
    /// <param name="Before">操作前的独立设计快照。</param>
    /// <param name="After">操作后的独立设计快照。</param>
    private sealed record Entry(string Label, SceneDocument Before, SceneDocument After);
    /// <summary>历史操作所修改的设计图，不保存运行世界资源。</summary>
    private readonly SceneGraph _graph;
    /// <summary>按提交顺序保存的前后快照，最多保留Capacity条。</summary>
    private readonly List<Entry> _entries = [];
    /// <summary>按GUID保留DTO对象身份，使删除后Undo仍能复用同一对象。</summary>
    private readonly Dictionary<Guid, SceneObjectData> _identities = [];
    /// <summary>下一条可Redo记录的位置，也是当前可Undo条数。</summary>
    private int _cursor;
    /// <summary>当前拖动等组合事务的起始快照；null表示没有事务。</summary>
    private SceneDocument? _transactionBefore;
    /// <summary>组合事务提交时使用的操作标签。</summary>
    private string? _transactionLabel;
    /// <summary>实际保存目标的序列化基准；null表示尚无有效保存内容。</summary>
    private string? _saved;

    /// <summary>绑定设计图并初始化身份池、容量和当前内容的默认保存基准。</summary>
    public EditorHistory(SceneGraph graph, int capacity = 100)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _graph = graph;
        Capacity = capacity;
        RememberIdentities();
        _saved = SceneSerializer.Serialize(graph.Document);
    }

    /// <summary>最多保留的历史记录条数，默认100。</summary>
    public int Capacity { get; }
    // 应用在安全点准备候选资源；失败在设计/历史提交前回滚，Engine不依赖具体渲染后端。
    /// <summary>应用层提供的资源预备回调；失败阻止本次编辑或历史切换提交。</summary>
    public Action<SceneGraph>? PrepareChange { get; set; }
    /// <summary>无活动事务且游标前有记录时才允许撤销。</summary>
    public bool CanUndo => !InTransaction && _cursor > 0;
    /// <summary>无活动事务且游标后有记录时才允许重做。</summary>
    public bool CanRedo => !InTransaction && _cursor < _entries.Count;
    /// <summary>是否已有尚未提交或取消的组合事务。</summary>
    public bool InTransaction => _transactionBefore is not null;
    /// <summary>当前设计文本是否不同于实际保存基准，缺少基准时始终视为未保存。</summary>
    public bool IsDirty => _saved is null || SceneSerializer.Serialize(_graph.Document) != _saved;
    /// <summary>游标之前可撤销的记录数量。</summary>
    public int UndoCount => _cursor;
    /// <summary>游标之后可重做的记录数量。</summary>
    public int RedoCount => _entries.Count - _cursor;
    /// <summary>下一次撤销的操作说明，不可撤销时为null。</summary>
    public string? UndoLabel => CanUndo ? _entries[_cursor - 1].Label : null;
    /// <summary>下一次重做的操作说明，不可重做时为null。</summary>
    public string? RedoLabel => CanRedo ? _entries[_cursor].Label : null;

    /// <summary>执行一次设计更改并校验/预备资源；失败回滚，事务外成功才新增记录。</summary>
    public void Execute(string label, Action change)
    {
        ArgumentNullException.ThrowIfNull(change);
        var before = SceneSerializer.Clone(_graph.Document);
        try
        {
            change();
            _graph.Rebuild();
            // 校验与资源准备均成功后才记录历史；失败恢复本次操作前的合法值。
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

    /// <summary>记录一次连续手势的起点，将后续合法编辑合并为一条历史。</summary>
    public void BeginTransaction(string label)
    {
        if (InTransaction) throw new InvalidOperationException("An editor transaction is already active.");
        _transactionBefore = SceneSerializer.Clone(_graph.Document);
        _transactionLabel = label;
    }

    /// <summary>将事务起点与当前设计合并记录，无实质差异时不新增历史。</summary>
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

    /// <summary>恢复事务起点并结束事务，保留原有Redo分支。</summary>
    public void CancelTransaction()
    {
        if (_transactionBefore is null) return;
        var before = _transactionBefore;
        Restore(before);
        _transactionBefore = null;
        _transactionLabel = null;
        PruneIdentities();
    }

    /// <summary>先准备上一记录的Before资源，成功恢复后再回退游标。</summary>
    public bool Undo()
    {
        if (!CanUndo) return false;
        RestorePrepared(_entries[_cursor - 1].Before);
        _cursor--;
        return true;
    }

    /// <summary>先准备当前记录的After资源，成功恢复后再前进游标。</summary>
    public bool Redo()
    {
        if (!CanRedo) return false;
        RestorePrepared(_entries[_cursor].After);
        _cursor++;
        return true;
    }

    /// <summary>把当前设计内容设为保存基准；调用者应已完成实际文件保存。</summary>
    public void MarkSaved()
    {
        SetSavedBaseline(_graph.Document);
    }

    // 基准是实际保存目标的内容；切换到种子不会把种子误标成该文件已保存。
    /// <summary>设置实际目标文件的内容基准；null表示无有效存档，活动事务时拒绝。</summary>
    public void SetSavedBaseline(SceneDocument? savedDocument)
    {
        if (InTransaction) throw new InvalidOperationException("Commit or cancel the editor transaction before saving.");
        _saved = savedDocument is null ? null : SceneSerializer.Serialize(savedDocument);
    }

    /// <summary>清空历史并重建当前对象身份池，可选择将当前设计标为保存基准。</summary>
    public void Clear(bool markSaved = true)
    {
        if (InTransaction) CancelTransaction();
        _entries.Clear();
        _cursor = 0;
        _identities.Clear();
        RememberIdentities();
        if (markSaved) MarkSaved();
    }

    /// <summary>忽略空更改，截断旧Redo分支并按容量保留最近提交。</summary>
    private void Record(string label, SceneDocument before, SceneDocument after)
    {
        if (SceneSerializer.Serialize(before) == SceneSerializer.Serialize(after)) return;
        if (_cursor < _entries.Count) _entries.RemoveRange(_cursor, _entries.Count - _cursor);
        _entries.Add(new Entry(label, before, after));
        if (_entries.Count > Capacity) _entries.RemoveAt(0);
        _cursor = _entries.Count;
        PruneIdentities();
    }

    /// <summary>先在独立候选图准备资源，成功后再修改当前设计与身份对象。</summary>
    private void RestorePrepared(SceneDocument snapshot)
    {
        // Undo/Redo先准备独立候选，资源失败不移动游标或替换当前设计。
        var candidate = new SceneGraph(SceneSerializer.Clone(snapshot));
        PrepareChange?.Invoke(candidate);
        Restore(snapshot);
    }

    /// <summary>复制快照设计并按GUID复用DTO，再重建图与显示历史。</summary>
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

    /// <summary>登记当前场景对象，供以后删除、Undo和Redo恢复同一DTO。</summary>
    private void RememberIdentities()
    {
        foreach (var item in _graph.Document.Objects) _identities[item.Id] = item;
    }

    /// <summary>只保留当前设计和历史快照仍引用的对象，限制身份池增长。</summary>
    private void PruneIdentities()
    {
        var retained = _graph.Document.Objects.Select(item => item.Id)
            .Concat(_entries.SelectMany(entry => entry.Before.Objects.Concat(entry.After.Objects)).Select(item => item.Id)).ToHashSet();
        foreach (var id in _identities.Keys.Where(id => !retained.Contains(id)).ToArray()) _identities.Remove(id);
    }

    /// <summary>把快照字段写回既有DTO，保留其引用身份与稳定ID。</summary>
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
