using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using qutCUT.Agent.Clients;
using qutCUT.Editor;

namespace qutCUT.Agent.Panel;

// Simple bubble model for the list
public sealed class MessageBubble(string content, bool isUser)
{
    public string Content { get; } = content;
    public HorizontalAlignment HorizontalAlignment { get; } = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;
    public Windows.UI.Color BubbleColor { get; } = isUser
        ? Windows.UI.Color.FromArgb(255, 99, 102, 241)
        : Windows.UI.Color.FromArgb(255, 36, 36, 42);
}

public sealed partial class AgentPanelView : UserControl
{
    private EditorViewModel? _viewModel;
    public EditorViewModel? ViewModel
    {
        get => _viewModel;
        set { _viewModel = value; Bindings.Update(); }
    }

    public bool CanSend => _viewModel?.AgentService.IsStreaming == false
                        && !string.IsNullOrWhiteSpace(InputBox?.Text);

    public IReadOnlyList<MessageBubble> Messages =>
        _viewModel?.AgentService.ActiveSession.Messages
            .Select(m => new MessageBubble(m.Content, m.Role == "user"))
            .ToList() ?? [];

    public AgentPanelView() => InitializeComponent();

    private void OnInputKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter && !Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
        {
            e.Handled = true;
            SendMessage();
        }
    }

    private void OnSend(object sender, RoutedEventArgs e) => SendMessage();

    private void SendMessage()
    {
        var text = InputBox.Text.Trim();
        if (string.IsNullOrEmpty(text) || _viewModel is null) return;
        InputBox.Text = string.Empty;
        _ = SendAsync(text);
    }

    private async Task SendAsync(string text)
    {
        if (_viewModel is null) return;
        var agent = _viewModel.AgentService;

        Bindings.Update();

        await agent.SendAsync(
            text,
            systemPrompt: "You are an AI video editor assistant inside qutCUT. Help the user edit their video timeline.",
            tools: [],
            onToolCall: (_, _, _) => Task.FromResult("{}"),
            onTextDelta: _ =>
            {
                DispatcherQueue.TryEnqueue(() => Bindings.Update());
            });

        DispatcherQueue.TryEnqueue(() =>
        {
            Bindings.Update();
            MessagesScroll.ScrollToVerticalOffset(MessagesScroll.ScrollableHeight);
        });
    }

    private void OnModelChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel is null) return;
        var tag = (ModelPicker.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        if (Enum.TryParse<AnthropicModel>(tag, out var model))
            _viewModel.AgentService.Model = model;
    }
}
