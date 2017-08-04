using ExpressionBuilder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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

            items.Add("ms-appx:///Assets/Tmp/0.png");
            items.Add("ms-appx:///Assets/Tmp/1.png");
            items.Add("ms-appx:///Assets/Tmp/2.png");
            items.Add("ms-appx:///Assets/Tmp/3.png");
            items.Add("ms-appx:///Assets/Tmp/4.png");
            items.Add("ms-appx:///Assets/Tmp/5.png");

            this.Loaded += Carrusel_Loaded;
            bttnLeft.Tapped += BttnLeft_Tapped;
            bttnRight.Tapped += BttnRight_Tapped;
        }

        private void BttnRight_Tapped2(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            _tracker.TryUpdatePositionWithAdditionalVelocity(new Vector3(1500f, 0f, 0f));
        }

        private void BttnLeft_Tapped2(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;

            var animation = _compositor.CreateVector3KeyFrameAnimation();
            animation.InsertKeyFrame(1, new Vector3(0, 0, 0));
            animation.Duration = TimeSpan.FromMilliseconds(333);
            _tracker.TryUpdatePositionWithAnimation(animation);

            //var value = _tracker.TryUpdatePositionWithAdditionalVelocity(new Vector3(-1500f, 0f, 0f));
        }

        private void Carrusel_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.ActualWidth > 0)
            {
                InitializeComposition();
                InitializeLayout();                
                InitializeAnimations();
                //ConfigureRestingPoints();
            }
        }

        int _itemWidth = 640;
        int _itemHeight = 360;

        CarruselItemTemplate left;
        CarruselItemTemplate center;
        CarruselItemTemplate right;


        CircularList<CarruselItemTemplate> itemsRendered = new CircularList<CarruselItemTemplate>();
        int _indexSelectedInItemsCollection = 0;

        private void InitializeLayout()
        {
            if (items.Count < 3) throw new ArgumentException("Not enought items");                        

            center = CreatePlaceHolder();
            center.Shadow.Opacity = 0;
            center.DataContext = items[_indexSelectedInItemsCollection];
            pnlRoot.Children.Add(center);
            itemsRendered.AddRight(center);

            var rightIdx = _indexSelectedInItemsCollection;
            var leftIdx = _indexSelectedInItemsCollection;

            //render two items on the right and two items on the left
            for (int i = 1; i <= 2; i++)
            {
                rightIdx = GetRight(rightIdx);
                right = CreatePlaceHolder();
                right.DataContext = items[rightIdx];
                right.Shadow.Opacity = 0;
                pnlRoot.Children.Add(right);
                right.GetVisual().Offset = new System.Numerics.Vector3((float)right.Width * i, 0, 0);
                itemsRendered.AddRight(right);


                leftIdx = GetLeft(leftIdx);
                left = CreatePlaceHolder();
                left.DataContext = items[leftIdx];
                left.Shadow.Opacity = 0;
                pnlRoot.Children.Add(left);
                left.GetVisual().Offset = new System.Numerics.Vector3(-(float)right.Width * i, 0, 0);
                itemsRendered.AddLeft(left);
            }

            ConfigureMinMax();
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

        private async void BttnLeft_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var itemMoved = itemsRendered.MoveLeft();

            var animation = _compositor.CreateVector3KeyFrameAnimation();
            animation.InsertKeyFrame(1, new Vector3(itemsRendered.SelectedIndex * _itemWidth, 0, 0));
            animation.Duration = TimeSpan.FromMilliseconds(333);            
            _tracker.TryUpdatePositionWithAnimation(animation);
                        
            var template = itemMoved.Item2;
            var newIndex = itemMoved.Item1;

            await Task.Delay(333);
            template.GetVisual().Offset = new System.Numerics.Vector3((float)right.Width * newIndex, 0, 0);
            ConfigureTemplateAnimations();
            ConfigureMinMax();
        }

        private async void BttnRight_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var itemMoved = itemsRendered.MoveRight();

            var animation = _compositor.CreateVector3KeyFrameAnimation();
            animation.InsertKeyFrame(1, new Vector3(itemsRendered.SelectedIndex * _itemWidth, 0, 0));
            animation.Duration = TimeSpan.FromMilliseconds(333);
            _tracker.TryUpdatePositionWithAnimation(animation);

            var template = itemMoved.Item2;
            var newIndex = itemMoved.Item1;

            await Task.Delay(333);
            template.GetVisual().Offset = new System.Numerics.Vector3((float)right.Width * newIndex, 0, 0);
            ConfigureTemplateAnimations();
            ConfigureMinMax();
        }

        private void ConfigureMinMax()
        {            
            _tracker.MaxPosition = new Vector3(itemsRendered.SelectedIndex * _itemWidth + _itemWidth);
            _tracker.MinPosition = new Vector3(itemsRendered.SelectedIndex * _itemWidth - _itemWidth);
        }

        private int GetLeft(int position)
        {
            var idx = position - 1;
            if (idx < 0)
            {
                return items.Count - 1;
            }
            return idx;
        }

        private int GetRight(int position)
        {
            var idx = position + 1;
            if (idx >= items.Count)
            {
                return 0;
            }
            return idx;
        }

        private void InitializeAnimations()
        {
            _props.InsertScalar("position", 0);
            _props.InsertScalar("progress", 0);            

            var trackerNode = _tracker.GetReference();
            _props.StartAnimation("position", -trackerNode.Position.X);
            _props.StartAnimation("progress", EF.Abs(trackerNode.Position.X) / trackerNode.MaxPosition.X);

            ConfigureTemplateAnimations();
        }

        private void ConfigureTemplateAnimations()
        {
            var propSetProgress = _props.GetReference().GetScalarProperty("progress");
            var trackerNode = _tracker.GetReference();

            foreach (var itemRendered in itemsRendered.Items)
            {
                var template = itemRendered.Item2;

                template.GetVisual().StopAnimation("offset.x");
                template.GetVisual().StopAnimation("Scale");

                template.GetVisual().StartAnimation("offset.x", _props.GetReference().GetScalarProperty("position") + itemRendered.Item1 * _itemWidth);
                //template.GetVisual().StartAnimation("Scale", EF.Vector3(1, 1, 1) * EF.Lerp(1.2f, 1, propSetProgress));

                float positionCenter = (itemRendered.Item1 * _itemWidth);
                float position = (itemRendered.Item1 * _itemWidth) - _itemWidth / 2;
                float positionEnd = (itemRendered.Item1 * _itemWidth) + _itemWidth/2;

                Debug.WriteLine($"{itemRendered.Item1} position: {position} positionend: {positionEnd}");

                template.GetVisual()
                    .StartAnimation("Scale",
                        EF.Vector3(1, 1, 1) * EF.Conditional(trackerNode.Position.X > position & trackerNode.Position.X < positionEnd
                        , EF.Lerp(1.2f, 1, EF.Abs(positionCenter - trackerNode.Position.X) / (_itemWidth / 2))
                        , 1));

                template.Shadow
                    .StartAnimation("opacity",
                        EF.Conditional(trackerNode.Position.X > position & trackerNode.Position.X < positionEnd
                        , EF.Lerp(1, 0, EF.Abs(positionCenter - trackerNode.Position.X) / (_itemWidth / 2))
                        , 0));

                //template.GetVisual().StartAnimation("Scale", EF.Vector3(1, 1, 1) * EF.Lerp(1.2f, 1, EF.Clamp(EF.Abs(trackerNode.Position.X), EF.Abs(position), EF.Abs(positionEnd))/_itemWidth/2));
                //template.GetVisual().StartAnimation("Scale", EF.Vector3(1, 1, 1) *  EF.Conditional(EF.Abs(trackerNode.Position.X) > EF.Abs(position) & EF.Abs(trackerNode.Position.X) < EF.Abs(positionEnd), 1.2f ,1));

                /*
                var _expression = _compositor.CreateExpressionAnimation("trackerNode.Position.X > position ? 2 : 1");
                _expression.SetReferenceParameter("trackerNode", _tracker);
                _expression.SetScalarParameter("position", 0);
                template.GetVisual().StartAnimation("Scale", _expression);
                */
                /*
                var _expression = _compositor.CreateExpressionAnimation("(ScrollManipulation.Translation.X > -StartPositionInGuide) ? 8 : -(ScrollManipulation.Translation.X + StartPositionInGuide - 8)");
                _expression.SetReferenceParameter("ScrollManipulation", _scrollProperties);
                _expression.SetScalarParameter("StartPositionInGuide", (float)_startPositionInGuide);
                _channelsVisual.StartAnimation("Offset.X", _expression);
                */
            }            

            /*
            var propSetProgress = _props.GetReference().GetScalarProperty("progress");
            center.GetVisual().StartAnimation("Scale", EF.Vector3(1, 1, 1) * EF.Lerp(1.2f, 1, propSetProgress));

            var propSetProgressSigned = _props.GetReference().GetScalarProperty("progressSigned");
            right.GetVisual().StartAnimation("Scale", EF.Vector3(1, 1, 1) * EF.Lerp(1, 1.2f, propSetProgressSigned));
            left.GetVisual().StartAnimation("Scale", EF.Vector3(1, 1, 1) * EF.Lerp(1, 1.2f, _props.GetReference().GetScalarProperty("progressNeg")));
            */
        }



        

        private void InitializeLayoutOld()
        {
            center = CreatePlaceHolder();
            center.DataContext = items.First();
            pnlRoot.Children.Add(center);

            right = CreatePlaceHolder();
            right.DataContext = items[1];
            right.Shadow.Opacity = 0;
            pnlRoot.Children.Add(right);

            left = CreatePlaceHolder();
            left.DataContext = items[2];
            left.Shadow.Opacity = 0;
            pnlRoot.Children.Add(left);

            var rightVisual = right.GetVisual();
            rightVisual.Size = new System.Numerics.Vector2(_itemWidth, _itemHeight);
            

            var leftVisual = left.GetVisual();
            leftVisual.Size = new System.Numerics.Vector2(_itemWidth, _itemHeight);
            leftVisual.Offset = new System.Numerics.Vector3(-(float)right.Width, 0, 0);
        }

        private void InitializeAnimations2()
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

        private CarruselItemTemplate CreatePlaceHolder()
        {
            CarruselItemTemplate item = new CarruselItemTemplate();
            item.Background = new SolidColorBrush(Colors.Yellow);
            item.Width = _itemWidth;
            item.Height = _itemHeight;

            item.GetVisual().CenterPoint = new Vector3((float)_itemWidth * .5f, (float)_itemHeight * .5f, 0f);

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
