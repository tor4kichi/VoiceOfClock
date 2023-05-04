using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace VoiceOfClock.Views;

public sealed class NullOrContentDataTemplateSelector : DataTemplateSelector
{
    public DataTemplate Null { get; set; }
    public DataTemplate Content { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return this.SelectTemplateCore(item, null);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item == null)
        {
            return Null;
        }
        else
        {
            return Content;
        }
    }
}

public sealed class NullOrContentStyleSelector : StyleSelector
{
    public Style Null { get; set; }
    public Style Content { get; set; }

    protected override Style SelectStyleCore(object item, DependencyObject container)
    {
        if (item == null)
        {
            return Null;
        }
        else
        {
            return Content;
        }
    }
}
