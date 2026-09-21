using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace baranggaysystem1.helper;

public class GridLengthAnimation : AnimationTimeline
{
	public static readonly DependencyProperty FromProperty = DependencyProperty.Register(
		nameof(From), typeof(GridLength), typeof(GridLengthAnimation));

	public static readonly DependencyProperty ToProperty = DependencyProperty.Register(
		nameof(To), typeof(GridLength), typeof(GridLengthAnimation));

	public static readonly DependencyProperty EasingFunctionProperty = DependencyProperty.Register(
		nameof(EasingFunction), typeof(IEasingFunction), typeof(GridLengthAnimation));

	public override Type TargetPropertyType => typeof(GridLength);

	protected override Freezable CreateInstanceCore() => new GridLengthAnimation();

	public GridLength From
	{
		get => (GridLength)GetValue(FromProperty);
		set => SetValue(FromProperty, value);
	}

	public GridLength To
	{
		get => (GridLength)GetValue(ToProperty);
		set => SetValue(ToProperty, value);
	}

	public IEasingFunction? EasingFunction
	{
		get => (IEasingFunction?)GetValue(EasingFunctionProperty);
		set => SetValue(EasingFunctionProperty, value);
	}

	public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
	{
		double fromVal = ((GridLength)GetValue(FromProperty)).Value;
		double toVal = ((GridLength)GetValue(ToProperty)).Value;

		if (animationClock.CurrentProgress == null)
		{
			return To;
		}

		double progress = animationClock.CurrentProgress.Value;
		if (EasingFunction != null)
		{
			progress = EasingFunction.Ease(progress);
		}

		double currentVal = fromVal + (toVal - fromVal) * progress;
		return new GridLength(Math.Max(0.0, currentVal), GridUnitType.Pixel);
	}
}
