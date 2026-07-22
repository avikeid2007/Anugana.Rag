using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;

namespace Anugana.Rag.Presentation;

public sealed partial class ChatPage : Page
{
    public ChatPage()
    {
        this.InitializeComponent();
    }

    private void InputTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter || e.OriginalKey == VirtualKey.Enter)
        {
            bool isShiftPressed = false;
            try
            {
                var shiftState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
                isShiftPressed = (shiftState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
            }
            catch
            {
                try
                {
                    var windowState = CoreWindow.GetForCurrentThread()?.GetKeyState(VirtualKey.Shift);
                    if (windowState.HasValue)
                    {
                        isShiftPressed = (windowState.Value & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
                    }
                }
                catch { }
            }

            if (!isShiftPressed)
            {
                e.Handled = true; // Stop newline insertion in TextBox

                if (DataContext is ChatViewModel vm && vm.SendMessageCommand.CanExecute(null))
                {
                    vm.SendMessageCommand.Execute(null);
                }
            }
        }
    }
}
