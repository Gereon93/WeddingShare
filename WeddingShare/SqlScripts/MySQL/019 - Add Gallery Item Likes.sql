--
-- Table structure for gallery_item_likes
--
DROP TABLE IF EXISTS `gallery_item_likes`;
CREATE TABLE `gallery_item_likes` (
  `id` BIGINT NOT NULL PRIMARY KEY AUTO_INCREMENT,
  `gallery_item_id` BIGINT NOT NULL,
  `device_uuid` VARCHAR(100) NOT NULL,
  `created_at` DATETIME NOT NULL,
  FOREIGN KEY (`gallery_item_id`) REFERENCES `gallery_items` (`id`) ON DELETE CASCADE,
  UNIQUE KEY `unique_like` (`gallery_item_id`, `device_uuid`)
);

CREATE INDEX `idx_gallery_item_likes_item_id` ON `gallery_item_likes` (`gallery_item_id`);
CREATE INDEX `idx_gallery_item_likes_device_uuid` ON `gallery_item_likes` (`device_uuid`);
