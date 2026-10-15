using ShootCounter.Helpers;

namespace ShootCounter.Views;

// popup with the meaning of the icons, placed above the content of a page
public partial class LegendOverlay : ContentView
{
    public static readonly BindableProperty ItemsProperty =
        BindableProperty.Create(nameof(Items), typeof(IEnumerable<LegendItem>), typeof(LegendOverlay));

    public IEnumerable<LegendItem>? Items
    {
        get => (IEnumerable<LegendItem>?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public LegendOverlay()
    {
        InitializeComponent();
    }

    public void Show() => IsVisible = true;

    private void Close_Tapped(object? sender, EventArgs e) => IsVisible = false;
}
