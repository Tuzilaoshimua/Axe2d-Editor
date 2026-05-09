using System.Runtime.CompilerServices;

namespace Axe2DEditor.Editor.Controls;

internal static class TabSelectAllBehavior
{
    private sealed class State
    {
        public bool TabNavigationActive;
    }

    private sealed class AttachedState
    {
    }

    private sealed class Filter : IMessageFilter
    {
        public bool PreFilterMessage(ref Message m)
        {
            const int WmKeyDown = 0x0100;
            if (m.Msg != WmKeyDown || (Keys)m.WParam != Keys.Tab)
            {
                return false;
            }

            var control = Control.FromHandle(m.HWnd);
            var form = control?.FindForm();
            if (form is null)
            {
                return false;
            }

            if (_states.TryGetValue(form, out var state))
            {
                state.TabNavigationActive = true;
            }
            else
            {
                _states.GetValue(form, _ => new State()).TabNavigationActive = true;
            }

            return false;
        }
    }

    private static readonly ConditionalWeakTable<Form, State> _states = new();
    private static readonly ConditionalWeakTable<Control, AttachedState> _attachedControls = new();
    private static readonly Filter _filter = new();
    private static bool _installed;

    public static void Install(Form form)
    {
        if (!_installed)
        {
            Application.AddMessageFilter(_filter);
            _installed = true;
        }

        _states.GetValue(form, _ => new State());
        form.FormClosed += (_, _) => _states.Remove(form);
    }

    public static void InstallRecursive(Form form)
    {
        Install(form);
        AttachRecursive(form);
        form.ControlAdded += (_, e) =>
        {
            if (e.Control is not null)
            {
                AttachRecursive(e.Control);
            }
        };
    }

    public static void AttachRecursive(Control root)
    {
        AttachIfSupported(root);
        root.ControlAdded += (_, e) =>
        {
            if (e.Control is not null)
            {
                AttachRecursive(e.Control);
            }
        };
        foreach (Control child in root.Controls)
        {
            AttachRecursive(child);
        }
    }

    public static void Attach(TextBox box)
    {
        if (IsAttached(box))
        {
            return;
        }

        MarkAttached(box);
        box.Enter += (_, _) =>
        {
            if (!ShouldSelectAllOnEnter(box))
            {
                return;
            }

            if (box.IsHandleCreated && box.SelectionLength != box.TextLength)
            {
                box.BeginInvoke(new Action(box.SelectAll));
            }
        };

        box.MouseDown += (_, _) => ClearPendingSelectAll(box);
    }

    public static void Attach(NumericUpDown box)
    {
        if (IsAttached(box))
        {
            return;
        }

        MarkAttached(box);
        box.Enter += (_, _) =>
        {
            if (!ShouldSelectAllOnEnter(box))
            {
                return;
            }

            if (box.IsHandleCreated)
            {
                box.BeginInvoke(new Action(() =>
                {
                    box.Select(0, box.Text.Length);
                }));
            }
        };

        box.MouseDown += (_, _) => ClearPendingSelectAll(box);
    }

    public static void Attach(DomainUpDown box)
    {
        if (IsAttached(box))
        {
            return;
        }

        MarkAttached(box);
        box.Enter += (_, _) =>
        {
            if (!ShouldSelectAllOnEnter(box))
            {
                return;
            }

            if (box.IsHandleCreated)
            {
                box.BeginInvoke(new Action(() =>
                {
                    box.Select(0, box.Text.Length);
                }));
            }
        };

        box.MouseDown += (_, _) => ClearPendingSelectAll(box);
    }

    public static void Attach(ComboBox box)
    {
        if (IsAttached(box))
        {
            return;
        }

        MarkAttached(box);
        box.Enter += (_, _) =>
        {
            if (!ShouldSelectAllOnEnter(box))
            {
                return;
            }

            if (box.IsHandleCreated && box.DropDownStyle == ComboBoxStyle.DropDown)
            {
                box.BeginInvoke(new Action(() =>
                {
                    box.Select(0, box.Text.Length);
                }));
            }
        };

        box.MouseDown += (_, _) => ClearPendingSelectAll(box);
    }

    private static void AttachIfSupported(Control control)
    {
        switch (control)
        {
            case TextBox box:
                Attach(box);
                break;
            case NumericUpDown box:
                Attach(box);
                break;
            case DomainUpDown box:
                Attach(box);
                break;
            case ComboBox box:
                Attach(box);
                break;
        }
    }

    private static bool ShouldSelectAllOnEnter(Control control)
    {
        var form = control.FindForm();
        if (form is null || !_states.TryGetValue(form, out var state) || !state.TabNavigationActive)
        {
            return false;
        }

        return true;
    }

    private static void ClearPendingSelectAll(Control control)
    {
        var form = control.FindForm();
        if (form is null || !_states.TryGetValue(form, out var state))
        {
            return;
        }

        state.TabNavigationActive = false;
    }

    private static bool IsAttached(Control control)
    {
        return _attachedControls.TryGetValue(control, out _);
    }

    private static void MarkAttached(Control control)
    {
        _attachedControls.GetValue(control, _ => new AttachedState());
    }
}
