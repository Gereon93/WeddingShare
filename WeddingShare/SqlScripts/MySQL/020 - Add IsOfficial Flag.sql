--
-- Add is_official column to gallery_items table
--
ALTER TABLE `gallery_items` ADD COLUMN `is_official` TINYINT NOT NULL DEFAULT 0;
