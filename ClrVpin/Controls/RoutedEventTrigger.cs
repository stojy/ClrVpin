using System;
using System.Windows;
using Microsoft.Xaml.Behaviors;

namespace ClrVpin.Controls
{
    // Workaround for the standard behavior EventTrigger which doesn't support attached events
    // - e.g. ComboBox 'TextBoxBase.TextChanged' (an attached event to TextBoxBase) vs TextBox 'TextChanged' (event is defined with the control)
    // - refer
    //   - https: //stackoverflow.com/a/37635681/227110
    //   - http://joyfulwpf.blogspot.com/2009/05/mvvm-invoking-command-on-attached-event.html
    // - "In the overridable method OnAttached, I attached a handler to the RoutedEvent and in the handler call the OnEvent method present in the base.
    //    That in turn calls all the actions associated with that trigger"
    // - accepts Routed events..
    //   - refer https://docs.microsoft.com/en-us/dotnet/desktop/wpf/advanced/routed-events-overview?view=netframeworkdesktop-4.8
    //   - Routed event = "\"routed event is "a type of event that can invoke handlers on multiple listeners in an element tree, rather than just on the object that raised the event."
    //   - Strategies..
    //     a. Tunneling
    //        - event is fired from root element/window and tunnels down to the routed event source
    //        - if event reaches the source, then the source will typically fire a bubbling event
    //        - nodes that mark event as handled prevent children elements from seeing the event
    //        - e.g. PreviewMouseDown event
    //     b. Bubbling
    //        - event is fired from source element, and bubbles up to root element/window until a parent marks it as handled.
    //        - nodes that mark event as handled prevent parent elements from seeing the event
    //        - typically used.  e.g. ComboBox's TextBox fires TextBoxBase.TextChanged which is handled by parent ComboBox.
    //        - e.g. MouseDown event
    //     c. Direct
    //        - only source can accept event
    // - e.g. use for ComboBox's TextBox fires TextBoxBase.TextChanged to be handled within the ComboBox parent
    public class RoutedEventTrigger : EventTriggerBase<DependencyObject>
    {
        public RoutedEvent RoutedEvent { get; set; }

        protected override void OnAttached()
        {
            var associatedElement = AssociatedObject as FrameworkElement;

            if (AssociatedObject is Behavior behavior)
                associatedElement = ((IAttachedObject)behavior).AssociatedObject as FrameworkElement;

            if (associatedElement == null)
                throw new ArgumentException("Routed Event trigger can only be associated to framework elements");

            // register for the specific routed event to invoke the OnEvent trigger base
            if (RoutedEvent != null)
                associatedElement.AddHandler(RoutedEvent, new RoutedEventHandler((_, args) => OnEvent(args)));
        }

        protected override string GetEventName() => RoutedEvent.Name;
    }
}