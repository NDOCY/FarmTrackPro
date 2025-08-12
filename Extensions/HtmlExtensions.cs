// Extensions/HtmlExtensions.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.WebPages.Html;

namespace FarmTrack.Extensions
{
    public static class HtmlExtensions
    {
        public static SelectList GetEnumSelectList<TEnum>(this HtmlHelper htmlHelper) where TEnum : struct
        {
            return new SelectList(Enum.GetValues(typeof(TEnum)).Cast<TEnum>()
                .Select(e => new System.Web.Mvc.SelectListItem
                {
                    Text = e.ToString(),
                    Value = e.ToString()
                }), "Value", "Text");
        }
    }
    public static class HtmlHelperExtensions
    {
        public static SelectList GetEnumSelectList<TEnum>(this IHtmlHelper htmlHelper) where TEnum : struct
        {
            return new SelectList(Enum.GetValues(typeof(TEnum)).Cast<TEnum>()
                .Select(e => new System.Web.Mvc.SelectListItem
                {
                    Text = e.ToString(),
                    Value = e.ToString()
                }), "Value", "Text");
        }
    }
}