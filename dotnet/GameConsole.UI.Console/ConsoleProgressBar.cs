namespace GameConsole.UI.Console;

/// <summary>
/// Progress bar component for displaying progress indicators and status.
/// </summary>
public class ConsoleProgressBar : BaseUIComponent
{
    private double _value = 0.0;
    private double _minimum = 0.0;
    private double _maximum = 100.0;
    private string _text = string.Empty;
    private bool _showPercentage = true;
    private bool _showBorder = false;
    private ProgressBarStyle _style = ProgressBarStyle.Block;
    private ConsoleColor _foregroundColor = ConsoleColor.Green;
    private ConsoleColor _backgroundColor = ConsoleColor.DarkGray;
    
    public ConsoleProgressBar(string id) : base(id)
    {
        CanFocus = false;
        DesiredBounds = new Rectangle(0, 0, 40, 1);
    }
    
    /// <summary>
    /// Gets or sets the current progress value.
    /// </summary>
    public double Value
    {
        get => _value;
        set
        {
            var newValue = Math.Max(_minimum, Math.Min(_maximum, value));
            if (Math.Abs(_value - newValue) > double.Epsilon)
            {
                _value = newValue;
                OnValueChanged();
            }
        }
    }
    
    /// <summary>
    /// Gets or sets the minimum value.
    /// </summary>
    public double Minimum
    {
        get => _minimum;
        set
        {
            if (_minimum != value)
            {
                _minimum = value;
                if (_value < _minimum) Value = _minimum;
            }
        }
    }
    
    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    public double Maximum
    {
        get => _maximum;
        set
        {
            if (_maximum != value)
            {
                _maximum = value;
                if (_value > _maximum) Value = _maximum;
            }
        }
    }
    
    /// <summary>
    /// Gets or sets the text to display on the progress bar.
    /// </summary>
    public string Text
    {
        get => _text;
        set => _text = value ?? string.Empty;
    }
    
    /// <summary>
    /// Gets or sets whether to show percentage text.
    /// </summary>
    public bool ShowPercentage
    {
        get => _showPercentage;
        set => _showPercentage = value;
    }
    
    /// <summary>
    /// Gets or sets whether to show a border around the progress bar.
    /// </summary>
    public bool ShowBorder
    {
        get => _showBorder;
        set => _showBorder = value;
    }
    
    /// <summary>
    /// Gets or sets the progress bar style.
    /// </summary>
    public ProgressBarStyle Style
    {
        get => _style;
        set => _style = value;
    }
    
    /// <summary>
    /// Gets or sets the foreground (progress) color.
    /// </summary>
    public ConsoleColor ForegroundColor
    {
        get => _foregroundColor;
        set => _foregroundColor = value;
    }
    
    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public ConsoleColor BackgroundColor
    {
        get => _backgroundColor;
        set => _backgroundColor = value;
    }
    
    /// <summary>
    /// Gets the current progress as a percentage (0-100).
    /// </summary>
    public double Percentage => _maximum > _minimum ? ((_value - _minimum) / (_maximum - _minimum)) * 100.0 : 0.0;
    
    /// <summary>
    /// Event fired when the progress value changes.
    /// </summary>
    public event EventHandler<ProgressChangedEventArgs>? ValueChanged;
    
    /// <summary>
    /// Sets the progress to the specified percentage (0-100).
    /// </summary>
    /// <param name="percentage">Percentage value (0-100).</param>
    public void SetPercentage(double percentage)
    {
        var range = _maximum - _minimum;
        Value = _minimum + (range * (percentage / 100.0));
    }
    
    /// <summary>
    /// Increments the progress value by the specified amount.
    /// </summary>
    /// <param name="amount">Amount to increment by.</param>
    public void Increment(double amount = 1.0)
    {
        Value += amount;
    }
    
