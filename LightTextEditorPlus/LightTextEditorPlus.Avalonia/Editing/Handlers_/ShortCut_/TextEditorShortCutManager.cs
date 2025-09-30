using Avalonia.Input;

using System;
using System.Collections.Generic;

namespace LightTextEditorPlus.Editing;

public class TextEditorShortCutManager
{
    public void Add(TextEditorCommandName commandName, TextEditorKeyGesture keyGesture, TextEditorCommandHandler handler)
    {
        KeyBindings.Add(new TextEditorShortCutKeyBinding(commandName, keyGesture));
        CommandBindings.Add(new TextEditorShortCutCommandBinding(commandName, handler));
    }

    public void AddKeyBindings(TextEditorShortCutKeyBinding keyBinding)
    {
        KeyBindings.Add(keyBinding);
    }

    public void AddKeyBindings(TextEditorCommandName commandName, TextEditorKeyGesture keyGesture)
    {
        KeyBindings.Add(new TextEditorShortCutKeyBinding(commandName, keyGesture));
    }

    public void AddCommandBindings(TextEditorShortCutCommandBinding commandBinding)
    {
        CommandBindings.Add(commandBinding);
    }

    public void AddCommandBindings(TextEditorCommandName commandName, TextEditorCommandHandler handler)
    {
        CommandBindings.Add(new TextEditorShortCutCommandBinding(commandName, handler));
    }

    public TextEditorShortCutCommandBindingCollection CommandBindings { get; } = new();

    public TextEditorShortCutKeyBindingCollection KeyBindings { get; } = new();

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

public class TextEditorShortCutCommandBindingCollection : List<TextEditorShortCutCommandBinding>
{
}

public class TextEditorShortCutKeyBindingCollection : List<TextEditorShortCutKeyBinding>
{
}

public readonly record struct TextEditorKeyGesture(Key Key, KeyModifiers Modifiers);

public readonly record struct TextEditorShortCutCommandBinding(TextEditorCommandName CommandName, TextEditorCommandHandler Handler)
{
}

public readonly record struct TextEditorShortCutKeyBinding(TextEditorCommandName CommandName, TextEditorKeyGesture KeyGesture)
{
}

public readonly record struct TextEditorCommandName(string Name);

public delegate void TextEditorCommandHandler(object sender, EventArgs e);