# LocalTTS Reader View - Project Specification

## Overview
Extend the existing LocalTTS WPF application to include a distraction-free reader view that displays selected text in a clean, customizable format. This will work alongside the existing text-to-speech functionality, giving users the choice to either listen to text or read it in an optimized display.

## Current State
LocalTTS is a WPF application that:
- Listens for Ctrl+Shift+R hotkey globally
- Captures selected text from any application
- Sends text to a FastAPI backend (Kokoro TTS in Docker)
- Plays the generated audio

## New Feature: Reader View

### User Experience

**Hotkey Behavior:**
- Single press Ctrl+Shift+R: Read aloud (existing behavior)
- Double press Ctrl+Shift+R (within 500ms): Open reader view with selected text
- Alternative: Add a modifier like Ctrl+Shift+Alt+R for reader view

**Reader Window:**
- Clean, minimal window that displays extracted text
- Centered on the current monitor
- Resizable, but defaults to comfortable reading width (600-800px)
- Stays on top initially, but can be toggled
- Press Esc to close
- Can have multiple reader windows open simultaneously (each new selection opens a new window, or reuses the most recent one - user preference)

### Technical Implementation

#### 1. Text Extraction & Cleaning
Create a `TextProcessor` class that:
- Takes raw selected text as input
- Removes excessive whitespace and normalizes line breaks
- Fixes common encoding issues (smart quotes, em-dashes, etc.)
- Preserves paragraph structure
- Removes URLs and email addresses (optional, user setting)
- Returns cleaned text ready for display

Example:
```csharp
public class TextProcessor
{
    public string Clean(string rawText)
    {
        // Remove excessive whitespace
        // Fix encoding issues  
        // Preserve paragraph breaks
        // Return cleaned text
    }
}
```

#### 2. Reader Window (ReaderWindow.xaml)
A new WPF window with:

**XAML Structure:**
```xml
<Window>
    <DockPanel>
        <!-- Top toolbar (optional, can be minimal) -->
        <StackPanel DockPanel.Dock="Top" Orientation="Horizontal">
            <Button Content="▶ Read Aloud" />
            <Button Content="Copy" />
            <Button Content="+" ToolTip="Increase font size" />
            <Button Content="-" ToolTip="Decrease font size" />
            <ToggleButton Content="🌙" ToolTip="Toggle dark mode" />
        </StackPanel>
        
        <!-- Main text display -->
        <FlowDocumentScrollViewer>
            <FlowDocument 
                FontFamily="Segoe UI" 
                FontSize="18"
                LineHeight="1.6"
                TextAlignment="Left"
                ColumnWidth="600"
                Foreground="{DynamicResource TextBrush}">
                <!-- Text content goes here -->
            </FlowDocument>
        </FlowDocumentScrollViewer>
    </DockPanel>
</Window>
```

**Key Features:**
- Use WPF's `FlowDocument` for proper text rendering
- Support dynamic font sizing (Ctrl+Plus/Ctrl+Minus)
- Implement dark/light theme switching
- Save window position/size between sessions
- Handle Esc key to close window

#### 3. Settings Extension
Add to existing LocalTTS settings:

```
Reader View Settings:
☑ Enable reader view
Trigger: [Double-tap Ctrl+Shift+R ▼]
         Options: Double-tap, Hold, Separate hotkey

Display:
Font: [Segoe UI ▼]
Size: [18 ▼]
Line Height: [1.6 ▼]
Max Width: [600px ▼]
Theme: ○ Light ● Dark ○ System

☑ Clean extracted text
☑ Remove URLs
☐ Preserve formatting
```

#### 4. Integration Points

**Modify existing hotkey handler:**
```csharp
private DateTime lastKeyPress = DateTime.MinValue;
private const int DoublePressDurationMs = 500;

private void OnHotkeyPressed()
{
    string selectedText = GetSelectedText();
    if (string.IsNullOrWhiteSpace(selectedText)) return;
    
    var now = DateTime.Now;
    var timeSinceLastPress = (now - lastKeyPress).TotalMilliseconds;
    
    if (timeSinceLastPress < DoublePressDurationMs)
    {
        // Double press detected - open reader
        OpenReaderView(selectedText);
    }
    else
    {
        // Single press - read aloud (existing behavior)
        ReadAloud(selectedText);
    }
    
    lastKeyPress = now;
}

private void OpenReaderView(string rawText)
{
    var processor = new TextProcessor();
    var cleanedText = processor.Clean(rawText);
    
    var reader = new ReaderWindow(cleanedText, Settings);
    reader.Show();
}
```

### Optional Enhancements (Phase 2)

**TTS Integration in Reader:**
- "Read Aloud" button in reader window that sends text to existing TTS system
- Highlight current word/sentence being spoken
- Sync scroll position with audio playback

**Advanced Text Processing:**
- Integrate readability algorithms (port of Mozilla's Readability)
- Smart paragraph detection
- Extract main content from HTML (if applicable)

**Reading Aids:**
- Adjustable text highlighting/focus mode
- Bookmark current position
- Save frequently accessed text snippets
- Reading progress indicator

**Accessibility:**
- Dyslexia-friendly font options (OpenDyslexic, Atkinson Hyperlegible)
- Bionic reading mode (bold first letters)
- Customizable color schemes for visual comfort
- Per-user profiles for different use cases

### Technical Constraints & Considerations

1. **Performance:** Reader window should open instantly (<100ms)
2. **Memory:** Each reader window should be lightweight; clean up properly on close
3. **Settings Persistence:** Store user preferences (font, size, theme) in existing settings file
4. **Window Management:** Handle multiple monitors correctly; remember last position
5. **Thread Safety:** Ensure UI updates happen on UI thread when calling from hotkey handler

### File Structure
```
LocalTTS/
├── MainWindow.xaml (existing)
├── SettingsWindow.xaml (existing - extend)
├── ReaderWindow.xaml (new)
├── ReaderWindow.xaml.cs (new)
├── TextProcessor.cs (new)
├── Settings.cs (existing - extend with reader settings)
└── HotkeyHandler.cs (existing - modify for double-press detection)
```

### Testing Checklist
- [ ] Double-press detection works reliably
- [ ] Text cleaning handles various input formats
- [ ] Window opens on correct monitor
- [ ] Theme switching works without restart
- [ ] Font size adjustment works (keyboard & buttons)
- [ ] Esc key closes window
- [ ] Settings persist between sessions
- [ ] Works with non-English text/Unicode
- [ ] Memory cleanup when closing windows
- [ ] Multiple reader windows can coexist

### Success Criteria
A user should be able to:
1. Highlight text anywhere in Windows
2. Press Ctrl+Shift+R twice quickly
3. See text appear in clean, readable format within 200ms
4. Adjust display settings to their preference
5. Close with Esc or by clicking elsewhere
6. Have their preferences remembered for next time

### Implementation Notes
- Start with minimal viable version: basic window + text display + Esc to close
- Add settings/customization in second pass
- TTS integration from reader window is Phase 2
- Focus on rock-solid hotkey detection first - this is the core UX

### Questions to Resolve
1. Should reader window be modal or modeless?
2. Reuse single window vs create new window per selection?
3. Should reader window steal focus or appear behind current window?
4. Maximum number of simultaneous reader windows allowed?
5. Auto-save text history for quick access later?

---

**Goal:** Extend LocalTTS to be not just a TTS tool, but a comprehensive "text consumption assistant" that lets users choose how they want to engage with text - by listening or by reading in an optimized format.
