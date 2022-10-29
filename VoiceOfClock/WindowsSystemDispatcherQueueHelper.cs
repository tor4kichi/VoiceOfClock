using System.Runtime.InteropServices;

namespace VoiceOfClock;

internal class WindowsSystemDispatcherQueueHelper
{
    [StructLayout(LayoutKind.Sequential)]
    struct DispatcherQueueOptions
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:命名スタイル", Justification = "<保留中>")]
        internal int dwSize;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:命名スタイル", Justification = "<保留中>")]
        internal int threadType;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:命名スタイル", Justification = "<保留中>")]
        internal int apartmentType;
    }

    [DllImport("CoreMessaging.dll")]
    private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

#pragma warning disable CS8625 // null リテラルを null 非許容参照型に変換できません。
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:命名スタイル", Justification = "<保留中>")]
    object m_dispatcherQueueController = null;
#pragma warning restore CS8625 // null リテラルを null 非許容参照型に変換できません。
    public void EnsureWindowsSystemDispatcherQueueController()
    {
        if (m_dispatcherQueueController == null)
        {
            DispatcherQueueOptions options;
            options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
            options.threadType = 2;    // DQTYPE_THREAD_CURRENT
            options.apartmentType = 2; // DQTAT_COM_STA

#pragma warning disable CS8601 // Null 参照代入の可能性があります。
            _ = CreateDispatcherQueueController(options, ref m_dispatcherQueueController);
#pragma warning restore CS8601 // Null 参照代入の可能性があります。
        }
    }
}