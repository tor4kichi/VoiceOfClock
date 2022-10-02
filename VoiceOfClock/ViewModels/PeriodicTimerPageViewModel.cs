using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Models.Domain;
using VoiceOfClock.UseCases;

namespace VoiceOfClock.ViewModels
{
    public interface IPeriodicTimerDialogService
    {
        Task<PeriodicTimerDialogResult> ShowEditTimerAsync(string dialogTitle, string timerTitle, TimeSpan startTime, TimeSpan endTime, TimeSpan intervalTime);
    }

    public sealed class PeriodicTimerDialogResult
    {
        public bool IsConfirmed { get; init; }
        public string Title { get; init; }
        public TimeSpan StartTime { get; init; }
        public TimeSpan EndTime { get; init; }
        public TimeSpan IntervalTime { get; init; }        
    }

    // ページを開いていなくても時刻読み上げは動作し続けることを前提に
    // ページの表示状態を管理する
    public sealed partial class PeriodicTimerPageViewModel : ObservableRecipient
    {
        private readonly IPeriodicTimerDialogService _dialogService;
        private readonly TimerLifetimeManager _timerLifetimeManager;
        public ReadOnlyReactiveCollection<PeriodicTimerViewModel> Timers { get; }
        public TimerSettings TimerSettings { get; }
        public PeriodicTimerViewModel InstantPeriodicTimer { get; }


        public PeriodicTimerPageViewModel(
            IMessenger messenger,
            IPeriodicTimerDialogService dialogService,
            TimerLifetimeManager timerLifetimeManager,
            TimerSettings timerSettings
            )
            : base(messenger)
        {
            _dialogService = dialogService;
            _timerLifetimeManager = timerLifetimeManager;
            TimerSettings = timerSettings;
            
            Timers = timerLifetimeManager.PeriodicTimers.ToReadOnlyReactiveCollection(x => new PeriodicTimerViewModel(x, DeleteTimerCommand));
            InstantPeriodicTimer = new PeriodicTimerViewModel(timerLifetimeManager.InstantPeriodicTimer, DeleteTimerCommand);
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            InstantPeriodicTimer.RefrectValue();
            foreach (var timer in Timers)
            {
                timer.RefrectValue();
            }

            ImmidiateTimerStartTime = DateTime.Today + _timerLifetimeManager.InstantPeriodicTimer.StartTime;
        }

        [RelayCommand]
        async Task AddTimer()
        {
            var result = await _dialogService.ShowEditTimerAsync("タイマーを追加", "", TimeSpan.Zero, TimeSpan.FromHours(1), TimeSpan.FromMinutes(5));
            if (result?.IsConfirmed is true)
            {
                var runningTimer = _timerLifetimeManager.CreatePeriodicTimer(
                    result.Title,
                    result.StartTime,
                    result.EndTime,
                    result.IntervalTime
                    );
            }
        }

        [RelayCommand]
        void DeleteTimer(PeriodicTimerViewModel timerVM)
        {
            if (timerVM.IsRemovable is false) { return; }

            _timerLifetimeManager.DeletePeriodicTimer(timerVM.PeriodicTimerRunningInfo);
        }

        [RelayCommand]
        async Task EditTimer(PeriodicTimerViewModel timerVM)
        {
            var result = await _dialogService.ShowEditTimerAsync("タイマーを編集", timerVM.Title, timerVM.StartTime, timerVM.EndTime, timerVM.IntervalTime);
            if (result?.IsConfirmed is true)
            {
                var timerInfo = timerVM.PeriodicTimerRunningInfo;
                using (timerInfo.DeferUpdate())
                {
                    timerVM.StartTime = timerInfo.StartTime = result.StartTime;
                    timerVM.EndTime  = timerInfo.EndTime = result.EndTime;
                    timerVM.IntervalTime = timerInfo.IntervalTime = result.IntervalTime;
                    timerVM.Title  = timerInfo.Title = result.Title;
                }
            }
        }

               
        private bool _nowEditting;
        public bool NowEditting
        {
            get => _nowEditting;
            private set => SetProperty(ref _nowEditting, value);
        }


        [RelayCommand]
        void DeleteToggle()
        {
            NowEditting = !NowEditting;
            foreach (var timer in Timers)
            {
                timer.IsEditting = NowEditting;
            }
        }

        private DateTime _immidiateTimerStartTime;
        public DateTime ImmidiateTimerStartTime
        {
            get => _immidiateTimerStartTime;
            private set => SetProperty(ref _immidiateTimerStartTime, value);
        }

            [RelayCommand]
        void StartImmidiateTimer(TimeSpan intervalTime)
        {
            _timerLifetimeManager.StartInstantPeriodicTimer(intervalTime);            
            ImmidiateTimerStartTime = DateTime.Today + _timerLifetimeManager.InstantPeriodicTimer.StartTime;
        }

        [RelayCommand]
        void StopImmidiateTimer()
        {
            _timerLifetimeManager.StopInstantPeriodicTimer();            
        }
    }
}
