using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GuideMaker.App;

internal sealed class DetachedImageWindow : Window
{
    private readonly Image imageControl;

    public DetachedImageWindow(string title, ImageSource imageSource)
    {
        Title = title;
        Width = 920;
        Height = 680;
        MinWidth = 420;
        MinHeight = 320;

        imageControl = new Image
        {
            Stretch = Stretch.Uniform,
            Source = imageSource,
            Margin = new Thickness(12)
        };

        Content = new Border
        {
            Background = Brushes.Black,
            Child = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = imageControl
            }
        };
    }

    public void UpdateContent(string title, ImageSource imageSource)
    {
        Title = title;
        imageControl.Source = imageSource;
    }
}
