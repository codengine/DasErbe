using Game.Shared.Diagnostics;
using Game.Shared.Host;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Game.Desktop.MonoGame;

/// <summary>
///     Owns borderless-window transitions and their focused pointer-confinement policy.
/// </summary>
internal sealed class MonoGameWindowController
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly GameWindow _window;
    private Point _windowedClientSize;
    private Point _windowedPosition;
    private bool _enterWasDown;
    private bool _hasWindowedPlacement;

    /// <summary>
    ///     Creates the controller and configures fullscreen transitions to avoid changing the monitor display mode.
    /// </summary>
    /// <param name="graphics">Graphics manager that applies window-mode transitions.</param>
    /// <param name="window">Game window whose placement and pointer bounds are controlled.</param>
    internal MonoGameWindowController(GraphicsDeviceManager graphics, GameWindow window)
    {
        _graphics = graphics;
        _window = window;
        _graphics.HardwareModeSwitch = false;
    }

    /// <summary>
    ///     Toggles borderless mode once when Enter is newly pressed while either Alt key is held.
    /// </summary>
    /// <param name="isActive">Whether the game window currently owns keyboard focus.</param>
    internal void HandleKeyboardShortcut(bool isActive)
    {
        var keyboard = Keyboard.GetState();
        var enterDown = keyboard.IsKeyDown(Keys.Enter);
        var shouldToggle = isActive && enterDown && !_enterWasDown &&
                           (keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt));
        _enterWasDown = enterDown;
        if (!shouldToggle)
        {
            return;
        }

        var enteringBorderless = !_graphics.IsFullScreen;
        if (enteringBorderless)
        {
            _windowedClientSize = new Point(_window.ClientBounds.Width, _window.ClientBounds.Height);
            _windowedPosition = _window.Position;
            _hasWindowedPlacement = true;
            _graphics.PreferredBackBufferWidth = _windowedClientSize.X;
            _graphics.PreferredBackBufferHeight = _windowedClientSize.Y;
        }
        else if (_hasWindowedPlacement)
        {
            _graphics.PreferredBackBufferWidth = _windowedClientSize.X;
            _graphics.PreferredBackBufferHeight = _windowedClientSize.Y;
        }

        _graphics.ToggleFullScreen();
        if (!enteringBorderless && _hasWindowedPlacement)
        {
            _window.Position = _windowedPosition;
        }

        GameLog.Write(LoggingChannel.Program,
            enteringBorderless ? "Entered borderless window mode." : "Exited borderless window mode.");
    }

    /// <summary>
    ///     Keeps the physical pointer inside the visible game rect while borderless mode is focused.
    /// </summary>
    /// <param name="rect">Visible game-content bounds in window coordinates.</param>
    /// <param name="isActive">Whether the game window currently owns input focus.</param>
    internal void ConstrainMouse(HostPresentationRect rect, bool isActive)
    {
        if (!isActive || !_graphics.IsFullScreen)
        {
            return;
        }

        var mouse = Mouse.GetState();
        var constrainedX = Math.Clamp(mouse.X, rect.ContentX, rect.ContentX + rect.ContentWidth - 1);
        var constrainedY = Math.Clamp(mouse.Y, rect.ContentY, rect.ContentY + rect.ContentHeight - 1);
        if (mouse.X != constrainedX || mouse.Y != constrainedY)
        {
            Mouse.SetPosition(constrainedX, constrainedY);
        }
    }
}
