using Kinrou.Controls.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Kinrou.Controls
{
    public class ListBoxWithSnap : System.Windows.Controls.ListBox
    {
        public delegate void SelectedItemChangedEventHandler(object sender, EventArgs data);
        public event SelectedItemChangedEventHandler selectedItemChanged;


        public delegate void TansitionCompleteEventHandler(object sender, EventArgs data);
        public event TansitionCompleteEventHandler transitionComplete;

        public delegate void ReadyEventHandler(object sender, EventArgs data);
        public event ReadyEventHandler ready;


        // reference to the scrollviewer contained in the listbox
        private ScrollViewer _scrollViewer;

        // stores the vertical offset of the scrollviewer when the user touches the screen
        private double _originalVerticalOffset = 0;

        // stores the old vertical offset when the vertical offset updates
        private double _oldVerticalOffset = 0;

        // stores the new vertical offset  when the vertical offset updates
        private double _newVerticalOffset = 0;

        // stores the curretly shown item index
        private int _itemIndex = 0;

        // stores the total number of items
        private int _totalNumOfItems;

        // stores whether the user's finger has left the screen
        private bool _hasCompletedFired = false;

        // stores storyboard instance
        private Storyboard _storyboard;

        // this variable is used to track whether the reset of the scrollviewer was requested
        // it will stop the o.scrollViewer.ScrollToVerticalOffset() to be called in the onVerticalOffsetChanged mehtod
        // it look like scrollViewer.VerticalOffset is not being updated rapidly enough
        // because when calling this method scrollViewer.ScrollToVerticalOffset(0) the event handler onVerticalOffsetChanged 
        // has the previous updated value not 0
        private bool _hasToReset = false;

        private bool _isInitialised = false;

        // set to true when the listbox items have been create and ready to be interacted with
        public bool IsInitialised { get { return _isInitialised; } }


        /*
         * declaration of the vertical offset dependency property property which when updated will call the onVerticalOffsetChanged event handler
         */
        public static readonly DependencyProperty verticalOffsetProperty =
            DependencyProperty.Register(
                "verticalOffset",
                typeof(double),
                typeof(ListBoxWithSnap),
                new PropertyMetadata(onVerticalOffsetChanged));


        public ListBoxWithSnap() : base()
        {
            SizeChanged += onSizeChanged;
        }

        /*
         * sets the index to 0
         */
        public void resetViewScroller()
        {
            if (_isInitialised)
            {
                _scrollViewer.ScrollToVerticalOffset(0);
                _itemIndex = 0;
                _hasToReset = true;
            }
        }


        /*
         * Apply Templete event handler
         * 
         * used to initialsed the different part of the new functionality added to the listbox control
         */ 
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            (Parent as UIElement).MouseLeftButtonUp += mouseLeftButtonUp;

            AddHandler(ListBox.ManipulationStartedEvent, (EventHandler<ManipulationStartedEventArgs>)listboxManipulationStarted, true);
            AddHandler(ListBox.ManipulationCompletedEvent, (EventHandler<ManipulationCompletedEventArgs>)listboxManipulationCompleted, true);
            //AddHandler(ListBox.ManipulationDeltaEvent, (EventHandler<ManipulationDeltaEventArgs>)listboxManipulationDelta, true); 

            _scrollViewer = GetTemplateChild("ScrollViewer") as ScrollViewer;
            _scrollViewer.ManipulationMode = ManipulationMode.Control;
            _scrollViewer.ApplyTemplate();

            ScrollBar verticalBar = ((FrameworkElement)VisualTreeHelper.GetChild(_scrollViewer, 0)).FindName("VerticalScrollBar") as ScrollBar;
            verticalBar.ValueChanged += verticalBarValueChanged;

            _totalNumOfItems = Items.Count;
        }

        /// <summary>
        /// get the list box item at the position defined by the index and of type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="index"></param>
        /// <returns></returns>
        public T getItemByIndex<T>(int index) where T : class
        {
            FrameworkElement element = ItemContainerGenerator.ContainerFromIndex(index) as FrameworkElement;
            T control = null;
            if (element != null)
            {
                control = FindDescendantByType<T>(element) as T;
            }
            return control;
        }


        /// <summary>
        /// helper to go through the visual tree,
        /// it brings back the first item of type T in the passed element
        /// // this is used to extract the control from the list box item
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="element"></param>
        /// <returns></returns>
        private FrameworkElement FindDescendantByType<T>(FrameworkElement element)
        {
            if (element == null || typeof(T) == null) { return null; }
            Type elementType = element.GetType();
            Type type = typeof(T);

            if (type.IsInstanceOfType(element))
            {
                return element;
            }
            var childCount = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < childCount; i++)
            {
                var result = FindDescendantByType<T>((VisualTreeHelper.GetChild(element, i) as FrameworkElement));
                if (result != null) { return result; }
            }
            return null;
        }


        /*
         * vertical offset property that is set to the vertical Offset Property
         */
        public double verticalOffset
        {
            get { return (double)GetValue(verticalOffsetProperty); }
            set { SetValue(verticalOffsetProperty, value); }
        }


        /*
       * vertical offset update event handler
       * 
       * is triggered every time the vertical offset property is updated
       * the new value (vertical offset) is then used to update the scroll vertical offset of the listbox's scrollviewer
       */ 
        public static void onVerticalOffsetChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var o = (ListBoxWithSnap)sender;
            if (null != o._scrollViewer && !o._hasToReset)
            {
                o._scrollViewer.ScrollToVerticalOffset((double)(e.NewValue));
            }
        }




        /*
         * list box manipulation started event handler
         * 
         * is triggered every time the user's finger enters the screen
         */ 
        private void listboxManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            //Debug.WriteLine("LB_ManipulationCompleted - " + e.TotalManipulation.Translation.Y.ToString());
            _hasCompletedFired = false;
            _hasToReset = false;
            if (_storyboard != null) _storyboard.Pause();
            _originalVerticalOffset = _oldVerticalOffset;

        }

        /*
         * list box manipulation complete event handler
         * 
         * is triggered every time the user's finger leaves the screen
         */ 
        private void listboxManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            //Debug.WriteLine("LB_ManipulationCompleted - " + e.TotalManipulation.Translation.Y.ToString());

            if (!_hasCompletedFired)
            {                
                setItem();
                onSelectedItemChanged(this, new ListBoxWithSnapSelectedItemChangedEventArgs(_itemIndex));
            }
            _hasCompletedFired = true;
        }

        
        /*
        private void listboxManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            _hasCompletedFired = false;
            _originalVerticalOffset = _oldVerticalOffset;
            //Debug.WriteLine("LB_ManipulationDelta - " + e.CumulativeManipulation.Translation.Y.ToString());
        }
        */


        /*
         * vertical bar position event handler
         * 
         * is triggered every time the position of the scroll bar changes, this reflects the position of the scrollviewer
         */
        private void verticalBarValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _oldVerticalOffset = _newVerticalOffset;          
            _newVerticalOffset = e.NewValue;

            //Debug.WriteLine(string.Format("new={0} , old={1}" , _newVerticalOffset, _oldVerticalOffset));
        }


        /// <summary>
        /// we are using the SizeChanged event to know that listbox items were added to the listbox
        /// since there's no status event available on the ItemContainerGenerator object in WP8
        /// a onready event is fired which allow hooks in the class that implements the listbox so they can start 
        /// interacting with the listbox items as soon as they are avialable 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="data"></param>
        private void onSizeChanged(object sender, EventArgs data)
        {
            SizeChanged -= onSizeChanged;
            _isInitialised = true;
            onReady(this, null);
        }


        private void onSelectedItemChanged(object sender, EventArgs data)
        {
            if (selectedItemChanged != null)
            {
                selectedItemChanged(this, data);
            }
        }


        private void onTransitionComplete(object sender, EventArgs data)
        {
            if (transitionComplete != null)
            {
                transitionComplete(this, data);
            }
        }


        private void onReady(object sender, EventArgs data)
        {
            if (ready != null)
            {
                ready(this, data);
            }
        }


        /*
         * mouse up event handler
         * 
         * is triggered every time the user's finger leaves the screen
         * 
         * this was implemented due to a bug with the list box where the manipulation complete event does not always fire
         */
        private void mouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //Debug.WriteLine(string.Format("mouseLeftButtonUp -- new={0} , old={1}", _newVerticalOffset, _oldVerticalOffset));

            if (!_hasCompletedFired)
            {
                setItem();
                onSelectedItemChanged(this, new ListBoxWithSnapSelectedItemChangedEventArgs(_itemIndex));
            }
            _hasCompletedFired = true;
        }


        /*
         * triggers storyboard to animated the scrollviewer depending on the current vertical offset and the current item index
         */
        private void transition()
        {
            DoubleAnimationUsingKeyFrames animation = new DoubleAnimationUsingKeyFrames();
            animation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = _newVerticalOffset });
            animation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(300), Value = _itemIndex, EasingFunction = new System.Windows.Media.Animation.CubicEase() { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut } });
            Storyboard.SetTarget(animation, this);
            Storyboard.SetTargetProperty(animation, new PropertyPath("verticalOffset"));
            animation.Completed += transitionCompleteEventHanlder;

            _storyboard = new Storyboard();
            _storyboard.Children.Add(animation);
            _storyboard.Begin();
        }



        private void transitionCompleteEventHanlder(object sender, EventArgs e)
        {
            (sender as DoubleAnimationUsingKeyFrames).Completed -= transitionCompleteEventHanlder;
            onTransitionComplete(this, new ListBoxWithSnapTransitionCompleteEventArgs(_itemIndex));
        }


        /*
         * works out wheather the transition animation should go up or down
         */
        private void setItem()
        {
            double diff = _oldVerticalOffset - _newVerticalOffset;

            if (Math.Abs(_originalVerticalOffset - _newVerticalOffset) > .05)
            {
                if (diff > 0)
                {
                    if (_itemIndex > 0) _itemIndex--;
                    //Debug.WriteLine("1");
                }
                else if (diff < 0)
                {
                    if (_itemIndex < _totalNumOfItems) _itemIndex++;
                    //Debug.WriteLine("2");
                }
            }

            transition();

            if (_itemIndex == -1) _itemIndex = 0;
            if (_itemIndex == _totalNumOfItems) _itemIndex = _totalNumOfItems - 1;
        }
    }
}
