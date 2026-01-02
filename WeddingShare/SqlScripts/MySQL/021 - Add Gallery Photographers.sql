--
-- Table structure for gallery_photographers
-- Maps photographers to specific galleries they can upload to
--
DROP TABLE IF EXISTS `gallery_photographers`;
CREATE TABLE `gallery_photographers` (
  `id` BIGINT NOT NULL PRIMARY KEY AUTO_INCREMENT,
  `gallery_id` BIGINT NOT NULL,
  `user_id` BIGINT NOT NULL,
  `created_at` DATETIME NOT NULL,
  FOREIGN KEY (`gallery_id`) REFERENCES `galleries` (`id`) ON DELETE CASCADE,
  FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
  UNIQUE KEY `unique_gallery_photographer` (`gallery_id`, `user_id`)
);

CREATE INDEX `idx_gallery_photographers_gallery_id` ON `gallery_photographers` (`gallery_id`);
CREATE INDEX `idx_gallery_photographers_user_id` ON `gallery_photographers` (`user_id`);
