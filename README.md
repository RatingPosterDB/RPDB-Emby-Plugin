# RPDB Emby Plugin

Emby Plugin for [Rating Poster Database](https://ratingposterdb.com/).

**This plugin can be installed from the official Emby Plugins catalog, it is under the "General" category with the "RatingPosterDB" plugin name.**


[See Screenshots](https://ratingposterdb.com/#emby)

Currently supports:
- Choosing poster type (Tier 1+): 4 available options
- Textless posters (Tier 1+)
- Default Poster Language (Tier 1+)
- Backdrops (Tier 2+)
- Custom Rating Order (from 11 rating sources) (Tier 2+)
- Badges (Tier 2+)
- Image Styling (size, bar position, bar color, font color, etc) (Tier 3+)


You can also sideload it manually on your Emby Server:

- [download the plugin](https://github.com/RatingPosterDB/RPDB-Emby-Plugin/releases/latest/download/RPDB-Emby-Plugin.zip)
- unpack it
- copy the files from the "RPDB-Emby-Plugin" folder to Emby's "plugins" folder
- restart Emby Server


## Setting up the plugin

- disable IPv6 support on the device where you run your server (this is important as it can cause very high load times and even fail the image download)
- go to Settings > Advanced > Plugins
- click the RPDB plugin to set it up
- click "Save" to save settings
- go to Settings > Server > Library
- hover the libraries that you want to use RPDB with
- click the "..." to access the library's settings
- scroll down to "Movie Image Fetchers" (for series this is "Series Image Fetchers")
- enable the RPDB plugin from the list
- move the RPDB plugin to the top of the "Movie Image Fetchers" list
- optional: some users have mentioned that enabling "Keep a cached copy of images in the server's metadata folder" from the library settings also helped in their case
- click the "..." for the same library again
- select "Refresh Metadata"
- enable "Replace all Images"

Result Screenshot:

![emby-1](https://user-images.githubusercontent.com/1777923/114302550-3d77c080-9ad2-11eb-9699-fd4a5b6adfaf.jpg)

Settings Screenshot:

![Emby-Plugin-Settings](https://github.com/user-attachments/assets/52cb9fe1-cc70-4f5b-ad47-9a250b3a0500)

