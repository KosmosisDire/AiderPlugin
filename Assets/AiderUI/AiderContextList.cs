


using System.Collections.Generic;
using System.IO;
using UnityEngine.UIElements;

public class AiderContextList : VisualElement
{
    public AiderContextList()
    {
        AddToClassList("context-list");
    }

    public void Update(string[] items)
    {
        Clear();

        foreach (var item in items)
        {
            var filename = Path.GetFileName(item);
            var contextItem = new VisualElement();
            contextItem.AddToClassList("context-item");
            Add(contextItem);

            var dropButton = new Button(() =>
            {
                Client.DropFile(item);
                Update(Client.GetContextList());
            });
            dropButton.AddToClassList("context-drop-button");
            contextItem.Add(dropButton);
            
            var label = new Label(filename);
            label.AddToClassList("context-label");
            contextItem.Add(label);

        }
    }
}