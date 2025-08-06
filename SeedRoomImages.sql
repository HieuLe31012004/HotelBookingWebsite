-- Sample data to populate RoomImages table
-- This script adds sample images for the first 3 rooms

-- Insert images for Room 1 (assuming it exists)
INSERT INTO RoomImages (Path, RoomId, IsDefault) VALUES 
('/img/room-1.jpg', 1, 1),
('/img/about-1.jpg', 1, 0);

-- Insert images for Room 2 (assuming it exists)
INSERT INTO RoomImages (Path, RoomId, IsDefault) VALUES 
('/img/room-2.jpg', 2, 1),
('/img/about-2.jpg', 2, 0);

-- Insert images for Room 3 (assuming it exists)
INSERT INTO RoomImages (Path, RoomId, IsDefault) VALUES 
('/img/room-3.jpg', 3, 1),
('/img/about-3.jpg', 3, 0);

-- Check current rooms in the database
SELECT * FROM Rooms;

-- Check current room images
SELECT * FROM RoomImages;
