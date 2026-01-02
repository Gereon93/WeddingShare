--
-- Table structure for table `gallery_likes`
--
DROP TABLE IF EXISTS `gallery_likes`;
CREATE TABLE `gallery_likes` (
  `id` INTEGER NOT NULL PRIMARY KEY,
  `gallery_item_id` INTEGER NOT NULL,
  `user_id` INTEGER NOT NULL,
  `timestamp` INTEGER NOT NULL,
  FOREIGN KEY (`gallery_item_id`) REFERENCES `gallery_items` (`id`),
  FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
);