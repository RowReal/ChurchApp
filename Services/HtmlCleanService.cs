using Ganss.Xss;

namespace ChurchApp.Services
{
    public class HtmlCleanService
    {
        private readonly HtmlSanitizer _sanitizer;

        public HtmlCleanService()
        {
            _sanitizer = new HtmlSanitizer();

            _sanitizer.AllowedTags.Clear();
            _sanitizer.AllowedTags.Add("p");
            _sanitizer.AllowedTags.Add("br");
            _sanitizer.AllowedTags.Add("strong");
            _sanitizer.AllowedTags.Add("b");
            _sanitizer.AllowedTags.Add("em");
            _sanitizer.AllowedTags.Add("i");
            _sanitizer.AllowedTags.Add("u");
            _sanitizer.AllowedTags.Add("ol");
            _sanitizer.AllowedTags.Add("ul");
            _sanitizer.AllowedTags.Add("li");
            _sanitizer.AllowedTags.Add("h1");
            _sanitizer.AllowedTags.Add("h2");
            _sanitizer.AllowedTags.Add("h3");
            _sanitizer.AllowedTags.Add("a");
            _sanitizer.AllowedTags.Add("blockquote");

            _sanitizer.AllowedAttributes.Clear();
            _sanitizer.AllowedAttributes.Add("href");
            _sanitizer.AllowedAttributes.Add("target");
            _sanitizer.AllowedAttributes.Add("rel");

            _sanitizer.AllowedSchemes.Clear();
            _sanitizer.AllowedSchemes.Add("http");
            _sanitizer.AllowedSchemes.Add("https");
            _sanitizer.AllowedSchemes.Add("mailto");
        }

        public string Clean(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            return _sanitizer.Sanitize(html);
        }
    }
}