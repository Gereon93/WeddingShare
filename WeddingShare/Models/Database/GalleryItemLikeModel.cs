namespace WeddingShare.Models.Database
{
    public class GalleryItemLikeModel
    {
        public GalleryItemLikeModel()
            : this(0, 0, 0, string.Empty, null)
        {
        }

        public GalleryItemLikeModel(int id, int galleryItemId, int userId, string deviceUuid, DateTime? timestamp)
        {
            Id = id;
            GalleryItemId = galleryItemId;
            UserId = userId;
            DeviceUuid = deviceUuid;
            Timestamp = timestamp;

        }

        public int Id { get; set; }
        public int GalleryItemId { get; set; }
        public int UserId { get; set; }
        public DateTime? Timestamp { get; set; }
        public string DeviceUuid { get; set; }
    }
}
