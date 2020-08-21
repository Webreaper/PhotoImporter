# PhotoImporter
Utility to import images from an SD card into the Mac pictures folder.

This is a super simple utility which:

1. Scans volumes for an SD card.
2. Enumerates the image files on the SD Card
3. Enumerates the image files on the local Pictures folder (this is designed for MacOS)
4. Finds the new pictures on the SD card which don't already exist on the local disk
5. Moves the new pictures across, grouping by date in folders of the form "SD Imported Pics dd-MMM-yyyy"
6. If the move fails, it attempts to copy instead.
