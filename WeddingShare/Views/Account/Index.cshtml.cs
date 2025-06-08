using Microsoft.AspNetCore.Mvc.RazorPages;
using WeddingShare.Models.Database;

namespace WeddingShare.Views.Account
{
    public class IndexModel : PageModel
    {
        public IndexModel() 
        {
        }

        public List<GalleryItemModel>? PendingRequests { get; set; }
        public List<UserModel>? Users { get; set; }
        public List<GalleryModel>? Galleries { get; set; }
        public List<CustomResourceModel>? CustomResources { get; set; }
        public IEnumerable<AuditLogModel>? AuditLogs { get; set; }
        public IDictionary<string, string>? Settings { get; set; }

        public void OnGet()
        {
        }
    }
}