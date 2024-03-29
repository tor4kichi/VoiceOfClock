﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Core.Contracts.Services;
using VoiceOfClock.Core.Infrastructure;

namespace VoiceOfClock.Core.Infrastructure;

/*
public class JsonObjectSerializer : Microsoft.Toolkit.Helpers.IObjectSerializer
{
    public string Serialize<T>(T value) => System.Text.Json.JsonSerializer.Serialize(value);

    public T Deserialize<T>(string value) => string.IsNullOrEmpty(value) || value == "null" ? default(T) : System.Text.Json.JsonSerializer.Deserialize<T>(value);
}
*/


/// <remarks>
/// 注意：BinaryJsonObjectSerializer は Nullale[T] をシリアライズできない
/// </remarks>
public abstract class SettingsBase : ObservableObject
{
    private readonly static IStorageHelper _localStorageHelper = Ioc.Default.GetRequiredService<IStorageHelper>();
    private readonly static AsyncLock _fileUpdateLock = new();
    public SettingsBase()
    {
    }


    static protected T? Read<T>(T? @default = default, [CallerMemberName] string propertyName = null!)
    {
        return _localStorageHelper.Read<T>(propertyName, @default);
    }

    static protected async Task<T?> ReadFileAsync<T>(T value, [CallerMemberName] string propertyName = null!)
    {
        using (await _fileUpdateLock.LockAsync(default))
        {
            return await _localStorageHelper.ReadFileAsync(propertyName, value);
        }
    }

    static protected void Save<T>(T value, [CallerMemberName] string propertyName = null!)
    {             
        _localStorageHelper.Save(propertyName, value);
    }

    static protected async Task SaveFileAsync<T>(T value, [CallerMemberName] string propertyName = null!)
    {
        using (await _fileUpdateLock.LockAsync(default))
        {
            await _localStorageHelper.CreateFileAsync(propertyName, value);
        }
    }

    static protected void Save<T>(T? value, [CallerMemberName] string propertyName = null!)
        where T : struct
    {
        _localStorageHelper.Save(propertyName, value);
    }

    protected new bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null!)
    {
        if (base.SetProperty(ref storage, value, propertyName))
        {
            Save<T>(value, propertyName);
            return true;
        }
        else
        {
            return true;
        }
    }

    protected override void OnPropertyChanging(PropertyChangingEventArgs e)
    {
        base.OnPropertyChanging(e);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
    }

    protected bool SetProperty<T>(ref T? storage, T? value, [CallerMemberName] string propertyName = null!)
        where T : struct
    {
        if (base.SetProperty(ref storage, value, propertyName))
        {
            Save<T>(value, propertyName);
            return true;
        }
        else
        {
            return true;
        }
    }

}    
