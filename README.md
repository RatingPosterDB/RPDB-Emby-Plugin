# RPDB Emby Plugin

Emby Plugin for [Rating Poster Database](https://ratingposterdb.com/).

[See Screenshots](https://ratingposterdb.com/#emby)

Currently supports:
- Choosing poster type (Tier 1+): 4 available options
- Textless posters (Tier 3+)
- Backdrops (Tier 3+)

To install manually on Emby Server for Windows:
- [download the plugin](https://github.com/jaruba/RPDB-Emby-Plugin/releases/download/v1.0.0/RPDB-Emby-Plugin.zip)
- unpack it
- copy the files from the "RPDB-Emby-Plugin" folder to `%AppData%\Emby-Server\programdata\plugins`
- restart Emby Server

Using the plugin:
- go to Settings > Advanced > Plugins
- click the RPDB plugin to set it up
- click "Save" to save settings
- go to Settings > Server > Library
- hover the libraries that you want to use RPDB with
- click the "..." to access the library's settings
- scroll down to "Movie Image Fetchers"
- enable the RPDB plugin from the list
- move the RPDB plugin to the top of the "Movie Image Fetchers" list
- click the "..." for the same library again
- select "Refresh Metadata"