    public override void Render(IConsoleUIFramework framework)
    {
        if (!IsVisible || ActualBounds.IsEmpty) return;
        
        ClearArea(framework);
        
        int contentX = ActualBounds.X;
        int contentY = ActualBounds.Y;
        int contentWidth = ActualBounds.Width;
        int contentHeight = ActualBounds.Height;
        
        // Draw border if enabled
        if (_showBorder)
        {
            framework.DrawBox(ActualBounds.X, ActualBounds.Y, ActualBounds.Width, ActualBounds.Height, BoxStyle.Single);
            contentX += 1;
            contentY += 1;
            contentWidth -= 2;
            contentHeight -= 2;
        }
        
        if (contentWidth < 1 || contentHeight < 1) return;
        
        // Calculate progress bar dimensions
        double progressRatio = _maximum > _minimum ? (_value - _minimum) / (_maximum - _minimum) : 0.0;
        int progressWidth = (int)(contentWidth * progressRatio);
        
        // Render multiple rows if height allows
        for (int row = 0; row < contentHeight; row++)
        {
            int currentY = contentY + row;
            
            // Draw progress bar background
            var backgroundChars = GetProgressChars(_style, false);
            var backgroundLine = new string(backgroundChars, contentWidth);
            framework.WriteAt(contentX, currentY, backgroundLine, ConsoleColor.White, _backgroundColor);
            
            // Draw progress bar foreground
            if (progressWidth > 0)
            {
                var foregroundChars = GetProgressChars(_style, true);
                var foregroundLine = new string(foregroundChars, progressWidth);
                framework.WriteAt(contentX, currentY, foregroundLine, ConsoleColor.White, _foregroundColor);
            }
        }
        
        // Draw text overlay
        var displayText = GetDisplayText();
        if (!string.IsNullOrEmpty(displayText) && displayText.Length <= contentWidth)
        {
            int textX = contentX + (contentWidth - displayText.Length) / 2;
            int textY = contentY + contentHeight / 2;
            
            framework.WriteAt(textX, textY, displayText, ConsoleColor.White, null, TextStyle.Bold);
        }
    }
    
    private string GetDisplayText()
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrEmpty(_text))
        {
            parts.Add(_text);
        }
        
        if (_showPercentage)
        {
            parts.Add($"{Percentage:F1}%");
        }
        
        return string.Join(" - ", parts);
    }
    
    private static char GetProgressChars(ProgressBarStyle style, bool filled)
    {
        return style switch
        {
            ProgressBarStyle.Block => filled ? '█' : '░',
            ProgressBarStyle.Hash => filled ? '#' : '-',
            ProgressBarStyle.Equal => filled ? '=' : '-',
            ProgressBarStyle.Arrow when filled => '▶',
            ProgressBarStyle.Arrow => '─',
            ProgressBarStyle.Dot => filled ? '●' : '○',
            ProgressBarStyle.Square => filled ? '■' : '□',
            _ => filled ? '█' : '░'
        };
    }
    
    protected virtual void OnValueChanged()
    {
        ValueChanged?.Invoke(this, new ProgressChangedEventArgs(_value, Percentage));
    }
}

/// <summary>
/// Progress bar visual styles.
/// </summary>
public enum ProgressBarStyle
{
    Block,
    Hash,
    Equal,
    Arrow,
    Dot,
    Square
}

/// <summary>
/// Event arguments for progress change events.
/// </summary>
public class ProgressChangedEventArgs : EventArgs
{
    public double Value { get; }
    public double Percentage { get; }
    
    public ProgressChangedEventArgs(double value, double percentage)
    {
        Value = value;
        Percentage = percentage;
    }
}

/// <summary>
/// Multi-step progress bar component for showing progress through multiple stages.
/// </summary>
public class ConsoleMultiStepProgressBar : BaseUIComponent
{
    private readonly List<ProgressStep> _steps = new();
    private int _currentStepIndex = 0;
    private bool _showStepNames = true;
    private bool _showBorder = false;
    
    public ConsoleMultiStepProgressBar(string id) : base(id)
    {
        CanFocus = false;
        DesiredBounds = new Rectangle(0, 0, 60, 3);
    }
    
    /// <summary>
    /// Gets or sets whether to show step names.
    /// </summary>
    public bool ShowStepNames
    {
        get => _showStepNames;
        set => _showStepNames = value;
    }
    
    /// <summary>
    /// Gets or sets whether to show a border.
    /// </summary>
    public bool ShowBorder
    {
        get => _showBorder;
        set => _showBorder = value;
    }
    
    /// <summary>
    /// Gets the current step index.
    /// </summary>
    public int CurrentStepIndex => _currentStepIndex;
    
    /// <summary>
    /// Gets the current step.
    /// </summary>
    public ProgressStep? CurrentStep => _currentStepIndex < _steps.Count ? _steps[_currentStepIndex] : null;
    
    /// <summary>
    /// Gets the total number of steps.
    /// </summary>
    public int StepCount => _steps.Count;
    
    /// <summary>
    /// Adds a step to the progress bar.
    /// </summary>
    /// <param name="name">Step name.</param>
    /// <param name="description">Step description.</param>
    /// <returns>The created step.</returns>
    public ProgressStep AddStep(string name, string description = "")
    {
        var step = new ProgressStep(name, description);
        _steps.Add(step);
        return step;
    }
    
