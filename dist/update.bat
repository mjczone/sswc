del sswc.*
curl -L -o sswc.zip https://github.com/mjczone/sswc/raw/master/dist/sswc.zip
curl -L -o 7z.exe https://github.com/mjczone/sswc/raw/master/lib/7z.exe
curl -L -o 7z.dll https://github.com/mjczone/sswc/raw/master/lib/7z.dll
7z.exe x sswc.zip
del sswc.zip
del 7z.exe
del 7z.dll