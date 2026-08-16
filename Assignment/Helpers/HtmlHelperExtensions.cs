using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Assignment.Helpers
{
    public static class HtmlHelperExtensions 
    {
        public static IHtmlContent Button(this IHtmlHelper html,
                                          string text,
                                          string? type = null,
                                          string? @class = null,
                                          string? id = null)
        {
            var button = new TagBuilder("button");

            if (!string.IsNullOrEmpty(type))
            {
                button.Attributes["type"] = type;
            }

            if (!string.IsNullOrEmpty(@class))
            {
                button.Attributes["@class"] = @class;
            }

            if (!string.IsNullOrEmpty("id"))
            {
                button.Attributes["id"] = id;
            }
            
            button.InnerHtml.Append(text);

            return button;
        }

        public static IHtmlContent Input(this IHtmlHelper html,
                                         string? type = null,
                                         string? placeholder = null,
                                         string? @class = null,
                                         string? id = null,
                                         string? aspfor = null)
        {
            var input = new TagBuilder("input");

            if (!string.IsNullOrEmpty(type))
            {
                input.Attributes["type"] = type;
            }

            if (!string.IsNullOrEmpty(placeholder))
            {
                input.Attributes["placeholder"] = placeholder;
            }

            if (!string.IsNullOrEmpty(@class))
            {
                input.Attributes["class"] = @class;
            }

            if (!string.IsNullOrEmpty(id))
            {
                input.Attributes["id"] = id;
            }

            if (!string.IsNullOrEmpty(aspfor))
            {
                input.Attributes["name"] = aspfor;
            }

            return input;
        }
    }
}
