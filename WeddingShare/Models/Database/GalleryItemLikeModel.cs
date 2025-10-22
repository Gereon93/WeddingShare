namespace WeddingShare.Models.Database
{
    public class GalleryItemLikeModel
    {
        public GalleryItemLikeModel()
            : this(0, 0, string.Empty, null)
        {
        }

        public GalleryItemLikeModel(int id, int galleryItemId, string deviceUuid, DateTime? createdAt)
        {
            Id = id;
            GalleryItemId = galleryItemId;
            DeviceUuid = deviceUuid;
            CreatedAt = createdAt ?? DateTime.UtcNow;
        }

        public int Id { get; set; }
        public int GalleryItemId { get; set; }
        public string DeviceUuid { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
