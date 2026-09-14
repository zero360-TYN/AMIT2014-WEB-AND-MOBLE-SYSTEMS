using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Mail;

namespace Assignment.Helpers
{
    public static class HtmlHelperExtensions 
    {
        // ---------------------------BUTTON HELPER---------------------------
        public static IHtmlContent Button(this IHtmlHelper html,
                                          string text,
                                          string? type = null,
                                          string? @class = null,
                                          string? id = null,
                                          string? onclick = null)
        {
            var button = new TagBuilder("button");

            if (!string.IsNullOrEmpty(type))
            {
                button.Attributes["type"] = type;
            }

            if (!string.IsNullOrEmpty(@class))
            {
                button.Attributes["class"] = @class;
            }

            if (!string.IsNullOrEmpty(id))
            {
                button.Attributes["id"] = id;
            }

            if (!string.IsNullOrEmpty(onclick))
            {
                button.Attributes["onclick"] = onclick;
            }
            
            button.InnerHtml.Append(text);

            return button;
        }

        // ---------------------------INPUT HELPER---------------------------
        public static IHtmlContent Input(this IHtmlHelper html,
                                         string? type = null,
                                         string? placeholder = null,
                                         string? @class = null,
                                         string? maxlength = null,
                                         string? minlength = null,
                                         string? id = null,
                                         string? aspfor = null,
                                         string? name = null)
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

            if (!string.IsNullOrEmpty(maxlength))
            {
                input.Attributes["maxlength"] = maxlength;
            }

            if (!string.IsNullOrEmpty(minlength))
            {
                input.Attributes["minlength"] = minlength;
            }

            if (!string.IsNullOrEmpty(id))
            {
                input.Attributes["id"] = id;
            }

            if (!string.IsNullOrEmpty(aspfor))
            {
                input.Attributes["name"] = aspfor;
            }

            if (!string.IsNullOrEmpty(name))
            {
                input.Attributes["name"] = name;
            }

            return input;
        }
    }

    // ---------------------------EMAIL HELPER---------------------------

    public class Helper(IConfiguration cf, IWebHostEnvironment en, IHttpContextAccessor ct)
    {
        public void EmailHelper(MailMessage mail)
        {
            
        }
    }
}
