--
-- Table structure for gallery_item_likes
--
DROP TABLE IF EXISTS `gallery_item_likes`;
CREATE TABLE `gallery_item_likes` (
  `id` INTEGER NOT NULL PRIMARY KEY,
  `gallery_item_id` INTEGER NOT NULL,
  `device_uuid` TEXT NOT NULL,
  `created_at` INTEGER NOT NULL,
  FOREIGN KEY (`gallery_item_id`) REFERENCES `gallery_items` (`id`) ON DELETE CASCADE,
  UNIQUE(`gallery_item_id`, `device_uuid`)
);

CREATE INDEX `idx_gallery_item_likes_item_id` ON `gallery_item_likes` (`gallery_item_id`);
CREATE INDEX `idx_gallery_item_likes_device_uuid` ON `gallery_item_likes` (`device_uuid`);
