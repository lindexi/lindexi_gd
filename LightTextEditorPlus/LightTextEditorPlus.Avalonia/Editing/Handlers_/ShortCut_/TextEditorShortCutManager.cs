using Avalonia.Input;

using System;
using System.Collections.Generic;

namespace LightTextEditorPlus.Editing;

/// <summary>
/// Manages keyboard shortcuts (key bindings) and their associated command handlers
/// for the text editor.
/// </summary>
public class TextEditorShortCutManager
{
    /// <summary>
    /// Adds a command with its key gesture and handler to the manager.
    /// </summary>
    /// <param name="commandName">The name of the command to add.</param>
    /// <param name="keyGesture">The key gesture that triggers the command.</param>
    /// <param name="handler">The handler to invoke when the command is executed.</param>
    public void Add(TextEditorCommandName commandName, TextEditorKeyGesture keyGesture, TextEditorCommandHandler handler)
    {
        KeyBindings.Add(new TextEditorShortCutKeyBinding(commandName, keyGesture));
        CommandBindings.Add(new TextEditorShortCutCommandBinding(commandName, handler));
    }

    /// <summary>
    /// Adds a pre-constructed key binding to the manager.
    /// </summary>
    /// <param name="keyBinding">The key binding to add.</param>
    public void AddKeyBindings(TextEditorShortCutKeyBinding keyBinding)
    {
        KeyBindings.Add(keyBinding);
    }

    /// <summary>
    /// Adds a key binding defined by command name and key gesture.
    /// </summary>
    /// <param name="commandName">The name of the command to bind.</param>
    /// <param name="keyGesture">The key gesture that triggers the command.</param>
    public void AddKeyBindings(TextEditorCommandName commandName, TextEditorKeyGesture keyGesture)
    {
        KeyBindings.Add(new TextEditorShortCutKeyBinding(commandName, keyGesture));
    }

    /// <summary>
    /// Adds a pre-constructed command binding to the manager.
    /// </summary>
    /// <param name="commandBinding">The command binding to add.</param>
    public void AddCommandBindings(TextEditorShortCutCommandBinding commandBinding)
    {
        CommandBindings.Add(commandBinding);
    }

    /// <summary>
    /// Adds a command binding defined by command name and handler.
    /// </summary>
    /// <param name="commandName">The name of the command.</param>
    /// <param name="handler">The handler to execute for the command.</param>
    public void AddCommandBindings(TextEditorCommandName commandName, TextEditorCommandHandler handler)
    {
        CommandBindings.Add(new TextEditorShortCutCommandBinding(commandName, handler));
    }

    /// <summary>
    /// Gets the collection of command bindings managed by this instance.
    /// </summary>
    public TextEditorShortCutCommandBindingCollection CommandBindings { get; } = new();

    /// <summary>
    /// Gets the collection of key bindings managed by this instance.
    /// </summary>
    public TextEditorShortCutKeyBindingCollection KeyBindings { get; } = new();

    /// <summary>
    /// Finds the last-added key binding that matches the specified key gesture.
    /// Returns null if no matching binding is found.
    /// </summary>
    /// <param name="keyGesture">The key gesture to search for.</param>
    /// <returns>The matching key binding or <c>null</c> if not found.</returns>
    public TextEditorShortCutKeyBinding? FindShortCutKeyBinding(in TextEditorKeyGesture keyGesture)
    {
        for (var i = KeyBindings.Count - 1; i >= 0; i--)
        {
            var keyBinding = KeyBindings[i];
            if (keyBinding.KeyGesture == keyGesture)
            {
                return keyBinding;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the command binding associated with the given key gesture.
    /// If a key binding is found for the gesture, this method returns the
    /// corresponding command binding; otherwise returns <c>null</c>.
    /// </summary>
    /// <param name="keyGesture">The key gesture to look up.</param>
    /// <returns>The matching command binding or <c>null</c> if none exists.</returns>
    public TextEditorShortCutCommandBinding? FindCommandBinding(in TextEditorKeyGesture keyGesture)
    {
        TextEditorShortCutKeyBinding? keyBinding = FindShortCutKeyBinding(in keyGesture);
        if (keyBinding is not null)
        {
            TextEditorCommandName commandName = keyBinding.Value.CommandName;

            foreach (var commandBinding in CommandBindings)
            {
                if (commandBinding.CommandName == commandName)
                {
                    return commandBinding;
                }
            }
        }

        return null;
    }
}

/// <summary>
/// A collection of command bindings for the text editor shortcuts.
/// </summary>
public class TextEditorShortCutCommandBindingCollection : List<TextEditorShortCutCommandBinding>
{
}

/// <summary>
/// A collection of key bindings for the text editor shortcuts.
/// </summary>
public class TextEditorShortCutKeyBindingCollection : List<TextEditorShortCutKeyBinding>
{
}

/// <summary>
/// Represents a keyboard gesture consisting of a key and modifier keys.
/// </summary>
/// <param name="Key">The primary key of the gesture.</param>
/// <param name="Modifiers">Modifier keys (Ctrl, Alt, Shift, etc.).</param>
public readonly record struct TextEditorKeyGesture(Key Key, KeyModifiers Modifiers);

/// <summary>
/// Associates a command name with its handler.
/// </summary>
/// <param name="CommandName">The name of the command.</param>
/// <param name="Handler">The handler to execute for the command.</param>
public readonly record struct TextEditorShortCutCommandBinding(TextEditorCommandName CommandName, TextEditorCommandHandler Handler)
{
}

/// <summary>
/// Associates a command name with a key gesture.
/// </summary>
/// <param name="CommandName">The name of the command.</param>
/// <param name="KeyGesture">The key gesture that triggers the command.</param>
public readonly record struct TextEditorShortCutKeyBinding(TextEditorCommandName CommandName, TextEditorKeyGesture KeyGesture)
{
}

/// <summary>
/// Represents the identity of a command used by the text editor.
/// </summary>
/// <param name="Name">The string identifier of the command.</param>
public readonly record struct TextEditorCommandName(string Name);

/// <summary>
/// Delegate type for command handlers used by the text editor shortcuts.
/// </summary>
/// <param name="sender">The sender of the command invocation.</param>
/// <param name="e">Event arguments for the invocation.</param>
public delegate void TextEditorCommandHandler(object sender, EventArgs e);