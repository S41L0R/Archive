# Archive
### -- An implementation of a novel archival solution --

So, the concept is as follows: You want to have an archive for multiple seperatly-encrypted folders. You cannot access any folder without knowing it's name. In fact, without knowing it's name, you can't even tell that it exists by looking at the file. The only unencrypted data in the file is the size and length of anonymous entries.
#

## Implementation Currently Supports:
* Adding folders to the archive (With two levels of optional gzip compression)
* Extracting folders from the archive
* Checking the archive for corruption against it's SHA-256 hash
* Up to 2,147,483,647 bytes theoretical storage. (About 2 GB)
