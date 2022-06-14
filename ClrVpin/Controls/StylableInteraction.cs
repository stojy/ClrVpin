using System.Collections.Generic;
using System.Windows;
using Microsoft.Xaml.Behaviors;
using TriggerBase = Microsoft.Xaml.Behaviors.TriggerBase;

namespace ClrVpin.Controls;

// interactions are not stylable 'out of the box'
// - refer https://stackoverflow.com/a/4779168/227110..
//   a. The first problem is that we cannot even construct a behavior setter value because the constructor is internal.
//      So we need our own behavior and trigger collection classes.
//      --> Interaction.Triggers can be constructed as a resource as there is no public ctor.. thus a new Triggers/Behaviors public ctor is required
//   b. The next problem is that the behavior and trigger attached properties don't have setters and so they can only be added to with in-line XAML.
//      This problem we solve with our own attached properties that manipulate the primary behavior and trigger properties.
//      --> Interaction.Triggers has no public setter (can only assign via inline xaml).. thus a new attached property required to access the new collection
//   c. The third problem is that our behavior collection is only good for a single style target.
//      This we solve by utilizing a little-used XAML feature x:Shared="False" which creates a new copy of the resource each time it is referenced.
//      --> EventTrigger must be unique per usage which is default behavior when ued inline but not via a style.. thus use x:Shared=False 
// - Shared=False resource must be defined at a top level, refer https://docs.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/aa970778(v=vs.100)?redirectedfrom=MSDN

public class Behaviors : List<Behavior> { }

public class Triggers : List<TriggerBase> { }

public static class StylableInteraction
{
    // ReSharper disable once UnusedMember.Global
    public static Behaviors GetBehaviors(DependencyObject obj) => (Behaviors)obj.GetValue(BehaviorsProperty);

    // ReSharper disable once UnusedMember.Global
    public static void SetBehaviors(DependencyObject obj, Behaviors value)
    {
        obj.SetValue(BehaviorsProperty, value);
    }

    public static Triggers GetTriggers(DependencyObject obj) => (Triggers)obj.GetValue(TriggersProperty);

    public static void SetTriggers(DependencyObject obj, Triggers value)
    {
        obj.SetValue(TriggersProperty, value);
    }

    private static void OnPropertyBehaviorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var behaviors = Interaction.GetBehaviors(d);
        foreach (var behavior in (Behaviors)e.NewValue)
            behaviors.Add(behavior);
    }

    private static void OnPropertyTriggersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // retrieve the 'regular' interaction triggers from the control
        var triggers = Interaction.GetTriggers(d);

        // add the new interaction to the control
        if (e.NewValue != null)
        {
            foreach (var trigger in (Triggers)e.NewValue)
                triggers.Add(trigger);
        }

        // remove the new interaction from the control
        if (e.OldValue != null)
        {
            foreach (var trigger in (Triggers)e.OldValue)
                triggers.Remove(trigger);
        }
    }

    public static readonly DependencyProperty BehaviorsProperty =
        DependencyProperty.RegisterAttached("Behaviors", typeof(Behaviors), typeof(StylableInteraction), new UIPropertyMetadata(null, OnPropertyBehaviorsChanged));

    public static readonly DependencyProperty TriggersProperty =
        DependencyProperty.RegisterAttached("Triggers", typeof(Triggers), typeof(StylableInteraction), new UIPropertyMetadata(null, OnPropertyTriggersChanged));
}