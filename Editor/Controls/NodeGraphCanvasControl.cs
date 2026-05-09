using Axe2DEditor.Core.Graphs;
using Axe2DEditor.Editor.Localization;
using System.Drawing.Drawing2D;

namespace Axe2DEditor.Editor.Controls;

internal sealed class NodeGraphCanvasControl : Panel
{
    private const string TriggerOwnerParameterKey = "__ownerTriggerId";

    private EventGraphDefinition? _graph;
    private GraphNodeDefinition? _rootNode;
    private string? _selectedNodeId;
    private string? _linkSourceNodeId;
    private float _cameraX = NodeGraphCanvasStyle.CanvasPadding;
    private float _cameraY = NodeGraphCanvasStyle.CanvasPadding;
    private float _zoomPercent = NodeGraphCanvasStyle.DefaultZoomPercent;
    private bool _panning;
    private Point _panStart;
    private float _panCameraX;
    private float _panCameraY;
    private string? _draggingNodeId;
    private Point _dragStart;
    private PointF _dragNodeStart;
    private NodeSocketHit? _connectingSocket;
    private NodeSocketHit? _connectionSnapSocket;
    private Point _connectionStartScreen;
    private Point _connectionCurrentScreen;
    private bool _connectionChanged;
    private NodeHitInfo? _hoverHit;
    private readonly HashSet<string> _persistentVisibleNodeIds = new(StringComparer.OrdinalIgnoreCase);
    private string? _visibilityRootNodeId;
    private readonly Dictionary<string, NodeLayoutInfo> _layoutCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, GraphNodeDefinition> _visibleNodeCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, GraphEdgeDefinition> _visibleEdgeCache = new(StringComparer.OrdinalIgnoreCase);
    private LocalizationService _localization = new();
    private readonly TextBox _inlineEditor = new();
    private readonly ComboBox _inlineDropdown = new();
    private bool _editing;
    private EditTarget? _editTarget;

    public event EventHandler? GraphChanged;
    public event EventHandler<NodeSelectionChangedEventArgs>? SelectionChanged;
    public event EventHandler<NodeContextRequestedEventArgs>? NodeContextRequested;
    public event EventHandler<CanvasContextRequestedEventArgs>? CanvasContextRequested;

    public LocalizationService Localization
    {
        get => _localization;
        set
        {
            _localization = value ?? new LocalizationService();
            RebuildLayout();
            Invalidate();
        }
    }

    public NodeGraphCanvasControl()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        SetStyle(ControlStyles.Selectable, true);
        TabStop = true;

        _inlineEditor.Visible = false;
        _inlineEditor.BorderStyle = BorderStyle.FixedSingle;
        _inlineEditor.KeyDown += InlineEditor_KeyDown;
        _inlineEditor.Leave += InlineEditor_Leave;
        Controls.Add(_inlineEditor);

