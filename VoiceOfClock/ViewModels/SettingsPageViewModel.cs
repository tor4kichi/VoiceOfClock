using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Helpers;
using CommunityToolkit.WinUI.UI.Helpers;
using I18NPortable;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Models.Domain;
using VoiceOfClock.UseCases;
using Windows.ApplicationModel;
using Windows.Media.SpeechSynthesis;

namespace VoiceOfClock.ViewModels
{
    public sealed partial class SettingsPageViewModel : ObservableRecipient
    {
        private readonly TimerSettings _timerSettings;
        private readonly ApplicationSettings _applicationSettings;

        public string AppName { get; }
        public string AppVersion { get; }


        public SettingsPageViewModel(
            IMessenger messenger,
            TimerSettings timerSettings,
            ApplicationSettings applicationSettings
            )
            : base(messenger)
        {
            _timerSettings = timerSettings;
            _applicationSettings = applicationSettings;
            AppName = Package.Current.DisplayName;
            AppVersion = Package.Current.Id.Version.ToFormattedString(4);
        }

        protected override void OnDeactivated()
        {
            foreach (var item in Items)
            {
                (item as IDisposable)?.Dispose();
            }

            base.OnDeactivated();
        }

        protected override void OnActivated()
        {
            Items = new ObservableCollection<ISettingContent>(
                new[]
                {
                    CreateSpeechSettingContent(),
                    CreateAppearanceColorThemeSettingContent(),
                }
                .SelectMany(x => x)
                );

            InitializeItemContainerPosition(Items);

            base.OnActivated();
        }



        static void InitializeItemContainerPosition(IEnumerable<ISettingContent> items)
        {
            foreach (var item in items)
            {
                if (item is ExpanderSettingContent expander)
                {
                    foreach (var contentItem in expander.Items.SkipLast(1))
                    {
                        if (contentItem is SettingContentWithHeader header)
                        {
                            header.Position = SettingContainerPositionType.ExpanderMiddle;
                        }
                    }

                    var lastItem = expander.Items.Reverse().SkipWhile(x => x is not SettingContentWithHeader).FirstOrDefault();
                    if (lastItem is SettingContentWithHeader lastHeader)
                    {
                        lastHeader.Position = SettingContainerPositionType.ExpanderLast;
                    }
                }
            }
        }


        [ObservableProperty]
        private ObservableCollection<ISettingContent> _items;

