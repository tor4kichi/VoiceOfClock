using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Diagnostics;
using Windows.Foundation;

namespace VoiceOfClock.Views.Controls;


public enum TimeSelectBoxFocusStatus
{
    NotFocus,
    Hours,
    Minutes,
    Seconds,
}


public enum TimeSelectBoxDisplayMode
{
    Hours_Minutes_Seconds,
    Hours_Minutes,
    Minutes_Seconds,
    Hours,
    Minutes,
    Seconds,
}

public sealed class TimeSelectBoxTimeValueChangedEventArgs
{
    public TimeSpan NewTime { get; set; }
}


public sealed partial class TimeSelectBox : Control
{
    private readonly TimeSpan MaxTime = new TimeSpan(24, 0, 0);

    const string HoursNumberUserControlName = "Hours_Number_UserControl";
    const string MinutesNumberUserControlName = "Minutes_Number_UserControl";
    const string SecondsNumberUserControlName = "Seconds_Number_UserControl";

    const string HoursNumberTextBlockName = "Hours_Number_TextBlock";
    const string MinutesNumberTextBlockName = "Minutes_Number_TextBlock";
    const string SecondsNumberTextBlockName = "Seconds_Number_TextBlock";

    const string HoursUpButtonName = "Hours_Up_Button";
    const string HoursDownButtonName = "Hours_Down_Button";
    const string MinutesUpButtonName = "Minutes_Up_Button";
    const string MinutesDownButtonName = "Minutes_Down_Button";
    const string SecondsUpButtonName = "Seconds_Up_Button";
    const string SecondsDownButtonName = "Seconds_Down_Button";

    TimeSelectBoxFocusStatus _lastFocus = TimeSelectBoxFocusStatus.Hours;
    int? _lastInputNumber;

    int _lastHours;
    int _lastMinutes;
    int _lastSeconds;


    public event TypedEventHandler<TimeSelectBox, TimeSelectBoxTimeValueChangedEventArgs> TimeChanged;


    public TimeSelectBox()
    {
        this.DefaultStyleKey = typeof(TimeSelectBox);
    }

    private void UpdateTimeDisplay()
    {
        if (!IsLoaded) { return; }

        TimeSpan time = Time;
        if (time.Hours != _lastHours)
        {
            TextBlock hourTextBlock = GetTemplateChild(HoursNumberTextBlockName) as TextBlock;
            hourTextBlock.Text = time.Hours.ToString("d2");
            _lastHours = time.Hours;
        }

        if (time.Minutes != _lastMinutes)
        {
            TextBlock textBlock = GetTemplateChild(MinutesNumberTextBlockName) as TextBlock;
            textBlock.Text = time.Minutes.ToString("d2");
            _lastMinutes = time.Minutes;
        }

        if (time.Seconds != _lastSeconds)
        {
            TextBlock textBlock = GetTemplateChild(SecondsNumberTextBlockName) as TextBlock;
            textBlock.Text = time.Seconds.ToString("d2");
            _lastSeconds = time.Seconds;
        }        
    }


    public TimeSelectBoxFocusStatus CurrentFocus
    {
        get { return (TimeSelectBoxFocusStatus)GetValue(CurrentFocusProperty); }
        set { SetValue(CurrentFocusProperty, value); }
    }

    // Using a DependencyProperty as the backing store for CurrentFocus.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty CurrentFocusProperty =
        DependencyProperty.Register("CurrentFocus", typeof(TimeSelectBoxFocusStatus), typeof(TimeSelectBox), new PropertyMetadata(TimeSelectBoxFocusStatus.NotFocus, OnCurrentFocusPropertyChanged));

