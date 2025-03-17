using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.UIElements;

static class UIExtensions
{
    public static void SetPlaceholderText(this TextField textField, string placeholder)
    {
        string placeholderClass = "placeholder";

        onFocusOut();
        textField.RegisterCallback<FocusInEvent>(evt => onFocusIn());
        textField.RegisterCallback<FocusOutEvent>(evt => onFocusOut());

        void onFocusIn()
        {
            if (textField.ClassListContains(placeholderClass))
            {
                textField.value = string.Empty;
                textField.RemoveFromClassList(placeholderClass);
            }
        }

        void onFocusOut()
        {
            if (string.IsNullOrEmpty(textField.text))
            {
                textField.SetValueWithoutNotify(placeholder);
                textField.AddToClassList(placeholderClass);
            }
        }
    }

    public static float GetTransitionDuration(this VisualElement element, string property)
    {
        var durations = element.resolvedStyle.transitionDuration.ToArray();
        var propertyIndex = element.resolvedStyle.transitionProperty.ToList().IndexOf(property);
        if (propertyIndex < durations.Length && propertyIndex >= 0)
        {
            return durations[propertyIndex].value;
        }

        return 0;
    }

    public static void AddTransition(this VisualElement element, string property, float duration, float delay, EasingMode easing = EasingMode.EaseInOut)
    {
        if (element.style.transitionProperty == null || element.style.transitionProperty.value == null)
        {
            element.style.transitionProperty = new StyleList<StylePropertyName>(new List<StylePropertyName>());
            element.style.transitionDuration = new StyleList<TimeValue>(new List<TimeValue>());
            element.style.transitionDelay = new StyleList<TimeValue>(new List<TimeValue>());
            element.style.transitionTimingFunction = new StyleList<EasingFunction>(new List<EasingFunction>());
        }

        if (element.style.transitionProperty.value.Contains(property))
        {
            var index = element.style.transitionProperty.value.IndexOf(property);
            element.style.transitionDuration.value[index] = new TimeValue(duration);
            element.style.transitionDelay.value[index] = new TimeValue(delay);
            element.style.transitionTimingFunction.value[index] = new EasingFunction(easing);
            return;
        }

        element.style.transitionProperty.value.Add(property);
        element.style.transitionDuration.value.Add(new TimeValue(duration));
        element.style.transitionDelay.value.Add(new TimeValue(delay));
        element.style.transitionTimingFunction.value.Add(new EasingFunction(easing));
        return;
    }

    public static async Task FadeOut(this VisualElement element, float duration = 0.5f)
    {
        element.AddTransition("opacity", duration, 0);
        await Task.Delay(0);
        element.style.opacity = 0;    
        await Task.Delay((int)(duration * 1000));
        element.style.display = DisplayStyle.None;
        element.pickingMode = PickingMode.Ignore;
    }

    public static async Task FadeIn(this VisualElement element, float duration = 0.5f)
    {
        element.style.display = DisplayStyle.Flex;
        element.AddTransition("opacity", duration, 0);
        await Task.Delay(0);
        element.style.opacity = 1;
        await Task.Delay((int)(duration * 1000));
        element.pickingMode = PickingMode.Position;
    }
}