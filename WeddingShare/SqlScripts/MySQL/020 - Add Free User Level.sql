--
-- Bump user levels by 1 to make room for new levels
--
UPDATE `users` SET `level`=5 WHERE `level`=4;
UPDATE `users` SET `level`=4 WHERE `level`=3;
UPDATE `users` SET `level`=3 WHERE `level`=2;
UPDATE `users` SET `level`=2 WHERE `level`=1;

ALTER TABLE `users` MODIFY COLUMN `level` INT NOT NULL DEFAULT 5;