    private static void OnCurrentFocusPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        TimeSelectBox _this = d as TimeSelectBox;
        switch ((TimeSelectBoxFocusStatus)e.NewValue)
        {
            case TimeSelectBoxFocusStatus.Hours:
                VisualStateManager.GoToState(_this, "VS_CurrentFocus_Hours", true);
                break;
            case TimeSelectBoxFocusStatus.Minutes:
                VisualStateManager.GoToState(_this, "VS_CurrentFocus_Minutes", true);
                break;
            case TimeSelectBoxFocusStatus.Seconds:
                VisualStateManager.GoToState(_this, "VS_CurrentFocus_Seconds", true);
                break;
            default:
                VisualStateManager.GoToState(_this, "VS_CurrentFocus_NotFocus", true);
                break;
        }
    }


    public TimeSpan Time
    {
        get { return (TimeSpan)GetValue(TimeProperty); }
        set { SetValue(TimeProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Time.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TimeProperty =
        DependencyProperty.Register("Time", typeof(TimeSpan), typeof(TimeSelectBox), new PropertyMetadata(TimeSpan.Zero, OnTimePropertyChanged));

    private static void OnTimePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        TimeSelectBox _this = d as TimeSelectBox;
        _this.UpdateTimeDisplay();        
    }




    public TimeSelectBoxDisplayMode DisplayMode
    {
        get { return (TimeSelectBoxDisplayMode)GetValue(DisplayModeProperty); }
        set { SetValue(DisplayModeProperty, value); }
    }

    // Using a DependencyProperty as the backing store for DisplayMode.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty DisplayModeProperty =
        DependencyProperty.Register("DisplayMode", typeof(TimeSelectBoxDisplayMode), typeof(TimeSelectBox), new PropertyMetadata(TimeSelectBoxDisplayMode.Hours_Minutes_Seconds, OnDisplayModePropertyChanged));

    private static void OnDisplayModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        TimeSelectBox _this = d as TimeSelectBox;
        _this.UpdateDisplayMode();
    }


    private void UpdateDisplayMode()
    {
        switch (DisplayMode)
        {
            case TimeSelectBoxDisplayMode.Hours_Minutes_Seconds:
                VisualStateManager.GoToState(this, "VS_DisplayMode_Hours_Minutes_Seconds", true);
                break;
            case TimeSelectBoxDisplayMode.Hours_Minutes:
                VisualStateManager.GoToState(this, "VS_DisplayMode_Hours_Minutes", true);
                break;
            case TimeSelectBoxDisplayMode.Minutes_Seconds:
                VisualStateManager.GoToState(this, "VS_DisplayMode_Minutes_Seconds", true);
                break;
            case TimeSelectBoxDisplayMode.Hours:
                VisualStateManager.GoToState(this, "VS_DisplayMode_Hours", true);
                break;
            case TimeSelectBoxDisplayMode.Minutes:
                VisualStateManager.GoToState(this, "VS_DisplayMode_Minutes", true);
                break;
            case TimeSelectBoxDisplayMode.Seconds:
                VisualStateManager.GoToState(this, "VS_DisplayMode_Seconds", true);
                break;
        }
    }

    protected override void OnGotFocus(RoutedEventArgs e)
    {                
        base.OnGotFocus(e);

        VisualStateManager.GoToState(this, "VS_GotFocus", true);
        Debug.WriteLine($"[NumberBox] OnGotFocus : {CurrentFocus}");
    }


    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);

        _lastInputNumber = null;

        VisualStateManager.GoToState(this, "VS_LostFocus", true);
        Debug.WriteLine($"[NumberBox] OnLostFocus : {_lastFocus}");
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        KeyDown -= NumberBox_KeyDown;
        KeyDown += NumberBox_KeyDown;

        string[] buttonNames = new[]
        {
            HoursUpButtonName,
            HoursDownButtonName,
            MinutesUpButtonName,
            MinutesDownButtonName,
            SecondsUpButtonName,
            SecondsDownButtonName,
        };
        
        foreach (string buttonName in buttonNames)
        {
            if (GetTemplateChild(buttonName) is ButtonBase button)
            {
                button.Click -= UpDownButton_Click;
                button.Click += UpDownButton_Click;
            }
        }


        string[] userControlNames = new[]
        {
            HoursNumberUserControlName,
            MinutesNumberUserControlName,
            SecondsNumberUserControlName,
        };

        foreach (string userControl in userControlNames)
        {
            if (GetTemplateChild(userControl) is UserControl uc)
            {
                uc.GotFocus -= Uc_GotFocus;
                uc.GotFocus += Uc_GotFocus;
                uc.LostFocus -= Uc_LostFocus;
                uc.LostFocus += Uc_LostFocus;
                uc.Tapped -= Uc_Tapped;
                uc.Tapped += Uc_Tapped;
                uc.PointerWheelChanged -= Uc_PointerWheelChanged;
                uc.PointerWheelChanged += Uc_PointerWheelChanged;
            }
        }


        if (GetTemplateChild("ContentBackground_Grid") is Grid backGrid)
        {
            backGrid.Tapped -= BackGrid_Tapped;
            backGrid.Tapped += BackGrid_Tapped;
        }

        UpdateTimeDisplay();
    }

    private void BackGrid_Tapped(object sender, TappedRoutedEventArgs e)
    {
        string focusTargetName = _lastFocus switch
        {
            TimeSelectBoxFocusStatus.Hours => HoursNumberUserControlName,
            TimeSelectBoxFocusStatus.Minutes => MinutesNumberUserControlName,
            TimeSelectBoxFocusStatus.Seconds => SecondsNumberUserControlName,
            _ => HoursNumberUserControlName,
        };

        (GetTemplateChild(focusTargetName) as UserControl).Focus(FocusState.Programmatic);
    }

    private void Uc_GotFocus(object sender, RoutedEventArgs e)
    {
        _lastFocus = CurrentFocus = (sender as UserControl).Name switch
        {
            HoursNumberUserControlName => TimeSelectBoxFocusStatus.Hours,
            MinutesNumberUserControlName => TimeSelectBoxFocusStatus.Minutes,
            SecondsNumberUserControlName => TimeSelectBoxFocusStatus.Seconds,
            _ => TimeSelectBoxFocusStatus.Hours,
        };
    }

    private void Uc_LostFocus(object sender, RoutedEventArgs e)
    {
        CurrentFocus = TimeSelectBoxFocusStatus.NotFocus;
    }

    private void Uc_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is UserControl uc)
        {
            uc.Focus(FocusState.Programmatic);
            Debug.WriteLine($"[NumberBox] Uc_Tapped");
        }
    }

    private void Uc_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (sender is UserControl uc)
        {
            int wheelDelta = e.GetCurrentPoint(null).Properties.MouseWheelDelta;
            bool increment = wheelDelta > 0;
            bool decrement = wheelDelta < 0;
            switch (uc.Name)
            {
                case HoursNumberUserControlName when increment:
                    IncrementHours();
                    break;
                case HoursNumberUserControlName when decrement:
                    DecrementHours();
                    break;
                case MinutesNumberUserControlName when increment:
                    IncrementMinutes();
                    break;
                case MinutesNumberUserControlName when decrement:
                    DecrementMinutes();
                    break;
                case SecondsNumberUserControlName when increment:
                    IncrementSeconds();
                    break;
                case SecondsNumberUserControlName when decrement:
                    DecrementSeconds();
                    break;
            }

            Debug.WriteLine($"[NumberBox] Uc_PointerWheelChanged");
        }
    }


    private void UpDownButton_Click(object sender, RoutedEventArgs e)
    {       
        switch ((sender as ButtonBase).Name)
        {
            case HoursUpButtonName: 
                CurrentFocus = TimeSelectBoxFocusStatus.Hours;
                (GetTemplateChild(HoursNumberUserControlName) as UserControl).Focus(FocusState.Programmatic);
                IncrementHours();
                break;
            case HoursDownButtonName:
                CurrentFocus = TimeSelectBoxFocusStatus.Hours;
                (GetTemplateChild(HoursNumberUserControlName) as UserControl).Focus(FocusState.Programmatic);
                DecrementHours();
                break;
            case MinutesUpButtonName:
                CurrentFocus = TimeSelectBoxFocusStatus.Minutes;
                (GetTemplateChild(MinutesNumberUserControlName) as UserControl).Focus(FocusState.Programmatic);
                IncrementMinutes();
                break;
            case MinutesDownButtonName:
                CurrentFocus = TimeSelectBoxFocusStatus.Minutes;
                (GetTemplateChild(MinutesNumberUserControlName) as UserControl).Focus(FocusState.Programmatic);
                DecrementMinutes();
                break;
            case SecondsUpButtonName:
                CurrentFocus = TimeSelectBoxFocusStatus.Seconds;
                (GetTemplateChild(SecondsNumberUserControlName) as UserControl).Focus(FocusState.Programmatic);
                IncrementSeconds();
                break;
            case SecondsDownButtonName:
                CurrentFocus = TimeSelectBoxFocusStatus.Seconds;
                (GetTemplateChild(SecondsNumberUserControlName) as UserControl).Focus(FocusState.Programmatic);
                DecrementSeconds();
                break;
            default: break;
        }

        _lastFocus = CurrentFocus;

        Debug.WriteLine($"[NumberBox] UpDownButton_Tapped : {(sender as ButtonBase).Name}");
    }



    private void IncrementHours()
    {
        Time = Time.Hours == (int)MaxTime.TotalHours ? new TimeSpan(0, Time.Minutes, Time.Seconds) : Time + TimeSpan.FromHours(1);
        NoticeTimePropertyChanged();
    }

    private void DecrementHours()
    {
        Time = Time.Hours == 0 ? new TimeSpan((int)MaxTime.TotalHours - 1, Time.Minutes, Time.Seconds) : Time - TimeSpan.FromHours(1);
        NoticeTimePropertyChanged();
    }

    private void IncrementMinutes()
    {
        Time = Time.Minutes == 59 ? new TimeSpan(Time.Hours, 0, Time.Seconds) : Time + TimeSpan.FromMinutes(1);
        NoticeTimePropertyChanged();
    }

    private void DecrementMinutes()
    {
        Time = Time.Minutes == 0 ? new TimeSpan(Time.Hours, 59, Time.Seconds) : Time - TimeSpan.FromMinutes(1);
        NoticeTimePropertyChanged();
    }

    private void IncrementSeconds()
    {
        Time = Time.Seconds == 59 ? new TimeSpan(Time.Hours, Time.Minutes, 0) : Time + TimeSpan.FromSeconds(1);
        NoticeTimePropertyChanged();
    }

    private void DecrementSeconds()
    {
        Time = Time.Seconds == 0 ? new TimeSpan(Time.Hours, Time.Minutes, 59) : Time - TimeSpan.FromSeconds(1);
        NoticeTimePropertyChanged();
    }

    private void NoticeTimePropertyChanged()
    {
        this.TimeChanged?.Invoke(this, new TimeSelectBoxTimeValueChangedEventArgs() { NewTime = Time });
    }


    private void NumberBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        Debug.WriteLine($"[NumberBox] NumberBox_KeyDown : {e.Key}");
        if (e.Key == Windows.System.VirtualKey.Down)
        {
            switch (CurrentFocus)
            {
                case TimeSelectBoxFocusStatus.Hours: DecrementHours(); break;
                case TimeSelectBoxFocusStatus.Minutes: DecrementMinutes(); break;
                case TimeSelectBoxFocusStatus.Seconds: DecrementSeconds(); break;
            }
        }
        else if (e.Key == Windows.System.VirtualKey.Up)
        {
            switch (CurrentFocus)
            {
                case TimeSelectBoxFocusStatus.Hours: IncrementHours(); break;
                case TimeSelectBoxFocusStatus.Minutes: IncrementMinutes(); break;
                case TimeSelectBoxFocusStatus.Seconds: IncrementSeconds(); break;
            }
        }
        else if (e.Key is Windows.System.VirtualKey.Number0
            or Windows.System.VirtualKey.Number1
            or Windows.System.VirtualKey.Number2
            or Windows.System.VirtualKey.Number3
            or Windows.System.VirtualKey.Number4
            or Windows.System.VirtualKey.Number5
            or Windows.System.VirtualKey.Number6
            or Windows.System.VirtualKey.Number7
            or Windows.System.VirtualKey.Number8
            or Windows.System.VirtualKey.Number9
            )
        {
            TimeSelectBoxFocusStatus fucus = CurrentFocus;
            if (fucus == TimeSelectBoxFocusStatus.NotFocus) { return; }

            int inputNum = (int)e.Key - (int)Windows.System.VirtualKey.Number0;
            int num = inputNum;
            if (_lastInputNumber.HasValue)
            {
                num += _lastInputNumber.Value * 10;
            }

            _lastInputNumber = inputNum;
            TimeSpan time = Time;
            switch (fucus)
            {
                case TimeSelectBoxFocusStatus.Hours:
                    if (num >= MaxTime.TotalHours)
                    {
                        num = inputNum;
                    }

                    Time = new TimeSpan(num, time.Minutes, time.Seconds);
                    break;
                case TimeSelectBoxFocusStatus.Minutes:
                    if (num >= 60)
                    {
                        num = inputNum;
                    }
                    Time = new TimeSpan(time.Hours, num, time.Seconds);
                    break;
                case TimeSelectBoxFocusStatus.Seconds:
                    if (num >= 60)
                    {
                        num = inputNum;
                    }
                    Time = new TimeSpan(time.Hours, time.Minutes, num);
                    break;
            }
            
            NoticeTimePropertyChanged();
            UpdateTimeDisplay();
        }
    }
}