        private ComboBoxSettingContentItem[] _allVoices;
        private IEnumerable<ISettingContent> CreateSpeechSettingContent()
        {
            yield return new SettingHeader("Speech".Translate());
            {
                if (_allVoices == null)
                {
                    var voices = new System.Speech.Synthesis.SpeechSynthesizer().GetInstalledVoices().Where(x => x.Enabled).Select(x => new LegacyVoiceInformation(x.VoiceInfo));
                    var winVoices = Windows.Media.SpeechSynthesis.SpeechSynthesizer.AllVoices.Select(x => new WindowsVoiceInformation(x));

                    // TODO: CurrentCultureのボイスを先頭に表示するようにしたい
                    _allVoices = Enumerable.Concat<IVoiceInformation>(voices, winVoices).Select(x => new ComboBoxSettingContentItem(x, x.ToString(), x.Id)).ToArray();
                }

                if (string.IsNullOrEmpty(_timerSettings.SpeechActorId))
                {
                    _timerSettings.SpeechActorId = _allVoices.FirstOrDefault(x => (x.Source as IVoiceInformation).Language == CultureInfo.CurrentCulture.Name, _allVoices.First()).Id;
                }

                var selectedVoice = _allVoices.FirstOrDefault(x => (x.Source as IVoiceInformation).Id == _timerSettings.SpeechActorId);
                yield return CreateComboBoxContent(_allVoices, selectedVoice, (s, voice) => _timerSettings.SpeechActorId = voice.Id, label: "SpeechActor".Translate());
            }

            yield return CreateSliderContent(_timerSettings.SpeechRate, x => _timerSettings.SpeechRate = x, TimerSettings.MinSpeechRate, TimerSettings.MaxSpeechRate, converter: ParcentageValueConverter.Default, label: "SpeechRate".Translate());
            yield return CreateSliderContent(_timerSettings.SpeechPitch, x => _timerSettings.SpeechPitch = x, TimerSettings.MinSpeechPitch, TimerSettings.MaxSpeechPitch, converter: ParcentageValueConverter.Default, label: "SpeechPitch".Translate());
            yield return CreateSliderContent(_timerSettings.SpeechVolume, x => _timerSettings.SpeechVolume = x, TimerSettings.MinSpeechVolume, TimerSettings.MaxSpeechVolume, converter: ParcentageValueConverter.Default, label: "SpeechVolume".Translate());

            {
                var speechWith24hComboBoxItems = new[]
                {
                    new ComboBoxSettingContentItem(true, "SpeechWith24h".Translate(), "true"),
                    new ComboBoxSettingContentItem(false, "SpeechWithAM_PM".Translate(), "false"),
                };

                yield return CreateComboBoxContent(speechWith24hComboBoxItems, speechWith24hComboBoxItems.First(x => (bool)x.Source == _timerSettings.IsTimeSpeechWith24h), (s, x) => _timerSettings.IsTimeSpeechWith24h = (bool)x.Source, label: "IsTimeSpeechWith24h".Translate());
            }

            //items.Add(CreateToggleSwitchContent(_timerSettings.UseSsml, useSsml => _timerSettings.UseSsml = useSsml, label: "SSMLを使用する"));
            yield return CreateButtonContent("SpeechSettingsTest".Translate(), async () => await Messenger.Send(new TimeOfDayPlayVoiceRequest(DateTime.Now)));



            List<ISettingContent> ampmPositionByLanguageItems = new List<ISettingContent>();
            var ampmPositionItems = Enum.GetValues<AMPMPosition>().Select(x => new ComboBoxSettingContentItem(x, x.Translate(), x.ToString())).ToArray();
            foreach (var language in _allVoices.Select(x => (x.Source as IVoiceInformation).Language).Distinct())
            {
                CultureInfo cultureInfo = CultureInfo.GetCultureInfo(language);
                var pos = _timerSettings.GetAmpmPosition(language);
                ampmPositionByLanguageItems.Add(CreateComboBoxContent(ampmPositionItems, ampmPositionItems.FirstOrDefault(x => (AMPMPosition)x.Source == pos), (container, selected) => _timerSettings.SetAmpmPosition(language, (AMPMPosition)selected.Source), language, $"{cultureInfo.DisplayName} - {cultureInfo.DateTimeFormat.AMDesignator}/{cultureInfo.DateTimeFormat.PMDesignator}"));
            }

            yield return new ExpanderSettingContent(ampmPositionByLanguageItems
                , label: "SpeechSettings_AmpmPositionByLanguage_Title".Translate()
                , description: "SpeechSettings_AmpmPositionByLanguage_Description".Translate()
                //, content: new TextSettingContent(_timerSettings.ObserveProperty(x => x.SpeechActorId).Select(x => _allVoices.FirstOrDefault(voice => voice.Id == x).Label ?? "NotSelected".Translate()))
                );
        }



        private IEnumerable<ISettingContent> CreateAppearanceColorThemeSettingContent()
        {
            yield return new SettingHeader("GeneralSettings".Translate());

            var themeItems = new[]
            {
                ElementTheme.Default,
                ElementTheme.Light,
                ElementTheme.Dark,
            }
            .Select(x => new ComboBoxSettingContentItem(x, x.Translate(), x.ToString()))
            .ToList();

            using var themeListener = new ThemeListener();
            var currentThemeItem = themeItems.First(x => (ElementTheme)x.Source == _applicationSettings.Theme);

            void ThemeChanged(ComboBoxSettingContent sender, ComboBoxSettingContentItem selected)
            {
                _applicationSettings.Theme = (ElementTheme)selected.Source;
                sender.Description = "ThemeApplyRequireRestartApp".Translate();
            }

            yield return CreateComboBoxContent(themeItems, currentThemeItem, ThemeChanged, label: "ColorTheme".Translate());
        }



        static ISettingContent CreateComboBoxContent(ICollection<ComboBoxSettingContentItem> items, ComboBoxSettingContentItem firstSelection, Action<ComboBoxSettingContent, ComboBoxSettingContentItem> selectedAction, string label = "", string description = "")
        {
            return new SettingContentWithHeader(new ComboBoxSettingContent(items, firstSelection, selectedAction), label, description);
        }

