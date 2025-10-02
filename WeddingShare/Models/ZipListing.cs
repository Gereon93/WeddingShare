namespace WeddingShare.Models
{
    public class ZipListing
    {
        public ZipListing(string sourcePath, IEnumerable<string> files, string? filename = null)
        {
            this.SourcePath = sourcePath;
            this.FileName = filename;
            this.Files = files;
        }

        public string SourcePath { get; }
        public string? FileName { get; }
        public IEnumerable<string>? Files { get; }
    }
}