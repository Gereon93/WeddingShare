using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WeddingShare.Resources.Templates.Email
{
    public class BasicEmail : PageModel
    {
        public string Title { get; set; } = "WeddingShare";
        public string Message { get; set; } = string.Empty;
        public BasicEmailLink? Link { get; set; }

        public void OnGet()
        {
        }
    }

    public class BasicEmailLink : PageModel
    {
        public string Heading { get; set; } = "Visit";
        public string Value { get; set; } = string.Empty;
    }
}