        static ISettingContent CreateSliderContent(double firstValue, Action<double> valueChanged, double minValue, double maxValue, IValueConverter converter = null, string label = "", string description = "")
        {
            return new SettingContentWithHeader(new SliderSettingContent(firstValue, valueChanged, minValue, maxValue, converter), label, description);
        }

        static ISettingContent CreateButtonContent(string buttonLabel, Action clickAction, string label = "", string description = "")
        {
            return new SettingContentWithHeader(new ButtonSettingContent(buttonLabel, clickAction), label, description);
        }

        static ISettingContent CreateButtonContent(string buttonLabel, Func<Task> clickAction, string label = "", string description = "")
        {
            return new SettingContentWithHeader(new ButtonSettingContent(buttonLabel, clickAction), label, description);
        }

        static ISettingContent CreateToggleSwitchContent(bool firstValue, Action<bool> valueChanged, string onContent = "", string offContent = "", string label = "", string description = "")
        {
            return new SettingContentWithHeader(new ToggleSwitchSettingContent(firstValue, valueChanged, onContent, offContent), label, description);
        }
    }

    public class VoiceInfoValueConverter : IValueConverter
    {
        private readonly IVoiceInformation[] _sourceItems;

        public VoiceInfoValueConverter(IVoiceInformation[] sourceItems)
        {
            _sourceItems = sourceItems;
        }
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string id)
            {
                return _sourceItems.FirstOrDefault(x => x.Id == id);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is IVoiceInformation vi)
            {
                return vi.Id;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }

    public interface IVoiceInformation
    { 
        string Id { get; }
        string Name { get; }
        string Language { get; }
        string Gender { get; }
    }

    public class WindowsVoiceInformation : IVoiceInformation
    {
        private readonly VoiceInformation _voiceInfomation;

        public WindowsVoiceInformation(VoiceInformation voiceInfomation)
        {
            _voiceInfomation = voiceInfomation;
        }

        public string Id => _voiceInfomation.Id;

        public string Name => _voiceInfomation.DisplayName;

        public string Language => _voiceInfomation.Language;

        public string Gender => _voiceInfomation.Gender.ToString();

        public override string ToString()
        {
            return "VoiceInfomationDisplayName".Translate(Name, Language);
        }
    }

    public class LegacyVoiceInformation : IVoiceInformation
    {
        private readonly VoiceInfo _voiceInfo;

        public LegacyVoiceInformation(VoiceInfo voiceInfo)
        {
            _voiceInfo = voiceInfo;
        }

        public string Id => _voiceInfo.Id;

        public string Name => _voiceInfo.Name;

        public string Language => _voiceInfo.Culture.Name;

        public string Gender => _voiceInfo.Gender.ToString();

        public override string ToString()
        {
            return "VoiceInfomationDisplayName".Translate(Name, Language);
        }
    }


    public interface ISettingContent
    {

    }

    public sealed class SettingHeader : ISettingContent
    {
        public SettingHeader(string title)
        {
            Title = title;
        }

        public string Title { get; }
    }

    public enum SettingContainerPositionType
    {
        Normal,
        ExpanderMiddle,
        ExpanderLast,
    }

    public partial class SettingContentWithHeader : ObservableObject, ISettingContent, IDisposable
    {
        public SettingContentWithHeader(ISettingContent content, string label = "", string description = "")
        {
            Content = content;
            _label = label;
            _description = description;            
        }

        private string _label;
        public string Label
        {
            get => _label;
            set => SetProperty(ref _label, value);
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }
        
        public ISettingContent Content { get; }
        public SettingContainerPositionType Position { get; set; }

         void IDisposable.Dispose()
        {
            (Content as IDisposable)?.Dispose();
        }
    }

    public sealed partial class ExpanderSettingContent 
        : SettingContentWithHeader, IDisposable

    {
        public ExpanderSettingContent(IEnumerable<ISettingContent> items, string label = "", string description = "", ISettingContent content = null)
            : base(content, label, description)
        {           
            Items = new ObservableCollection<ISettingContent>(items);
        }

        public ObservableCollection<ISettingContent> Items { get; }

        void IDisposable.Dispose()
        {
            foreach (var item in Items)
            {
                (item as IDisposable)?.Dispose();
            }            
        }
    }

