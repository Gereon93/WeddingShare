namespace WeddingShare.Models.Database
{
    public class GalleryPhotographerModel
    {
        public GalleryPhotographerModel()
            : this(0, 0, 0, null)
        {
        }

        public GalleryPhotographerModel(int id, int galleryId, int userId, DateTime? createdAt)
        {
            Id = id;
            GalleryId = galleryId;
            UserId = userId;
            CreatedAt = createdAt ?? DateTime.UtcNow;
        }

        public int Id { get; set; }
        public int GalleryId { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
