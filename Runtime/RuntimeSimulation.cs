namespace Axe2DEditor.Runtime;

internal sealed class RuntimeSimulation
{
    private readonly RuntimeSession _session;
    private readonly HashSet<Keys> _pressedKeys = [];
    private readonly float _playerSpeedPerSecond;

    public RuntimeSimulation(RuntimeSession session, float playerSpeedPerSecond = 4.2f)
    {
        _session = session;
        _playerSpeedPerSecond = playerSpeedPerSecond;
    }

    public void SetKeyState(Keys key, bool isPressed)
    {
        if (isPressed)
        {
            _pressedKeys.Add(key);
        }
        else
        {
            _pressedKeys.Remove(key);
        }
    }

    public void TeleportPlayer(PointF worldPosition)
    {
        _session.Player.X = worldPosition.X;
        _session.Player.Y = worldPosition.Y;
        ClampPlayer();
    }

    public void Step(float deltaSeconds)
    {
        var dx = 0f;
        var dy = 0f;

        if (_pressedKeys.Contains(Keys.A) || _pressedKeys.Contains(Keys.Left))
        {
            dx -= _playerSpeedPerSecond * deltaSeconds;
        }

        if (_pressedKeys.Contains(Keys.D) || _pressedKeys.Contains(Keys.Right))
        {
            dx += _playerSpeedPerSecond * deltaSeconds;
        }

        if (_pressedKeys.Contains(Keys.W) || _pressedKeys.Contains(Keys.Up))
        {
            dy -= _playerSpeedPerSecond * deltaSeconds;
        }

        if (_pressedKeys.Contains(Keys.S) || _pressedKeys.Contains(Keys.Down))
        {
            dy += _playerSpeedPerSecond * deltaSeconds;
        }

        if (dx == 0f && dy == 0f)
        {
            return;
        }

        if (!_session.TryMovePlayer(dx, dy))
        {
            return;
        }

        _session.TriggerTouchEventsForPlayer();
        _session.TriggerAreaEventsForPlayer();
    }

    private void ClampPlayer()
    {
        _session.ClampPlayerToBounds();
    }
}