    public sealed partial class SliderSettingContent : ObservableObject, ISettingContent
    {
        private readonly Action<double> _valueChanged;

        public SliderSettingContent(double firstValue, Action<double> valueChanged, double minValue, double maxValue, IValueConverter valueConverter)
        {
            _valueChanged = valueChanged;
            MinValue = minValue;
            MaxValue = maxValue;
            ValueConverter = valueConverter;

            _value = firstValue;
        }

        [ObservableProperty]
        private double _value;

        partial void OnValueChanged(double value)
        {
            _valueChanged(value);
        }

        public double MinValue { get; }
        public double MaxValue { get; }
        public Func<double, string> ConverterFunc { get; }
        public IValueConverter ValueConverter { get; }

        public string ConvertToString(double value)
        {            
            return ValueConverter.Convert(value, typeof(string), null, null).ToString();
        }
    }

    public sealed class ValueConverter : IValueConverter
    {
        private readonly Func<double, string> _converter;

        public ValueConverter(Func<double, string> converter)
        {
            _converter = converter;
        }
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double d)
            {
                return _converter(d);
            }
            else if (value is float f)
            {
                return _converter((double)f);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class ParcentageValueConverter : IValueConverter
    {
        public readonly static ParcentageValueConverter Default = new ParcentageValueConverter();

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double d)
            {
                return $"{d * 100.0d:F0}%";
            }
            else if (value is float f)
            {
                return $"{f * 100:F0}%";
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }


    public sealed partial class ButtonSettingContent : ISettingContent
    {
        public string Label { get; }
        public Func<Task> Action { get; }
        
        public ButtonSettingContent(string label, Func<Task> action)
        {
            Label = label;
            Action = action;
        }

        public ButtonSettingContent(string label, Action action)
        {
            Label = label;
            Action = () =>
            {
                action();
                return Task.CompletedTask;
            };
        }

        [RelayCommand]
        async Task DoAction()
        {
            await Action();
        }
    }

    public sealed partial class ToggleSwitchSettingContent : ObservableObject, ISettingContent
    {
        private readonly Action<bool> _valueChanged;

        public string OnContent { get; }
        public string OffContent { get; }
        
        public ToggleSwitchSettingContent(bool firstValue, Action<bool> valueChanged, string onCntent, string offContent)
        {
            _value = firstValue;
            _valueChanged = valueChanged;
            OnContent = onCntent;
            OffContent = offContent;
        }

        [ObservableProperty]
        private bool _value;

        partial void OnValueChanged(bool value)
        {
            _valueChanged(value);
        }
    }

    public sealed partial class ComboBoxSettingContent : ObservableObject, ISettingContent
    {
        public ComboBoxSettingContent(ICollection<ComboBoxSettingContentItem> items, ComboBoxSettingContentItem firstSelect, Action<ComboBoxSettingContent, ComboBoxSettingContentItem> selectedAction)
        {
            Items = items;
            FirstSelect = firstSelect;
            SelectedAction = selectedAction;
        }

        public ICollection<ComboBoxSettingContentItem> Items { get; }
        public ComboBoxSettingContentItem FirstSelect { get; }
        public Action<ComboBoxSettingContent, ComboBoxSettingContentItem> SelectedAction { get; }

        bool _skipOnFirst = true;

        [ObservableProperty]
        private string _description;

        public void ComboBoxSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (!args.AddedItems.Any()) { return; }
            if (_skipOnFirst)
            {
                _skipOnFirst = false;
                return;
            }

            SelectedAction(this, args.AddedItems[0] as ComboBoxSettingContentItem);
        }
    }

    public class ComboBoxSettingContentItem
    {
        public ComboBoxSettingContentItem(object source, string label, string id = null)
        {
            Source = source;
            Id = id;
            Label = label;
        }

        public object Source { get; }
        public string Id { get; }
        public string Label { get; }        
    }

    public sealed partial class TextSettingContent : ObservableObject, ISettingContent, IDisposable
    {
        public TextSettingContent(IObservable<string> textObservable)
        {
            _disposer = textObservable.Subscribe(x => Text = x);
        }

        [ObservableProperty]
        private string _text;

        private readonly IDisposable _disposer;

        public void Dispose()
        {
            _disposer.Dispose();
        }
    }
}
