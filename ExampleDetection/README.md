# ThemeServiceWebSite

Example app to detect a theme Chromaprint in a larger Chromaprint of the first 15 min of a TV Show Episode.

Using ffmpeg to extract the Chromaprint data with the chromaprint module.

This will extract the audio chromaprint data for a specific time period starting at 00:07:47 with a duration of 00:01:29
In the TV Show episode I was looking at this was where the intro themem music was.
```
C:\Install\ffmpeg\ffmpeg-5.0.1-full_build\bin\ffmpeg ^
-i "C:\Data\media\StarTrek - Discovery - S03E07.mkv" ^
-ss 00:07:47 ^
-t 00:01:29 ^
-ac 1 ^
-acodec pcm_s16le ^
-ar 16000 ^
-c:v nul ^
-f chromaprint ^
-fp_format raw ^
discovery_theme_01cmd.bin
```
This one does the same but for the first 15 minutes of the episode to give you something to search against.
```
C:\Install\ffmpeg\ffmpeg-5.0.1-full_build\bin\ffmpeg ^
-i "C:\Data\media\StarTrek - Discovery - S03E07.mkv" ^
-ss 00:00:00 ^
-t 00:15:00 ^
-ac 1 ^
-acodec pcm_s16le ^
-ar 16000 ^
-c:v nul ^
-f chromaprint ^
-fp_format raw ^
discovery_15min.bin
```

You can then use the example in the project to "find" the offset of the first shorter segment inside the larger one, this gives you the start of the into theme.

### Extracting WAV data
Checking your extractioned theme music as a wav file.
Use the following which is just the above but extracting the raw audio instead of the chromaprint to check you have the correct offsets when creating the chromaprint.
```
C:\Install\ffmpeg\ffmpeg-5.0.1-full_build\bin\ffmpeg ^
-i "C:\Data\media\StarTrek - Discovery - S03E07.mkv" ^
-ss 00:07:47 ^
-t 00:01:29 ^
-ac 1 ^
-acodec pcm_s16le ^
-ar 16000 ^
-c:v nul ^
discovery_theme_01.wav
```
