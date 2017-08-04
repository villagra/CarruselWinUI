using ExpressionBuilder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Composition.Interactions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using EF = ExpressionBuilder.ExpressionFunctions;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Carrusel.Controls
{
    public sealed partial class Carrusel : UserControl, IInteractionTrackerOwner
    {
        Visual _containerVisual;
        Compositor _compositor;

        InteractionTracker _tracker;
        CompositionPropertySet _props;

        List<string> items = new List<string>();

        public Carrusel()
        {
            this.InitializeComponent();

            items.Add("ms-appx:///Assets/Tmp/1.jpg");
            items.Add("ms-appx:///Assets/Tmp/2.jpg");
            items.Add("ms-appx:///Assets/Tmp/3.jpg");

            this.Loaded += Carrusel_Loaded;
            bttnLeft.Tapped += BttnLeft_Tapped;
            bttnRight.Tapped += BttnRight_Tapped;
        }

        private void BttnRight_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            _tracker.TryUpdatePositionWithAdditionalVelocity(new Vector3(1500f, 0f, 0f));
        }

        private void BttnLeft_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            var value = _tracker.TryUpdatePositionWithAdditionalVelocity(new Vector3(-1500f, 0f, 0f));
        }

        private void Carrusel_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.ActualWidth > 0)
            {
                InitializeLayout();
                InitializeComposition();
                InitializeAnimations();
                ConfigureRestingPoints();
            }
        }

        int _itemWidth = 640;
        int _itemHeight = 360;

        CarruselItemTemplate left;
        CarruselItemTemplate center;
        CarruselItemTemplate right;

        private void InitializeLayout()
        {
            center = CreateItem();
            center.DataContext = items.First();
            pnlRoot.Children.Add(center);

            right = CreateItem();
            right.DataContext = items[1];
            right.Shadow.Opacity = 0;
            pnlRoot.Children.Add(right);

            left = CreateItem();
            left.DataContext = items[2];
            left.Shadow.Opacity = 0;
            pnlRoot.Children.Add(left);

            var rightVisual = right.GetVisual();
            rightVisual.Size = new System.Numerics.Vector2(_itemWidth, _itemHeight);
            rightVisual.Offset = new System.Numerics.Vector3((float)right.Width, 0, 0);

            var leftVisual = left.GetVisual();
            leftVisual.Size = new System.Numerics.Vector2(_itemWidth, _itemHeight);
            leftVisual.Offset = new System.Numerics.Vector3(-(float)right.Width, 0, 0);
        }

        private void InitializeComposition()
        {
            _containerVisual = ElementCompositionPreview.GetElementVisual(pnlRoot);
            _compositor = _containerVisual.Compositor;

            _tracker = InteractionTracker.CreateWithOwner(_compositor, this);

            _containerVisual.Size = new Vector2((float)pnlRoot.ActualWidth, (float)pnlRoot.ActualHeight);
            _props = _compositor.CreatePropertySet();

            VisualInteractionSource interactionSource = VisualInteractionSource.Create(_containerVisual);
            interactionSource.PositionXSourceMode = InteractionSourceMode.EnabledWithInertia;
            _tracker.InteractionSources.Add(interactionSource);

            _tracker.MaxPosition = new Vector3((float)_itemWidth);
            _tracker.MinPosition = new Vector3(-(float)_itemWidth);

            pnlRoot.PointerPressed += (s, a) =>
            {
                // Capture the touch manipulation to the InteractionTracker for automatic handling
                if (a.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Touch)
                {
                    try
                    {
                        interactionSource.TryRedirectForManipulation(a.GetCurrentPoint(s as UIElement));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Ignoring the failed redirect to prevent app crashing
                    }
                }
            };
        }

        private void InitializeAnimations()
        {
            _props.InsertScalar("position", 0);
            _props.InsertScalar("position1", 0);
            _props.InsertScalar("progress", 0);
            _props.InsertScalar("progressSigned", 0);
            _props.InsertScalar("progressNeg", 0);
            _props.InsertVector3("Translation", Vector3.Zero);


            var trackerNode = _tracker.GetReference();
            _props.StartAnimation("position", -trackerNode.Position.X);
            _props.StartAnimation("position1", EF.Scale(-trackerNode.Position.X, (ScalarNode)1.25f));
            _props.StartAnimation("progress", EF.Abs(trackerNode.Position.X) / trackerNode.MaxPosition.X);
            _props.StartAnimation("progressSigned", trackerNode.Position.X / trackerNode.MaxPosition.X);
            _props.StartAnimation("progressNeg", -trackerNode.Position.X / trackerNode.MaxPosition.X);


            Canvas.SetZIndex(left, 1);
            Canvas.SetZIndex(center, 2);
            Canvas.SetZIndex(right, 1);

            ElementCompositionPreview.SetIsTranslationEnabled(center, true);

            center.GetVisual().CenterPoint = new Vector3((float)_itemWidth * .5f, (float)_itemHeight * .5f, 0f);
            left.GetVisual().CenterPoint = new Vector3((float)_itemWidth * .5f, (float)_itemHeight * .5f, 0f);
            right.GetVisual().CenterPoint = new Vector3((float)_itemWidth * .5f, (float)_itemHeight * .5f, 0f);

            center.GetVisual().StartAnimation("offset.x", _props.GetReference().GetScalarProperty("position"));
            left.GetVisual().StartAnimation("offset.x", -_itemWidth + _props.GetReference().GetScalarProperty("position"));
            right.GetVisual().StartAnimation("offset.x", _itemWidth + _props.GetReference().GetScalarProperty("position"));

            var propSetProgress = _props.GetReference().GetScalarProperty("progress");
            center.GetVisual().StartAnimation("Scale", EF.Vector3(1, 1, 1) * EF.Lerp(1.2f, 1, propSetProgress));

            var propSetProgressSigned = _props.GetReference().GetScalarProperty("progressSigned");
            right.GetVisual().StartAnimation("Scale", EF.Vector3(1, 1, 1) * EF.Lerp(1, 1.2f, propSetProgressSigned));
            left.GetVisual().StartAnimation("Scale", EF.Vector3(1, 1, 1) * EF.Lerp(1, 1.2f, _props.GetReference().GetScalarProperty("progressNeg")));

            center.Shadow.StartAnimation("opacity", EF.Lerp(1, 0, _props.GetReference().GetScalarProperty("Progress")));
        }

        float p = 0.5f;
        private void ConfigureRestingPoints()
        {
            // Setup a possible inertia endpoint (snap point) for the InteractionTracker's minimum position
            var endpoint1 = InteractionTrackerInertiaRestingValue.Create(_compositor);

            // Use this endpoint when the natural resting position of the interaction is less than the halfway point 
            var trackerTarget = ExpressionValues.Target.CreateInteractionTrackerTarget();
            endpoint1.SetCondition(EF.Abs(trackerTarget.NaturalRestingPosition.X) < p * _itemWidth);

            // Set the result for this condition to make the InteractionTracker's y position the minimum y position
            endpoint1.SetRestingValue(trackerTarget.MinPosition.X + _itemWidth);

            // Setup a possible inertia endpoint (snap point) for the InteractionTracker's maximum position
            var endpoint2 = InteractionTrackerInertiaRestingValue.Create(_compositor);

            //Use this endpoint when the natural resting position of the interaction is more than the halfway point 
            endpoint2.SetCondition(trackerTarget.NaturalRestingPosition.X >= p * _itemWidth);

            //Set the result for this condition to make the InteractionTracker's y position the maximum y position
            endpoint2.SetRestingValue(trackerTarget.MaxPosition.X);

            // Setup a possible inertia endpoint (snap point) for the InteractionTracker's maximum position
            var endpoint3 = InteractionTrackerInertiaRestingValue.Create(_compositor);

            //Use this endpoint when the natural resting position of the interaction is more than the halfway point 
            endpoint3.SetCondition(trackerTarget.NaturalRestingPosition.X <= -(p * _itemWidth));

            //Set the result for this condition to make the InteractionTracker's y position the maximum y position
            endpoint3.SetRestingValue(trackerTarget.MinPosition.X);

            _tracker.ConfigurePositionXInertiaModifiers(new InteractionTrackerInertiaModifier[] { endpoint1, endpoint2, endpoint3 });
        }

        private CarruselItemTemplate CreateItem()
        {
            CarruselItemTemplate item = new CarruselItemTemplate();
            item.Background = new SolidColorBrush(Colors.Yellow);
            item.Width = 640;
            item.Height = 360;
            return item;
        }

        #region IInteractionTrackerOwner
        public void CustomAnimationStateEntered(InteractionTracker sender, InteractionTrackerCustomAnimationStateEnteredArgs args)
        {
        }

        public void IdleStateEntered(InteractionTracker sender, InteractionTrackerIdleStateEnteredArgs args)
        {
        }

        public void InertiaStateEntered(InteractionTracker sender, InteractionTrackerInertiaStateEnteredArgs args)
        {

        }

        public void InteractingStateEntered(InteractionTracker sender, InteractionTrackerInteractingStateEnteredArgs args)
        {

        }

        public void RequestIgnored(InteractionTracker sender, InteractionTrackerRequestIgnoredArgs args)
        {

        }

        public void ValuesChanged(InteractionTracker sender, InteractionTrackerValuesChangedArgs args)
        {
            Debug.WriteLine("X position: " + args.Position.X);
        }
        #endregion
    }
}
