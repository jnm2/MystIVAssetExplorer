using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using System;

namespace MystIVAssetExplorer.Skybox;

public sealed partial class SkyboxControl : Control
{
    public static readonly StyledProperty<ReferenceCountedDisposable<SkyboxModel>?> SkyboxModelProperty =
        AvaloniaProperty.Register<SkyboxControl, ReferenceCountedDisposable<SkyboxModel>?>(nameof(SkyboxModel));

    private readonly DispatcherTimer rotationTimer = new();

    private readonly TextBlock instructionTextBlock = new()
    {
        Text = """For a rotatable 360° view, select a "data.m4b/global/w#/z##/n###" folder or one of the deepest-nested folders inside its "cube" folder.""",
        TextWrapping = TextWrapping.Wrap,
        TextAlignment = TextAlignment.Center,
        TextTrimming = TextTrimming.WordEllipsis,
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        FontSize = 16,
        FontWeight = FontWeight.Light,
        Padding = new Thickness(20),
        Foreground = Brushes.White,
    };

    private bool enablePanning;
    private double zoom;
    private (PointerPoint PointerPoint, double AngleX, double AngleY)? firstPointerDown;

    public ReferenceCountedDisposable<SkyboxModel>? SkyboxModel { get => GetValue(SkyboxModelProperty); set => SetValue(SkyboxModelProperty, value); }

    public double AngleX { get; set; }
    public double AngleY { get; set; }

    public SkyboxControl()
    {
        rotationTimer.Tick += OnRotationTimerTick;
        rotationTimer.Interval = TimeSpan.FromMilliseconds(20);

        VisualChildren.Add(instructionTextBlock);

        Focusable = true;
    }

    static SkyboxControl()
    {
        SkyboxModelProperty.Changed.AddClassHandler<SkyboxControl>((@this, e) =>
        {
            using (var lease = @this.SkyboxModel?.TryLease())
            {
                @this.enablePanning = lease?.LeasedInstance is not { Front: not null, Back: null, Left: null, Right: null, Top: null, Bottom: null };

                @this.instructionTextBlock.IsVisible = lease is null;
            }

            @this.rotationTimer.IsEnabled = @this.enablePanning;
            @this.Cursor = @this.enablePanning ? new Cursor(StandardCursorType.SizeAll) : null;
            @this.zoom = 1;

            if (!@this.enablePanning)
            {
                @this.AngleX = 0;
                @this.AngleY = 0;
            }

            @this.InvalidateVisual();
        });
    }

    public override void Render(DrawingContext context)
    {
        if (SkyboxModel?.TryLease() is { } lease)
        {
            context.Custom(new CheckerboardDrawOperation(this));
            context.Custom(new SkyboxDrawOperation(this, lease));
        }
    }

    private void OnRotationTimerTick(object? sender, EventArgs e)
    {
        AngleX -= 0.01;
        AngleY = -double.Sin(AngleX * 0.5) * double.Pi / 8;
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (enablePanning && firstPointerDown is null)
        {
            rotationTimer.Stop();
            firstPointerDown = (e.GetCurrentPoint(this), AngleX, AngleY);
            Cursor = new Cursor(StandardCursorType.None);
        }

        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (firstPointerDown?.PointerPoint.Pointer == e.Pointer)
        {
            firstPointerDown = null;
            Cursor = new Cursor(StandardCursorType.SizeAll);
        }

        base.OnPointerReleased(e);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        zoom *= double.Pow(1.1, e.Delta.X + e.Delta.Y);
        if (!enablePanning) zoom = double.Max(zoom, 1);
        InvalidateVisual();

        base.OnPointerWheelChanged(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (firstPointerDown is var (pointerPoint, startAngleX, startAngleY))
        {
            var viewportSize = Bounds.Size;

            var speedFactor = double.Pi / Math.Min(viewportSize.Width, viewportSize.Height);

            var diff = e.GetPosition(this) - pointerPoint.Position;

            AngleX = startAngleX - diff.X * speedFactor;
            AngleY = double.Clamp(startAngleY - diff.Y * speedFactor, -double.Pi / 2, double.Pi / 2);

            InvalidateVisual();
        }

        base.OnPointerMoved(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (enablePanning)
        {
            switch (e.Key)
            {
                case Key.F:
                    AngleX = 0;
                    AngleY = 0;
                    InvalidateVisual();
                    e.Handled = true;
                    break;

                case Key.B:
                    if (!e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    {
                        AngleX = -double.Pi;
                        AngleY = 0;
                    }
                    else
                    {
                        AngleX = 0;
                        AngleY = -double.Pi / 2;
                    }
                    InvalidateVisual();
                    e.Handled = true;
                    break;

                case Key.L:
                    AngleX = double.Pi / 2;
                    AngleY = 0;
                    InvalidateVisual();
                    e.Handled = true;
                    break;

                case Key.R:
                    AngleX = -double.Pi / 2;
                    AngleY = 0;
                    InvalidateVisual();
                    e.Handled = true;
                    break;

                case Key.T:
                    AngleX = 0;
                    AngleY = double.Pi / 2;
                    InvalidateVisual();
                    e.Handled = true;
                    break;
            }
        }

        base.OnKeyDown(e);
    }
}