        _inlineDropdown.Visible = false;
        _inlineDropdown.DropDownStyle = ComboBoxStyle.DropDownList;
        _inlineDropdown.FlatStyle = FlatStyle.Flat;
        _inlineDropdown.BackColor = Color.FromArgb(54, 55, 62);
        _inlineDropdown.ForeColor = Color.White;
        _inlineDropdown.IntegralHeight = false;
        _inlineDropdown.MaxDropDownItems = 12;
        _inlineDropdown.SelectionChangeCommitted += InlineDropdown_SelectionChangeCommitted;
        _inlineDropdown.Leave += InlineDropdown_Leave;
        Controls.Add(_inlineDropdown);
    }

    public EventGraphDefinition? Graph
    {
        get => _graph;
        set
        {
            var graphChanged = !ReferenceEquals(_graph, value);
            _graph = value;
            ResetVisibleNodeScope(graphChanged);
            RebuildLayout();
            Invalidate();
        }
    }

    public string? RootNodeId
    {
        get => _rootNode?.Id;
        set
        {
            var rootChanged = _rootNode is null || !string.Equals(_rootNode.Id, value, StringComparison.OrdinalIgnoreCase);
            _rootNode = ResolveNode(value);
            ResetVisibleNodeScope(rootChanged);
            RebuildLayout();
            Invalidate();
        }
    }

    public string? SelectedNodeId
    {
        get => _selectedNodeId;
        set
        {
            _selectedNodeId = value;
            Invalidate();
        }
    }

    public string? LinkSourceNodeId
    {
        get => _linkSourceNodeId;
        set
        {
            _linkSourceNodeId = value;
            Invalidate();
        }
    }

    public void BindGraph(EventGraphDefinition? graph, GraphNodeDefinition? rootNode)
    {
        var graphChanged = !ReferenceEquals(_graph, graph);
        var rootChanged = _rootNode is null || rootNode is null
            ? _rootNode is not null || rootNode is not null
            : !string.Equals(_rootNode.Id, rootNode.Id, StringComparison.OrdinalIgnoreCase);
        _graph = graph;
        _rootNode = rootNode;
        ResetVisibleNodeScope(graphChanged || rootChanged);
        if (_rootNode is null && _graph is not null)
        {
            _rootNode = _graph.Nodes.FirstOrDefault(node => node.Kind == NodeKinds.Trigger);
        }

        _selectedNodeId = _rootNode?.Id;
        RebuildLayout();
        Invalidate();
    }

    public GraphNodeDefinition? ResolveNode(string? nodeId)
    {
        if (_graph is null || string.IsNullOrWhiteSpace(nodeId))
        {
            return null;
        }

        return _graph.Nodes.FirstOrDefault(node => string.Equals(node.Id, nodeId, StringComparison.OrdinalIgnoreCase));
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        Focus();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (_editing)
        {
            CommitInlineEditor();
        }

        if (e.Button == MouseButtons.Middle)
        {
            _panning = true;
            _panStart = e.Location;
            _panCameraX = _cameraX;
            _panCameraY = _cameraY;
            Cursor = Cursors.SizeAll;
            return;
        }

        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        var hit = HitTest(ScreenToWorld(e.Location));
        _hoverHit = hit;

        if (hit?.Field is not null && hit.Socket is null && hit.Field.Editable && hit.Field.ValueType != NodePortValueTypes.Bool)
        {
            if (BeginQuickFieldEdit(hit.Field))
            {
                return;
            }
        }

        if (hit?.Socket is not null)
        {
            var socket = hit.Socket;
            if (socket.IsOutput)
            {
                _connectionChanged = false;
                _connectingSocket = socket;
                _connectionStartScreen = e.Location;
                _connectionCurrentScreen = e.Location;
                Invalidate();
                return;
            }

            if (socket.IsInput && TryDetachIncomingAndStartReconnect(socket, e.Location))
            {
                return;
            }
        }

        if (hit?.Field is not null && hit.Field.Editable && hit.Field.ValueType == NodePortValueTypes.Bool)
        {
            ToggleBooleanField(hit.Field);
            return;
        }

        if (hit?.Node is not null)
        {
            SelectNode(hit.Node.Node.Id);
            _draggingNodeId = hit.Node.Node.Id;
            _dragStart = e.Location;
            _dragNodeStart = new PointF(hit.Node.Node.X, hit.Node.Node.Y);
            Capture = true;
        }
        else
        {
            SelectNode(null);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_panning)
        {
            var dx = e.X - _panStart.X;
            var dy = e.Y - _panStart.Y;
            _cameraX = _panCameraX + dx;
            _cameraY = _panCameraY + dy;
            Invalidate();
            return;
        }

        if (_draggingNodeId is not null && e.Button == MouseButtons.Left)
        {
            var world = ScreenToWorld(e.Location);
            var startWorld = ScreenToWorld(_dragStart);
            var dx = world.X - startWorld.X;
            var dy = world.Y - startWorld.Y;
            var node = ResolveNode(_draggingNodeId);
            if (node is not null)
            {
                node.X = (int)Math.Round(_dragNodeStart.X + dx);
                node.Y = (int)Math.Round(_dragNodeStart.Y + dy);
                RebuildLayout();
                Invalidate();
            }

            return;
        }

        if (_connectingSocket is not null)
        {
            _connectionSnapSocket = TryGetSnappedInputSocket(e.Location, out var snappedSocket) ? snappedSocket : null;
            _connectionCurrentScreen = _connectionSnapSocket is not null
                ? Point.Round(GetSocketScreenCenter(_connectionSnapSocket))
                : e.Location;
            Invalidate();
            return;
        }

        _hoverHit = HitTest(ScreenToWorld(e.Location));
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.Button == MouseButtons.Middle)
        {
            _panning = false;
            Cursor = Cursors.Default;
            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            if (_draggingNodeId is not null)
            {
                _draggingNodeId = null;
                Capture = false;
                GraphChanged?.Invoke(this, EventArgs.Empty);
            }

            if (_connectingSocket is not null)
            {
                FinishConnection(e.Location);
            }
        }

        if (e.Button == MouseButtons.Right)
        {
            var hit = HitTest(ScreenToWorld(e.Location));
            if (hit?.Node is not null)
            {
                SelectNode(hit.Node.Node.Id);
                NodeContextRequested?.Invoke(this, new NodeContextRequestedEventArgs(hit.Node.Node.Id, e.Location, ScreenToWorld(e.Location)));
            }
            else
            {
                CanvasContextRequested?.Invoke(this, new CanvasContextRequestedEventArgs(e.Location, ScreenToWorld(e.Location)));
            }
        }
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);

        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        var hit = HitTest(ScreenToWorld(e.Location));
            if (hit?.Field is not null && hit.Field.Editable)
            {
                BeginInlineEdit(hit.Field);
                return;
            }

        if (hit?.Node is not null)
        {
            BeginNodeTitleEdit(hit.Node.Node);
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverHit = null;
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);

        var beforeWorld = ScreenToWorld(e.Location);
        _zoomPercent = e.Delta > 0 ? _zoomPercent * 1.08f : _zoomPercent / 1.08f;
        _zoomPercent = Math.Clamp(_zoomPercent, NodeGraphCanvasStyle.MinZoomPercent, NodeGraphCanvasStyle.MaxZoomPercent);
        var afterScreen = ToScreen(beforeWorld);
        _cameraX += e.Location.X - afterScreen.X;
        _cameraY += e.Location.Y - afterScreen.Y;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(NodeGraphCanvasStyle.CanvasBackColor);

        var content = ClientRectangle;
        NodeGraphBackgroundService.DrawGrid(g, content, PixelsPerUnit, ToScreen, ScreenToWorld);

        if (_graph is null || _rootNode is null)
        {
            NodeGraphBackgroundService.DrawEmptyState(g, Font, ClientSize);
            return;
        }

        EnsureLayout();
        NodeGraphRenderService.DrawScene(
            g,
            Font,
            PixelsPerUnit,
            _graph,
            _visibleNodeCache,
            _layoutCache,
            _selectedNodeId,
            _linkSourceNodeId,
            _hoverHit?.Field,
            _hoverHit?.Socket,
            _connectingSocket,
            _connectionSnapSocket,
            _connectionCurrentScreen,
            ToScreen,
            GetSocketScreenCenter,
            GetPortCenter,
            GetNodeTitle);
        NodeGraphBackgroundService.DrawRulerBackground(g);
        NodeGraphBackgroundService.DrawRulers(g, content, Font, PixelsPerUnit, Width, Height, ToScreen, ScreenToWorld);
    }

    private void SelectNode(string? nodeId)
    {
        if (string.Equals(_selectedNodeId, nodeId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _selectedNodeId = nodeId;
        SelectionChanged?.Invoke(this, new NodeSelectionChangedEventArgs(nodeId));
        Invalidate();
    }

    private void RebuildLayout()
    {
        _layoutCache.Clear();
        _visibleNodeCache.Clear();
        _visibleEdgeCache.Clear();

        if (_graph is null || _rootNode is null)
        {
            return;
        }

        if (!string.Equals(_visibilityRootNodeId, _rootNode.Id, StringComparison.OrdinalIgnoreCase))
        {
            _visibilityRootNodeId = _rootNode.Id;
            _persistentVisibleNodeIds.Clear();
        }

        var reachableIds = GetVisibleNodes(_rootNode).Select(node => node.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var reachableId in reachableIds)
        {
            _persistentVisibleNodeIds.Add(reachableId);
        }

        if (!string.IsNullOrWhiteSpace(_selectedNodeId) && reachableIds.Contains(_selectedNodeId))
        {
            _persistentVisibleNodeIds.Add(_selectedNodeId);
        }

        var allNodeIds = _graph.Nodes.Select(node => node.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _persistentVisibleNodeIds.RemoveWhere(id => !allNodeIds.Contains(id));

        foreach (var reachableId in reachableIds)
        {
            var reachableNode = _graph.Nodes.FirstOrDefault(node => string.Equals(node.Id, reachableId, StringComparison.OrdinalIgnoreCase));
            if (reachableNode is not null)
            {
                EnsureOwnerForRootScope(reachableNode);
            }
        }

        var visibleNodes = _graph.Nodes
            .Where(node =>
                string.Equals(node.Id, _rootNode.Id, StringComparison.OrdinalIgnoreCase) ||
                (!string.Equals(node.Kind, NodeKinds.Trigger, StringComparison.OrdinalIgnoreCase) &&
                 (_persistentVisibleNodeIds.Contains(node.Id) ||
                  string.Equals(GetNodeOwner(node), _rootNode.Id, StringComparison.OrdinalIgnoreCase))))
            .ToList();
        foreach (var node in visibleNodes)
        {
            _visibleNodeCache[node.Id] = node;
        }

        foreach (var edge in _graph.Edges)
        {
            if (_visibleNodeCache.ContainsKey(edge.FromNodeId) && _visibleNodeCache.ContainsKey(edge.ToNodeId))
            {
                _visibleEdgeCache[$"{edge.FromNodeId}:{edge.FromPort}->{edge.ToNodeId}:{edge.ToPort}"] = edge;
            }
        }

        foreach (var node in visibleNodes)
        {
            _layoutCache[node.Id] = NodeGraphLayoutService.BuildLayout(_localization, Font, node);
        }

        if (_selectedNodeId is not null && !_layoutCache.ContainsKey(_selectedNodeId))
        {
            _selectedNodeId = _rootNode.Id;
        }
    }

    private void EnsureLayout()
    {
        if (_graph is null || _rootNode is null)
        {
            return;
        }

        if (_layoutCache.Count == 0 || _layoutCache.Count != _visibleNodeCache.Count)
        {
            RebuildLayout();
        }
    }

    private IEnumerable<GraphNodeDefinition> GetVisibleNodes(GraphNodeDefinition rootNode)
    {
        if (_graph is null)
        {
            yield break;
        }

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<GraphNodeDefinition>();
        queue.Enqueue(rootNode);
        visited.Add(rootNode.Id);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            yield return current;

            foreach (var edge in _graph.Edges.Where(edge => string.Equals(edge.FromNodeId, current.Id, StringComparison.OrdinalIgnoreCase)))
            {
                var next = _graph.Nodes.FirstOrDefault(node => string.Equals(node.Id, edge.ToNodeId, StringComparison.OrdinalIgnoreCase));
                if (next is not null && visited.Add(next.Id))
                {
                    queue.Enqueue(next);
                }
            }
        }
    }

    private RectangleF ToScreen(RectangleF rect)
    {
        return new RectangleF(rect.X * PixelsPerUnit + _cameraX, rect.Y * PixelsPerUnit + _cameraY, rect.Width * PixelsPerUnit, rect.Height * PixelsPerUnit);
    }

    private PointF ToScreen(PointF point)
    {
        return new PointF(point.X * PixelsPerUnit + _cameraX, point.Y * PixelsPerUnit + _cameraY);
    }

    private RectangleF ToWorld(RectangleF rect)
    {
        return new RectangleF((rect.X - _cameraX) / PixelsPerUnit, (rect.Y - _cameraY) / PixelsPerUnit, rect.Width / PixelsPerUnit, rect.Height / PixelsPerUnit);
    }

    private PointF ScreenToWorld(Point point)
    {
        return new PointF((point.X - _cameraX) / PixelsPerUnit, (point.Y - _cameraY) / PixelsPerUnit);
    }

    private PointF ScreenToWorld(int coordinate)
    {
        return new PointF((coordinate - _cameraX) / PixelsPerUnit, (coordinate - _cameraY) / PixelsPerUnit);
    }

    private PointF ScreenToWorld(PointF point)
    {
        return new PointF((point.X - _cameraX) / PixelsPerUnit, (point.Y - _cameraY) / PixelsPerUnit);
    }

    private float PixelsPerUnit => _zoomPercent / 100f;

    private NodeHitInfo? HitTest(PointF worldPoint)
    {
        foreach (var layout in _layoutCache.Values.OrderByDescending(info => info.Bounds.Top))
        {
            if (!layout.Bounds.Contains(worldPoint))
            {
                continue;
            }

            foreach (var socket in layout.Inputs.Concat(layout.Outputs))
            {
                if (socket.Bounds.Contains(worldPoint))
                {
                    return new NodeHitInfo(layout, null, socket);
                }
            }

            foreach (var field in layout.Fields)
            {
                if (field.CanConnectInput)
                {
                    var leftSocket = field.LeftSocketBounds;
                    if (leftSocket.Contains(worldPoint))
                    {
                        return new NodeHitInfo(layout, field, new NodeSocketHit(layout.NodeId, field.PortName, field.ValueType, true, false, leftSocket));
                    }
                }

                if (field.CanConnectOutput)
                {
                    var rightSocket = field.RightSocketBounds;
                    if (rightSocket.Contains(worldPoint))
                    {
                        return new NodeHitInfo(layout, field, new NodeSocketHit(layout.NodeId, field.PortName, field.ValueType, false, true, rightSocket));
                    }
                }

                if (field.Bounds.Contains(worldPoint))
                {
                    return new NodeHitInfo(layout, field, null);
                }
            }

            return new NodeHitInfo(layout, null, null);
        }

        return null;
    }

    private void ToggleBooleanField(NodeFieldHit field)
    {
        if (_graph is null)
        {
            return;
        }

        var node = ResolveNode(field.NodeId);
        if (node is null)
        {
            return;
        }

        var current = GetParameter(node.Parameters, field.Key, "false");
        node.Parameters[field.Key] = !string.Equals(current, "true", StringComparison.OrdinalIgnoreCase) ? "true" : "false";
        GraphChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    private void BeginInlineEdit(NodeFieldHit field)
    {
        var screenRect = Rectangle.Round(ToScreen(field.Bounds));
        screenRect.Inflate(-2, -3);
        _editTarget = new EditTarget(field.NodeId, field.Key, field.ValueType, field.Editable, false);
        StartInlineEditor(screenRect, field.Value, field.ValueType);
    }

    private void BeginNodeTitleEdit(GraphNodeDefinition node)
    {
        var layout = _layoutCache.TryGetValue(node.Id, out var info) ? info : NodeGraphLayoutService.BuildLayout(_localization, Font, node);
        var titleRect = Rectangle.Round(ToScreen(layout.Bounds));
        titleRect.Inflate(-12, -8);
        titleRect.Height = 24;
        titleRect.X += 92;
        titleRect.Width -= 100;
        _editTarget = new EditTarget(node.Id, "__title", NodePortValueTypes.String, true, true);
        StartInlineEditor(titleRect, node.Title, NodePortValueTypes.String);
    }

    private void StartInlineEditor(Rectangle bounds, string text, string valueType)
    {
        _inlineDropdown.Visible = false;
        _inlineEditor.Bounds = bounds;
        _inlineEditor.Text = text;
        _inlineEditor.Visible = true;
        _inlineEditor.Tag = valueType;
        _inlineEditor.SelectAll();
        _inlineEditor.Focus();
        _editing = true;
    }

    private void CommitInlineEditor()
    {
        if (!_editing || _editTarget is null)
        {
            return;
        }

        if (_graph is null)
        {
            return;
        }

        var node = ResolveNode(_editTarget.NodeId);
        if (node is null)
        {
            return;
        }

        if (_editTarget.IsTitle)
        {
            node.Title = _inlineEditor.Text.Trim();
        }
        else
        {
            node.Parameters[_editTarget.FieldKey] = _inlineEditor.Text.Trim();
        }

        _inlineEditor.Visible = false;
        _inlineDropdown.Visible = false;
        _editing = false;
        _editTarget = null;
        GraphChanged?.Invoke(this, EventArgs.Empty);
        RebuildLayout();
        Invalidate();
    }

    private void FinishConnection(Point mouseLocation)
    {
        var source = _connectingSocket;
        _connectingSocket = null;
        var snappedSocket = _connectionSnapSocket;
        _connectionSnapSocket = null;
        var changed = _connectionChanged;
        _connectionChanged = false;
        if (source is null || _graph is null)
        {
            Invalidate();
            return;
        }

        var world = ScreenToWorld(mouseLocation);
        var hit = HitTest(world);
        if ((hit?.Socket is null || !hit.Socket.IsInput) && snappedSocket is not null)
        {
            hit = new NodeHitInfo(_layoutCache[snappedSocket.NodeId], FindFieldBySocket(snappedSocket), snappedSocket);
        }
        if (hit?.Socket is null || !hit.Socket.IsInput)
        {
            if (changed)
            {
                GraphChanged?.Invoke(this, EventArgs.Empty);
                RebuildLayout();
            }
            Invalidate();
            return;
        }

        if (string.Equals(source.NodeId, hit.Socket.NodeId, StringComparison.OrdinalIgnoreCase))
        {
            if (changed)
            {
                GraphChanged?.Invoke(this, EventArgs.Empty);
                RebuildLayout();
            }
            Invalidate();
            return;
        }

        if (!ArePortsCompatible(source.ValueType, hit.Socket.ValueType))
        {
            if (changed)
            {
                GraphChanged?.Invoke(this, EventArgs.Empty);
                RebuildLayout();
            }
            Invalidate();
            return;
        }

        var edge = new GraphEdgeDefinition
        {
            FromNodeId = source.NodeId,
            FromPort = source.PortName,
            ToNodeId = hit.Socket.NodeId,
            ToPort = hit.Socket.PortName,
            ValueType = NormalizePortType(source.ValueType)
        };

        if (_graph.Edges.Any(existing =>
                string.Equals(existing.FromNodeId, edge.FromNodeId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(existing.ToNodeId, edge.ToNodeId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(existing.FromPort, edge.FromPort, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(existing.ToPort, edge.ToPort, StringComparison.OrdinalIgnoreCase)))
        {
            if (changed)
            {
                GraphChanged?.Invoke(this, EventArgs.Empty);
                RebuildLayout();
            }
            Invalidate();
            return;
        }

        _graph.Edges.Add(edge);
        changed = true;
        _persistentVisibleNodeIds.Add(edge.ToNodeId);
        _persistentVisibleNodeIds.Add(edge.FromNodeId);
        if (changed)
        {
            GraphChanged?.Invoke(this, EventArgs.Empty);
            RebuildLayout();
        }
        Invalidate();
    }

    private bool TryDetachIncomingAndStartReconnect(NodeSocketHit targetInputSocket, Point mouseLocation)
    {
        if (_graph is null || !targetInputSocket.IsInput)
        {
            return false;
        }

        EnsureLayout();
        var edge = _graph.Edges.FirstOrDefault(existing =>
            string.Equals(existing.ToNodeId, targetInputSocket.NodeId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(existing.ToPort, targetInputSocket.PortName, StringComparison.OrdinalIgnoreCase));
        if (edge is null)
        {
            return false;
        }

        var sourceSocket = TryResolveOutputSocket(edge.FromNodeId, edge.FromPort);
        if (sourceSocket is null)
        {
            return false;
        }

        _graph.Edges.Remove(edge);
        _persistentVisibleNodeIds.Add(targetInputSocket.NodeId);
        _persistentVisibleNodeIds.Add(edge.FromNodeId);
        var targetNode = ResolveNode(targetInputSocket.NodeId);
        if (targetNode is not null)
        {
            EnsureOwnerForRootScope(targetNode);
        }
        _connectingSocket = sourceSocket;
        _connectionStartScreen = Point.Round(GetSocketScreenCenter(sourceSocket));
        _connectionCurrentScreen = TryGetSnappedInputSocket(mouseLocation, out var snappedSocket)
            ? Point.Round(GetSocketScreenCenter(snappedSocket))
            : mouseLocation;
        _connectionChanged = true;
        Invalidate();
        return true;
    }

    private static bool ArePortsCompatible(string fromType, string toType)
    {
        var sourceType = NormalizePortType(fromType);
        var targetType = NormalizePortType(toType);
        if (sourceType == NodePortValueTypes.Flow || targetType == NodePortValueTypes.Flow)
        {
            return string.Equals(sourceType, targetType, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(sourceType, targetType, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePortType(string type)
    {
        return string.IsNullOrWhiteSpace(type) ? NodePortValueTypes.String : type;
    }

    private PointF GetSocketScreenCenter(NodeSocketHit socket)
    {
        var center = new PointF(socket.Bounds.Left + socket.Bounds.Width / 2f, socket.Bounds.Top + socket.Bounds.Height / 2f);
        return ToScreen(center);
    }

    private bool TryGetSnappedInputSocket(Point mouseLocation, out NodeSocketHit socket)
    {
        socket = null!;
        var bestDistance = float.MaxValue;

        foreach (var layout in _layoutCache.Values)
        {
            foreach (var candidate in layout.Inputs)
            {
                var center = GetSocketScreenCenter(candidate);
                var dx = mouseLocation.X - center.X;
                var dy = mouseLocation.Y - center.Y;
                var distance = MathF.Sqrt(dx * dx + dy * dy);
                if (distance > NodeGraphCanvasStyle.ConnectionSnapDistance || distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                socket = candidate;
            }

            foreach (var field in layout.Fields.Where(field => field.CanConnectInput))
            {
                var candidate = new NodeSocketHit(layout.NodeId, field.PortName, field.ValueType, true, false, field.LeftSocketBounds);
                var center = GetSocketScreenCenter(candidate);
                var dx = mouseLocation.X - center.X;
                var dy = mouseLocation.Y - center.Y;
                var distance = MathF.Sqrt(dx * dx + dy * dy);
                if (distance > NodeGraphCanvasStyle.ConnectionSnapDistance || distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                socket = candidate;
            }
        }

        return bestDistance < float.MaxValue;
    }

    private NodeFieldHit? FindFieldBySocket(NodeSocketHit socket)
    {
        if (!_layoutCache.TryGetValue(socket.NodeId, out var layout))
        {
            return null;
        }

        return layout.Fields.FirstOrDefault(field =>
            string.Equals(field.PortName, socket.PortName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(field.ValueType, socket.ValueType, StringComparison.OrdinalIgnoreCase));
    }

    private PointF GetPortCenter(string nodeId, string portName, bool output)
    {
        if (_layoutCache.TryGetValue(nodeId, out var layout))
        {
            var ports = output ? layout.Outputs : layout.Inputs;
            var socket = ports.FirstOrDefault(port => string.Equals(port.PortName, portName, StringComparison.OrdinalIgnoreCase));
            if (socket is not null)
            {
                return GetSocketScreenCenter(socket);
            }

            foreach (var field in layout.Fields)
            {
                if (!string.Equals(field.PortName, portName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (output && field.CanConnectOutput)
                {
                    return GetSocketCenter(field.RightSocketBounds);
                }

                if (!output && field.CanConnectInput)
                {
                    return GetSocketCenter(field.LeftSocketBounds);
                }
            }
        }

        return Point.Empty;
    }

    private NodeSocketHit? TryResolveOutputSocket(string nodeId, string portName)
    {
        if (!_layoutCache.TryGetValue(nodeId, out var layout))
        {
            return null;
        }

        var flowSocket = layout.Outputs.FirstOrDefault(port => string.Equals(port.PortName, portName, StringComparison.OrdinalIgnoreCase));
        if (flowSocket is not null)
        {
            return flowSocket;
        }

        var field = layout.Fields.FirstOrDefault(existing =>
            existing.CanConnectOutput &&
            string.Equals(existing.PortName, portName, StringComparison.OrdinalIgnoreCase));
        if (field is null)
        {
            return null;
        }

        return new NodeSocketHit(nodeId, field.PortName, field.ValueType, false, true, field.RightSocketBounds);
    }

    private void InlineEditor_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            CommitInlineEditor();
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.Escape)
        {
            _inlineEditor.Visible = false;
            _inlineDropdown.Visible = false;
            _editing = false;
            _editTarget = null;
            Invalidate();
            e.Handled = true;
        }
    }

    private void InlineEditor_Leave(object? sender, EventArgs e)
    {
        if (_editing)
        {
            CommitInlineEditor();
        }
    }

    private void InlineDropdown_SelectionChangeCommitted(object? sender, EventArgs e)
    {
        CommitInlineDropdown();
    }

    private void InlineDropdown_Leave(object? sender, EventArgs e)
    {
        if (_editing && _inlineDropdown.Visible)
        {
            CommitInlineDropdown();
        }
    }

    private bool BeginQuickFieldEdit(NodeFieldHit field)
    {
        if (_graph is null)
        {
            return false;
        }

        var node = ResolveNode(field.NodeId);
        if (node is null)
        {
            return false;
        }

        var options = GetQuickFieldOptions(node, field).ToList();
        if (options.Count == 0)
        {
            return false;
        }

        if (_editing)
        {
            CommitInlineEditor();
        }

        var screenRect = GetQuickEditorBounds(field);
        _editTarget = new EditTarget(field.NodeId, field.Key, field.ValueType, field.Editable, false);
        _inlineEditor.Visible = false;
        _inlineDropdown.Bounds = screenRect;
        _inlineDropdown.Items.Clear();
        _inlineDropdown.Items.AddRange(options.Cast<object>().ToArray());
        _inlineDropdown.Visible = true;
        _inlineDropdown.BringToFront();

        var rawValue = GetParameter(node.Parameters, field.Key, string.Empty);
        var selected = options.FirstOrDefault(option => string.Equals(option.Value, rawValue, StringComparison.OrdinalIgnoreCase));
        if (selected is not null)
        {
            _inlineDropdown.SelectedItem = selected;
        }
        else if (options.Count > 0)
        {
            _inlineDropdown.SelectedIndex = 0;
        }

        _inlineDropdown.Focus();
        _inlineDropdown.DroppedDown = true;
        _editing = true;
        return true;
    }

    private Rectangle GetQuickEditorBounds(NodeFieldHit field)
    {
        var rect = ToScreen(field.Bounds);
        var socketOffset = field.CanConnectInput ? 22f : 0f;
        var contentLeft = rect.X + 10 + socketOffset;
        var contentRight = field.CanConnectOutput ? rect.Right - 28 : rect.Right - 10;
        var labelRect = new RectangleF(contentLeft, rect.Y, Math.Max(40, (contentRight - contentLeft) * 0.52f), rect.Height);
        var valueRect = new RectangleF(labelRect.Right + 4, rect.Y + 2, Math.Max(20, contentRight - labelRect.Right - 4), Math.Max(18f, rect.Height - 4));
        return Rectangle.Round(valueRect);
    }

    private void CommitInlineDropdown()
    {
        if (!_editing || _editTarget is null || _graph is null)
        {
            return;
        }

        var node = ResolveNode(_editTarget.NodeId);
        if (node is null)
        {
            return;
        }

        if (_inlineDropdown.SelectedItem is QuickFieldOption option)
        {
            node.Parameters[_editTarget.FieldKey] = option.Value;
        }

        _inlineDropdown.Visible = false;
        _editing = false;
        _editTarget = null;
        GraphChanged?.Invoke(this, EventArgs.Empty);
        RebuildLayout();
        Invalidate();
    }

    private IEnumerable<QuickFieldOption> GetQuickFieldOptions(GraphNodeDefinition node, NodeFieldHit field)
    {
        foreach (var option in GraphNodeCatalog.GetFieldOptions(node.Kind, field.Key))
        {
            yield return new QuickFieldOption(option.Value, option.Text);
        }
    }

    private PointF GetSocketCenter(RectangleF socketBounds)
    {
        var center = new PointF(socketBounds.Left + socketBounds.Width / 2f, socketBounds.Top + socketBounds.Height / 2f);
        return ToScreen(center);
    }

    private void ResetVisibleNodeScope(bool clearPersistentNodes)
    {
        _visibilityRootNodeId = _rootNode?.Id;
        if (clearPersistentNodes)
        {
            _persistentVisibleNodeIds.Clear();
        }
    }

    private string GetNodeOwner(GraphNodeDefinition node)
    {
        return GetParameter(node.Parameters, TriggerOwnerParameterKey, string.Empty);
    }

    private static string GetParameter(Dictionary<string, string> parameters, string key, string fallback)
    {
        return parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    private void EnsureOwnerForRootScope(GraphNodeDefinition node)
    {
        if (_rootNode is null || string.Equals(node.Kind, NodeKinds.Trigger, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!string.Equals(GetNodeOwner(node), _rootNode.Id, StringComparison.OrdinalIgnoreCase))
        {
            node.Parameters[TriggerOwnerParameterKey] = _rootNode.Id;
        }
    }

    private string GetNodeTitle(string nodeId)
    {
        var node = ResolveNode(nodeId);
        return node is null || string.IsNullOrWhiteSpace(node.Title) ? nodeId : node.Title;
    }

    private static Color ParseAccentColor(string accentColor)
    {
        return ColorTranslator.FromHtml($"#{accentColor}");
    }

    private static Color GetValueTypeColor(string valueType)
    {
        return valueType switch
        {
            NodePortValueTypes.Flow => Color.FromArgb(255, 182, 72),
            NodePortValueTypes.Bool => Color.FromArgb(90, 200, 120),
            NodePortValueTypes.Int => Color.FromArgb(120, 150, 255),
            NodePortValueTypes.Float => Color.FromArgb(186, 125, 255),
            NodePortValueTypes.String => Color.FromArgb(110, 205, 255),
            NodePortValueTypes.Entity => Color.FromArgb(255, 110, 155),
            NodePortValueTypes.Player => Color.FromArgb(255, 141, 110),
            NodePortValueTypes.Skill => Color.FromArgb(255, 190, 110),
            NodePortValueTypes.Item => Color.FromArgb(145, 220, 122),
            NodePortValueTypes.Area => Color.FromArgb(140, 210, 190),
            NodePortValueTypes.Vector2 => Color.FromArgb(120, 200, 255),
            NodePortValueTypes.AssetRef => Color.FromArgb(170, 170, 180),
            _ => Color.FromArgb(170, 170, 180)
        };
    }

    private static GraphicsPath CreateRoundedPath(RectangleF rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2f;
        var arc = new RectangleF(rect.X, rect.Y, diameter, diameter);
        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = rect.X;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static GraphicsPath CreateRoundedTopPath(RectangleF rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2f;
        var arc = new RectangleF(rect.X, rect.Y, diameter, diameter);
        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);
        path.AddLine(rect.Right, rect.Bottom, rect.X, rect.Bottom);
        path.CloseFigure();
        return path;
    }

    private sealed record NodeHitInfo(NodeLayoutInfo Node, NodeFieldHit? Field, NodeSocketHit? Socket);

    private sealed record EditTarget(string NodeId, string FieldKey, string ValueType, bool Editable, bool IsTitle);

    private sealed record QuickFieldOption(string Value, string Text)
    {
        public override string ToString() => Text;
    }
}

public sealed class NodeSelectionChangedEventArgs : EventArgs
{
    public NodeSelectionChangedEventArgs(string? nodeId)
    {
        NodeId = nodeId;
    }

    public string? NodeId { get; }
}

public sealed class NodeContextRequestedEventArgs : EventArgs
{
    public NodeContextRequestedEventArgs(string nodeId, Point location, PointF worldLocation)
    {
        NodeId = nodeId;
        Location = location;
        WorldLocation = worldLocation;
    }

    public string NodeId { get; }

    public Point Location { get; }

    public PointF WorldLocation { get; }
}

public sealed class CanvasContextRequestedEventArgs : EventArgs
{
    public CanvasContextRequestedEventArgs(Point location, PointF worldLocation)
    {
        Location = location;
        WorldLocation = worldLocation;
    }

    public Point Location { get; }

    public PointF WorldLocation { get; }
}
