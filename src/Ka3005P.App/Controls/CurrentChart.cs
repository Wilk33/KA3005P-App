using System.Collections.Specialized;
using System.Windows;
using System.Windows.Media;
using Ka3005P.App.ViewModels;

namespace Ka3005P.App.Controls;

public sealed class CurrentChart : FrameworkElement
{
	public static readonly DependencyProperty PointsProperty=
		DependencyProperty.Register(
			nameof(Points),
			typeof(IEnumerable<ChartPoint>),
			typeof(CurrentChart),
			new FrameworkPropertyMetadata(null,OnPointsChanged));

	public static readonly DependencyProperty MinimumYProperty=
		DependencyProperty.Register(
			nameof(MinimumY),
			typeof(double),
			typeof(CurrentChart),
			new FrameworkPropertyMetadata(0d,FrameworkPropertyMetadataOptions.AffectsRender));

	public static readonly DependencyProperty MaximumYProperty=
		DependencyProperty.Register(
			nameof(MaximumY),
			typeof(double),
			typeof(CurrentChart),
			new FrameworkPropertyMetadata(1d,FrameworkPropertyMetadataOptions.AffectsRender));

	private INotifyCollectionChanged? observedCollection;

	public IEnumerable<ChartPoint>? Points
	{
		get => (IEnumerable<ChartPoint>?)GetValue(PointsProperty);
		set => SetValue(PointsProperty,value);
	}

	public double MinimumY
	{
		get => (double)GetValue(MinimumYProperty);
		set => SetValue(MinimumYProperty,value);
	}

	public double MaximumY
	{
		get => (double)GetValue(MaximumYProperty);
		set => SetValue(MaximumYProperty,value);
	}

	protected override void OnRender(DrawingContext drawingContext)
	{
		base.OnRender(drawingContext);
		Rect area=new(40,8,Math.Max(0,ActualWidth-48),Math.Max(0,ActualHeight-28));
		drawingContext.DrawRectangle(
			new SolidColorBrush(Color.FromRgb(100,100,100)),
			new Pen(Brushes.Black,1),
			area);
		Pen gridPen=new(new SolidColorBrush(Color.FromRgb(190,190,190)),0.7);
		for(int index=0;index<=10;index++)
		{
			double y=area.Top+(area.Height*index/10d);
			drawingContext.DrawLine(
				gridPen,
				new Point(area.Left,y),
				new Point(area.Right,y));
		}

		ChartPoint[] points=Points?.ToArray() ?? [];
		if(points.Length<2 || area.Width<=0 || area.Height<=0)
		{
			return;
		}
		double range=Math.Max(0.000001,MaximumY-MinimumY);
		StreamGeometry geometry=new();
		using(StreamGeometryContext context=geometry.Open())
		{
			for(int index=0;index<points.Length;index++)
			{
				double x=area.Left+(area.Width*index/(points.Length-1d));
				double normalized=(points[index].Value-MinimumY)/range;
				double y=area.Bottom-(Math.Clamp(normalized,0,1)*area.Height);
				Point point=new(x,y);
				if(index == 0)
				{
					context.BeginFigure(point,false,false);
				}
				else
				{
					context.LineTo(point,true,false);
				}
			}
		}
		geometry.Freeze();
		drawingContext.DrawGeometry(
			null,
			new Pen(new SolidColorBrush(Color.FromRgb(0,230,90)),2),
			geometry);
	}

	private static void OnPointsChanged(
		DependencyObject dependencyObject,
		DependencyPropertyChangedEventArgs eventArgs)
	{
		CurrentChart chart=(CurrentChart)dependencyObject;
		if(chart.observedCollection is not null)
		{
			chart.observedCollection.CollectionChanged-=chart.OnCollectionChanged;
		}
		chart.observedCollection=eventArgs.NewValue as INotifyCollectionChanged;
		if(chart.observedCollection is not null)
		{
			chart.observedCollection.CollectionChanged+=chart.OnCollectionChanged;
		}
		chart.InvalidateVisual();
	}

	private void OnCollectionChanged(
		object? sender,
		NotifyCollectionChangedEventArgs eventArgs)
	{
		InvalidateVisual();
	}
}
