--
-- Table structure for table `gallery_likes`
--
DROP TABLE IF EXISTS `gallery_likes`;
CREATE TABLE `gallery_likes` (
  `id` BIGINT NOT NULL PRIMARY KEY AUTO_INCREMENT,
  `gallery_item_id` BIGINT NOT NULL,
  `user_id` BIGINT NOT NULL,
  `timestamp` DATETIME NOT NULL,
  FOREIGN KEY (`gallery_item_id`) REFERENCES `gallery_items` (`id`),
  FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
);

--
-- Fix bug where galleries can't have the same name
--
ALTER TABLE `galleries` DROP INDEX `name`;