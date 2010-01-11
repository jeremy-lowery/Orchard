using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using Orchard.Blogs.Models;
using Orchard.Blogs.Services;
using Orchard.Blogs.ViewModels;
using Orchard.Localization;
using Orchard.ContentManagement;
using Orchard.Mvc.Results;

namespace Orchard.Blogs.Controllers {
    [ValidateInput(false)]
    public class BlogPostController : Controller {
        private readonly IOrchardServices _services;
        private readonly IBlogService _blogService;
        private readonly IBlogPostService _blogPostService;

        public BlogPostController(IOrchardServices services, IBlogService blogService, IBlogPostService blogPostService) {
            _services = services;
            _blogService = blogService;
            _blogPostService = blogPostService;
            T = NullLocalizer.Instance;
        }

        private Localizer T { get; set; }

        //TODO: (erikpo) Should think about moving the slug parameters and get calls and null checks up into a model binder or action filter
        public ActionResult Item(string blogSlug, string postSlug) {
            if (!_services.Authorizer.Authorize(Permissions.ViewPost, T("Couldn't view blog post")))
                return new HttpUnauthorizedResult();

            //TODO: (erikpo) Move looking up the current blog up into a modelbinder
            Blog blog = _blogService.Get(blogSlug);

            if (blog == null)
                return new NotFoundResult();

            //TODO: (erikpo) Look up the current user and their permissions to this blog post and determine if they should be able to view it or not.
            VersionOptions versionOptions = VersionOptions.Published;
            BlogPost post = _blogPostService.Get(blog, postSlug, versionOptions);

            if (post == null)
                return new NotFoundResult();

            var model = new BlogPostViewModel {
                Blog = blog,
                BlogPost = _services.ContentManager.BuildDisplayModel(post, "Detail")
            };

            return View(model);
        }

        public ActionResult ListByArchive(string blogSlug, string archiveData) {
            //TODO: (erikpo) Move looking up the current blog up into a modelbinder
            Blog blog = _blogService.Get(blogSlug);

            if (blog == null)
                return new NotFoundResult();

            var archive = new ArchiveData(archiveData);
            var model = new BlogPostArchiveViewModel {
                Blog = blog,
                ArchiveData = archive,
                BlogPosts = _blogPostService.Get(blog, archive).Select(b => _services.ContentManager.BuildDisplayModel(b, "Summary"))
            };

            return View(model);
        }

        //TODO: (erikpo) This should move to be part of the RoutableAspect
        public ActionResult Slugify(string value) {
            string slug = value;

            //TODO: (erikpo) Move this into a utility class
            if (!string.IsNullOrEmpty(value)) {
                Regex regex = new Regex("([^a-z0-9-_]?)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

                slug = value.Trim();
                slug = slug.Replace(' ', '-');
                slug = slug.Replace("---", "-");
                slug = slug.Replace("--", "-");
                slug = regex.Replace(slug, "");

                if (slug.Length * 2 < value.Length)
                    return Json("");

                if (slug.Length > 100)
                    slug = slug.Substring(0, 100);
            }

            return Json(slug);
        }
    }
}