    /// <summary>
    /// Sets the current step by index.
    /// </summary>
    /// <param name="stepIndex">Index of the step to set as current.</param>
    public void SetCurrentStep(int stepIndex)
    {
        if (stepIndex >= 0 && stepIndex < _steps.Count)
        {
            _currentStepIndex = stepIndex;
        }
    }
    
    /// <summary>
    /// Advances to the next step.
    /// </summary>
    /// <returns>True if advanced; false if already at the last step.</returns>
    public bool NextStep()
    {
        if (_currentStepIndex < _steps.Count - 1)
        {
            _currentStepIndex++;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Marks the current step as completed and advances to the next step.
    /// </summary>
    public void CompleteCurrentStep()
    {
        if (_currentStepIndex < _steps.Count)
        {
            _steps[_currentStepIndex].Status = StepStatus.Completed;
            NextStep();
        }
    }
    
    public override void Render(IConsoleUIFramework framework)
    {
        if (!IsVisible || ActualBounds.IsEmpty) return;
        
        ClearArea(framework);
        
        int contentX = ActualBounds.X;
        int contentY = ActualBounds.Y;
        int contentWidth = ActualBounds.Width;
        int contentHeight = ActualBounds.Height;
        
        // Draw border if enabled
        if (_showBorder)
        {
            framework.DrawBox(ActualBounds.X, ActualBounds.Y, ActualBounds.Width, ActualBounds.Height, BoxStyle.Single);
            contentX += 1;
            contentY += 1;
            contentWidth -= 2;
            contentHeight -= 2;
        }
        
        if (_steps.Count == 0 || contentWidth < 1 || contentHeight < 1) return;
        
        // Draw step indicators
        DrawStepIndicators(framework, contentX, contentY, contentWidth);
        
        // Draw current step details
        if (_showStepNames && contentHeight > 1)
        {
            var currentStep = CurrentStep;
            if (currentStep != null)
            {
                var stepText = $"Step {_currentStepIndex + 1}/{_steps.Count}: {currentStep.Name}";
                framework.WriteAt(contentX, contentY + 1, stepText, ConsoleColor.White, null, TextStyle.Bold);
                
                if (!string.IsNullOrEmpty(currentStep.Description) && contentHeight > 2)
                {
                    framework.WriteAt(contentX, contentY + 2, currentStep.Description, ConsoleColor.Gray);
                }
            }
        }
    }
    
    private void DrawStepIndicators(IConsoleUIFramework framework, int x, int y, int width)
    {
        if (_steps.Count == 0) return;
        
        int stepWidth = Math.Max(1, width / _steps.Count);
        int currentX = x;
        
        for (int i = 0; i < _steps.Count; i++)
        {
            var step = _steps[i];
            var stepChar = GetStepChar(step.Status, i == _currentStepIndex);
            var color = GetStepColor(step.Status, i == _currentStepIndex);
            
            framework.WriteAt(currentX, y, stepChar.ToString(), color, null, TextStyle.Bold);
            
            // Draw connector line (except for last step)
            if (i < _steps.Count - 1)
            {
                var connectorLength = stepWidth - 1;
                var connectorChar = i < _currentStepIndex ? '━' : '─';
                var connectorColor = i < _currentStepIndex ? ConsoleColor.Green : ConsoleColor.DarkGray;
                
                var connector = new string(connectorChar, Math.Max(0, connectorLength));
                framework.WriteAt(currentX + 1, y, connector, connectorColor);
            }
            
            currentX += stepWidth;
        }
    }
    
    private static char GetStepChar(StepStatus status, bool isCurrent)
    {
        return status switch
        {
            StepStatus.Completed => '✓',
            StepStatus.Error => '✗',
            StepStatus.Warning => '⚠',
            _ when isCurrent => '●',
            _ => '○'
        };
    }
    
    private static ConsoleColor GetStepColor(StepStatus status, bool isCurrent)
    {
        return status switch
        {
            StepStatus.Completed => ConsoleColor.Green,
            StepStatus.Error => ConsoleColor.Red,
            StepStatus.Warning => ConsoleColor.Yellow,
            _ when isCurrent => ConsoleColor.Cyan,
            _ => ConsoleColor.DarkGray
        };
    }
}

/// <summary>
/// Represents a progress step.
/// </summary>
public class ProgressStep
{
    public string Name { get; set; }
    public string Description { get; set; }
    public StepStatus Status { get; set; } = StepStatus.Pending;
    
    public ProgressStep(string name, string description = "")
    {
        Name = name ?? string.Empty;
        Description = description ?? string.Empty;
    }
}

/// <summary>
/// Progress step status options.
/// </summary>
public enum StepStatus
{
    Pending,
    InProgress,
    Completed,
    Error,
    Warning
}