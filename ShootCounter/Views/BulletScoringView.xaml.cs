using System.Collections;
using System.ComponentModel;

namespace ShootCounter.Views;

public partial class BulletScoringView : ContentView
{
    // number of bullets that fit next to each other, the rest is reached by scrolling
    public const int VisibleBullets = 5;

    public static readonly BindableProperty ColumnWidthProperty =
        BindableProperty.Create(nameof(ColumnWidth), typeof(double), typeof(BulletScoringView), 80d);

    private int _lastVisibleIndex = VisibleBullets - 1;

    public double ColumnWidth
    {
        get => (double)GetValue(ColumnWidthProperty);
        set => SetValue(ColumnWidthProperty, value);
    }

    // raised when a valuation of a bullet is checked or unchecked
    public event EventHandler? ValuationChanged;

    public BulletScoringView()
    {
        InitializeComponent();

        SizeChanged += (s, e) =>
        {
            if (Width > 0)
                ColumnWidth = Width / VisibleBullets;

            UpdateListWidth();
        };

        BulletList.PropertyChanged += BulletList_PropertyChanged;
    }

    private void BulletList_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ItemsView.ItemsSource))
            return;

        // a new run starts at the first bullet again
        _lastVisibleIndex = VisibleBullets - 1;
        if (BulletCount > 0)
            BulletList.ScrollTo(0, position: ScrollToPosition.Start, animate: false);

        UpdateListWidth();
        UpdateScrollHint();
    }

    // fewer bullets than columns are centered instead of starting on the left
    private void UpdateListWidth()
    {
        int count = BulletCount;
        bool centered = count > 0 && count < VisibleBullets;

        BulletList.WidthRequest = centered ? count * ColumnWidth : -1;
        BulletList.HorizontalOptions = centered ? LayoutOptions.Center : LayoutOptions.Fill;
    }

    private void BulletList_Scrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        _lastVisibleIndex = e.LastVisibleItemIndex;
        UpdateScrollHint();
    }

    private int BulletCount => (BulletList.ItemsSource as IEnumerable)?.Cast<object>().Count() ?? 0;

    // tells the shooter that more bullets follow on the right side
    private void UpdateScrollHint()
    {
        int count = BulletCount;
        int nextBullet = _lastVisibleIndex + 2;

        // the hint keeps its space while scrolling, so the bullets do not jump when it disappears
        ScrollHint.IsVisible = count > VisibleBullets;
        ScrollHint.Opacity = nextBullet <= count ? 1 : 0;

        if (nextBullet <= count)
        {
            ScrollHint.Text = nextBullet < count
                ? $"Schuss {nextBullet}–{count}: nach links wischen ›"
                : $"Schuss {count}: nach links wischen ›";
        }
    }

    private void RadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        ValuationChanged?.Invoke(this, EventArgs.Empty);
    }
}
